using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Bricks.DepExtract;
using Nexo.Bricks.DepExtract.Profile;
using Nexo.Core.Application.Generation;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Bricks.Ports;
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
        static object[] T(string dir, string[] entries, string[] symbols, string note,
            string[]? includeDirs = null, string[]? defines = null) =>
            [dir, entries, symbols, note, includeDirs ?? [], defines ?? []];

        yield return T("T1_TrivialCounter", ["Counter.hpp"],
            ["SimpleCounter", "advance"], "in-memory only, no I/O, no seek");
        yield return T("T2_FixedRecordLog", ["FixedLog.hpp", "FixedLog.cpp"],
            ["FixedRecordLog", "readRecord"], "fixed-size records, sequential-only (no seek exists)");
        yield return T("T3_HeaderedFixedLog", ["TelemetryStream.hpp", "TelemetryStream.cpp"],
            ["TelemetryStream", "fetchNext", "jumpTo", "tell"], "fixed records + header + real seek");
        yield return T("T4_VariableLengthTLV", ["TlvEventStream.hpp", "TlvEventStream.cpp"],
            ["TlvEventStream", "nextEvent", "seekBytes"], "variable-length TLV records");
        yield return T("T5_MultiClassCursor", ["SessionLog.hpp", "SessionLog.cpp"],
            ["SessionLog", "SessionCursor", "openCursor", "seek"], "two-class indirection (must hold the cursor, not the log)");
        yield return T("T6_AbstractBaseHierarchy", ["RecordSource.hpp", "RecordSource.cpp"],
            ["BinaryRecordSource", "pull", "seekTo"], "abstract base + concrete subclass");
        yield return T("T7_StatefulMultiPass", ["ArchiveReader.hpp", "ArchiveReader.cpp"],
            ["ArchiveReader", "open", "readNext", "seekRecord"], "explicit open() lifecycle; seek is by RECORD INDEX not bytes");
        yield return T("T8_TemplatedRecordType", ["TypedEventLog.hpp"],
            ["SensorLog", "readTyped", "rewindTo"], "header-only template, must pick a concrete instantiation");
        yield return T("T9_ChunkedCompressed", ["ChunkedTraceLog.hpp", "ChunkedTraceLog.cpp"],
            ["ChunkedTraceLog", "nextTraceEvent", "seekToChunk"], "private buffer/decompress internals; seek is chunk-granular");
        yield return T("T10_IndexedMultiSection", ["FlightRecorderArchive.hpp", "FlightRecorderArchive.cpp"],
            ["FlightRecorderArchive", "selectSection", "nextRecord", "advanceToNextSection"], "TOC-indexed multi-section archive");
        yield return T("T11_MultiRootIncludes", ["src/MultiRootLog.hpp", "src/MultiRootLog.cpp"],
            ["MultiRootLog", "pullContact", "jumpTo", "bytePos"], "multi -I roots (include/ + vendor/)",
            includeDirs: ["include", "vendor/byteio"]);
        yield return T("T12_PlatformFactory", ["Core/EventFactory.hpp", "Core/EventFactory.cpp"],
            ["openPlatformStream", "IPlatformStream", "nextEvent", "seekAbs"], "ifdef platform factory",
            defines: ["USE_PLATFORM_B=1"]);
        yield return T("T13_GuiWarningFacade", ["WarningFacade.hpp", "WarningFacade.cpp"],
            ["WarningFacade", "readNamed", "seekSection", "warn", "critical"], "QMessageBox-shaped warnings + named fields");
        yield return T("T14_NestedRelativeGraph", ["engine/core/GraphReader.hpp", "engine/core/GraphReader.cpp"],
            ["GraphReader", "open", "next", "seekRecord", "PluginBridge"], "deep relative includes across engine/ + plugins/");
    }

    [Theory]
    [MemberData(nameof(Tiers))]
    public async Task Pipeline_handles_tier(string dir, string[] entries, string[] knownSymbols, string note,
        string[] includeDirs, string[] defines)
    {
        var srcDir = Path.Combine(FixturesRoot, dir);
        var outDir = Path.Combine(Path.GetTempPath(), "dep-extract-stress-" + dir + "-" + Guid.NewGuid());
        var ctx = new StressCtx();

        try
        {
            var extractor = new CppDependencyExtractorBrick(NullLogger<CppDependencyExtractorBrick>.Instance);
            var sw = Stopwatch.StartNew();
            var extractValues = new Dictionary<string, object>
            {
                ["srcDir"] = srcDir,
                ["outDir"] = outDir,
                ["entries"] = entries
            };
            if (includeDirs.Length > 0) extractValues["includeDirs"] = includeDirs;
            if (defines.Length > 0) extractValues["defines"] = defines;
            var extractOut = await extractor.ExecuteAsync(
                new BrickInput(extractValues),
                ImplementationType.Deterministic, ctx);
            var extractMs = sw.ElapsedMilliseconds;
            var duplicatePath = extractOut.Get<string>("duplicatePath");
            Directory.Exists(duplicatePath).Should().BeTrue($"[{dir}] extraction must produce a duplicate");

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IProviderFactory>(new ProviderFactory(NullLogger<ProviderFactory>.Instance));
            services.AddNexoCppEvtxAgentProfile();
            services.AddSingleton<GenerativeArtifactBrick>();
            await using var sp = services.BuildServiceProvider();
            var generator = sp.GetRequiredService<GenerativeArtifactBrick>();
            var grounding = CppAdapterPrompt.AssembleGrounding(duplicatePath, entries);
            sw.Restart();
            var adaptInput = new BrickInput();
            adaptInput.Set("target", CppEvtxProfileFactory.TargetId);
            adaptInput.Set("grounding", grounding);
            adaptInput.Set("preferDeterministic", false);
            adaptInput.Set("outputPath", Path.Combine(outDir, "adapted_reader.hpp"));
            var adaptOut = await generator.ExecuteAsync(adaptInput, ImplementationType.Agentic, ctx);
            var adaptMs = sw.ElapsedMilliseconds;

            var code = adaptOut.Get<GeneratedArtifact>("artifact").Content;
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
