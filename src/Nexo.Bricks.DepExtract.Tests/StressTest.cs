using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Bricks.DepExtract;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Nexo.Infrastructure.Execution;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Bricks.DepExtract.Tests;

/// <summary>
/// Runs the extract -> adapt pipeline against ten fixtures at escalating
/// complexity (Fixtures/stress/T1..T10) and scores each drafted adapter
/// against a known-good API surface for that fixture. Not a strict pass/fail
/// gate on adaptation QUALITY (a small local model won't nail every tier) —
/// each fixture only hard-asserts that extraction and adaptation both
/// *complete* and produce non-trivial output; the interesting numbers
/// (symbol coverage, override coverage) are reported, not asserted, and
/// written to stress_report.json for review.
/// </summary>
public sealed class StressTest
{
    private readonly ITestOutputHelper _out;
    public StressTest(ITestOutputHelper output) => _out = output;

    private static readonly string FixturesRoot = Path.Combine(AppContext.BaseDirectory, "Fixtures", "stress");

    private static readonly string[] RequiredOverrides =
        ["next(", "position(", "resume_at(", "can_resume(", "set_requested_fields(", "decodes_named_fields("];

    public static IEnumerable<object[]> Tiers()
    {
        yield return new object[] { "T1_TrivialCounter", new[] { "Counter.hpp" },
            new[] { "SimpleCounter", "advance" }, "in-memory only, no I/O, no seek" };
        yield return new object[] { "T2_FixedRecordLog", new[] { "FixedLog.hpp", "FixedLog.cpp" },
            new[] { "FixedRecordLog", "readRecord" }, "fixed-size records, sequential-only (no seek exists)" };
        yield return new object[] { "T3_HeaderedFixedLog", new[] { "TelemetryStream.hpp", "TelemetryStream.cpp" },
            new[] { "TelemetryStream", "fetchNext", "jumpTo", "tell" }, "fixed records + header + real seek" };
        yield return new object[] { "T4_VariableLengthTLV", new[] { "TlvEventStream.hpp", "TlvEventStream.cpp" },
            new[] { "TlvEventStream", "nextEvent", "seekBytes" }, "variable-length TLV records" };
        yield return new object[] { "T5_MultiClassCursor", new[] { "SessionLog.hpp", "SessionLog.cpp" },
            new[] { "SessionLog", "SessionCursor", "openCursor", "seek" }, "two-class indirection (must hold the cursor, not the log)" };
        yield return new object[] { "T6_AbstractBaseHierarchy", new[] { "RecordSource.hpp", "RecordSource.cpp" },
            new[] { "BinaryRecordSource", "pull", "seekTo" }, "abstract base + concrete subclass" };
        yield return new object[] { "T7_StatefulMultiPass", new[] { "ArchiveReader.hpp", "ArchiveReader.cpp" },
            new[] { "ArchiveReader", "open", "readNext", "seekRecord" }, "explicit open() lifecycle; seek is by RECORD INDEX not bytes" };
        yield return new object[] { "T8_TemplatedRecordType", new[] { "TypedEventLog.hpp" },
            new[] { "SensorLog", "readTyped", "rewindTo" }, "header-only template, must pick a concrete instantiation" };
        yield return new object[] { "T9_ChunkedCompressed", new[] { "ChunkedTraceLog.hpp", "ChunkedTraceLog.cpp" },
            new[] { "ChunkedTraceLog", "nextTraceEvent", "seekToChunk" }, "private buffer/decompress internals; seek is chunk-granular" };
        yield return new object[] { "T10_IndexedMultiSection", new[] { "FlightRecorderArchive.hpp", "FlightRecorderArchive.cpp" },
            new[] { "FlightRecorderArchive", "selectSection", "nextRecord", "advanceToNextSection" }, "TOC-indexed multi-section archive" };
    }

    [Theory]
    [MemberData(nameof(Tiers))]
    public async Task Pipeline_handles_tier(string dir, string[] entries, string[] knownSymbols, string note)
    {
        var srcDir = Path.Combine(FixturesRoot, dir);
        var outDir = Path.Combine(Path.GetTempPath(), "dep-extract-stress-" + dir + "-" + Guid.NewGuid());
        var ctx = new StressCtx();

        try
        {
            var extractor = new CppDependencyExtractorBrick(NullLogger<CppDependencyExtractorBrick>.Instance);
            var sw = Stopwatch.StartNew();
            var extractOut = await extractor.ExecuteAsync(
                new BrickInput(new Dictionary<string, object> { ["srcDir"] = srcDir, ["outDir"] = outDir, ["entries"] = entries }),
                ImplementationType.Deterministic, ctx);
            var extractMs = sw.ElapsedMilliseconds;
            var duplicatePath = extractOut.Get<string>("duplicatePath");
            Directory.Exists(duplicatePath).Should().BeTrue($"[{dir}] extraction must produce a duplicate");

            var providerFactory = new ProviderFactory(NullLogger<ProviderFactory>.Instance);
            var adapter = new CppParserAdapterBrick(providerFactory, NullLogger<CppParserAdapterBrick>.Instance);
            sw.Restart();
            var adaptOut = await adapter.ExecuteAsync(
                new BrickInput(new Dictionary<string, object>
                {
                    ["parserDir"] = duplicatePath,
                    ["entryFiles"] = entries,
                    ["outputPath"] = Path.Combine(outDir, "adapted_reader.hpp")
                }),
                ImplementationType.Agentic, ctx);
            var adaptMs = sw.ElapsedMilliseconds;

            var code = adaptOut.Get<string>("adapterCode");
            code.Should().NotBeNullOrWhiteSpace($"[{dir}] adaptation must produce non-trivial output");

            var foundSymbols = knownSymbols.Where(s => code.Contains(s, StringComparison.Ordinal)).ToArray();
            var foundOverrides = RequiredOverrides.Where(o => code.Contains(o, StringComparison.Ordinal)).ToArray();
            var targetsContract = code.Contains("CustomEventReader", StringComparison.Ordinal);

            var report = new
            {
                fixture = dir, note, extractMs, adaptMs,
                lineCount = code.Split('\n').Length,
                knownSymbolsFound = foundSymbols, knownSymbolsTotal = knownSymbols.Length,
                missingSymbols = knownSymbols.Except(foundSymbols).ToArray(),
                overridesFound = foundOverrides.Length, overridesTotal = RequiredOverrides.Length,
                targetsContract, code
            };
            AppendReport(report);

            _out.WriteLine($"[{dir}] extract={extractMs}ms adapt={adaptMs}ms " +
                $"symbols={foundSymbols.Length}/{knownSymbols.Length} " +
                $"overrides={foundOverrides.Length}/{RequiredOverrides.Length} " +
                $"contract={(targetsContract ? "yes" : "NO")}");
            if (foundSymbols.Length < knownSymbols.Length)
                _out.WriteLine($"  missing: {string.Join(", ", knownSymbols.Except(foundSymbols))}");
        }
        finally
        {
            if (Directory.Exists(outDir))
            {
                try { Directory.Delete(outDir, recursive: true); } catch { /* best-effort */ }
            }
        }
    }

    private static readonly object ReportLock = new();
    private static bool _reportResetThisRun;
    private static void AppendReport(object row)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "stress_report.json");
        path = Path.GetFullPath(path);
        lock (ReportLock)
        {
            List<JsonElement> rows;
            if (!_reportResetThisRun)
            {
                rows = new List<JsonElement>();          // fresh file for this test run, not an ever-growing log
                _reportResetThisRun = true;
            }
            else
            {
                rows = File.Exists(path)
                    ? JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(path))!
                    : new List<JsonElement>();
            }
            var merged = rows.Select(r => (object)r).Append(row).ToList();
            File.WriteAllText(path, JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private sealed class StressCtx : IExecutionContext
    {
        public string AgentId => "stress-test";
        public string BehaviorId => "cpp-parser-adapter-stress";
        public bool IsAirGapped => true;
        public bool AuditMode => true;
        public string Provider => "ollama";
        public IReadOnlyDictionary<string, object> Variables { get; } = new Dictionary<string, object>();
    }
}
