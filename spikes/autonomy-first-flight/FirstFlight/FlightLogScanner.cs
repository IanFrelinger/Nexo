using Nexo.Core.Application.Certification.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Spikes.FirstFlight;

/// <summary>
/// The flight's scratch brick — the same deterministic log-scanner shape the gate-teeth
/// suites certify with zero escapes. The INSTANCE below and the SOURCE string must stay
/// semantically identical: the witness executes the instance, the mutation gate compiles
/// and mutates the source, and the certificate's content hash binds the source bytes.
/// </summary>
public sealed class FlightLogScannerBrick : DomainBrick
{
    public FlightLogScannerBrick()
    {
        Id = "first-flight-log-scanner";
        Name = "First Flight Log Scanner";
        Description = "Deterministic error-count brick for the autonomy first flight.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("logText", "string", "log")],
            Outputs =
            [
                new BrickOutputDefinition("errorCount", "int", "count"),
                new BrickOutputDefinition("firstErrorMessage", "string", "first")
            ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var logText = input.Get<string>("logText") ?? string.Empty;
        var errorLines = new List<string>();
        foreach (var line in logText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Contains("ERROR", StringComparison.Ordinal))
                errorLines.Add(line);
        }

        var errorCount = errorLines.Count;
        var firstErrorMessage = errorCount > 0 ? ExtractErrorMessage(errorLines[0]) : string.Empty;
        var output = new BrickOutput
        {
            Summary = $"Found {errorCount} ERROR line(s); first: {firstErrorMessage}"
        };
        output.Set("errorCount", errorCount);
        output.Set("firstErrorMessage", firstErrorMessage);
        return Task.FromResult(output);
    }

    private static string ExtractErrorMessage(string line)
    {
        var idx = line.IndexOf("ERROR", StringComparison.Ordinal);
        if (idx < 0)
            return line.Trim();

        var rest = line[(idx + 5)..].TrimStart(' ', ':');
        return rest;
    }
}

/// <summary>Source, witness, and scaffolding for certifying the flight brick.</summary>
public static class FlightLogScannerSource
{
    public const string BrickId = "first-flight-log-scanner";

    public const string Code = """
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.Spikes.FirstFlight;

public sealed class FlightLogScannerBrick : DomainBrick
{
    public FlightLogScannerBrick()
    {
        Id = "first-flight-log-scanner";
        Name = "First Flight Log Scanner";
        Description = "Deterministic error-count brick for the autonomy first flight.";
        Interface = new BrickInterface
        {
            Inputs = [new BrickInputDefinition("logText", "string", "log")],
            Outputs =
            [
                new BrickOutputDefinition("errorCount", "int", "count"),
                new BrickOutputDefinition("firstErrorMessage", "string", "first")
            ]
        };
    }

    public override Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var logText = input.Get<string>("logText") ?? string.Empty;
        var errorLines = new List<string>();
        foreach (var line in logText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Contains("ERROR", StringComparison.Ordinal))
                errorLines.Add(line);
        }

        var errorCount = errorLines.Count;
        var firstErrorMessage = errorCount > 0 ? ExtractErrorMessage(errorLines[0]) : string.Empty;
        var output = new BrickOutput
        {
            Summary = $"Found {errorCount} ERROR line(s); first: {firstErrorMessage}"
        };
        output.Set("errorCount", errorCount);
        output.Set("firstErrorMessage", firstErrorMessage);
        return Task.FromResult(output);
    }

    private static string ExtractErrorMessage(string line)
    {
        var idx = line.IndexOf("ERROR", StringComparison.Ordinal);
        if (idx < 0)
            return line.Trim();

        var rest = line[(idx + 5)..].TrimStart(' ', ':');
        return rest;
    }
}
""";

    public static WitnessSpec StrongWitness { get; } = new(
        BrickId,
        [
            new WitnessCase(
                new Dictionary<string, object>
                {
                    ["logText"] = "2026-01-01 INFO started\n2026-01-01 ERROR First failure: connection reset\n2026-01-01 WARN retrying\n2026-01-01 ERROR Second failure: timeout"
                },
                new Dictionary<string, object>
                {
                    ["errorCount"] = 2,
                    ["firstErrorMessage"] = "First failure: connection reset"
                }),
            new WitnessCase(
                new Dictionary<string, object> { ["logText"] = "all quiet" },
                new Dictionary<string, object>
                {
                    ["errorCount"] = 0,
                    ["firstErrorMessage"] = ""
                }),
            // Boundary case, added after the first model-proposed flight: the mutation
            // gate rejected that proposal with three surviving boundary-index mutants
            // (skip-first-character equivalents), naming a REAL gap — the witness never
            // pinned markers at the start of the text or of a line. Contract-derived
            // hardening, not implementation-fitting: any correct scanner must count
            // edge-positioned markers and extract their messages.
            new WitnessCase(
                new Dictionary<string, object> { ["logText"] = "ERROR first\nERROR second" },
                new Dictionary<string, object>
                {
                    ["errorCount"] = 2,
                    ["firstErrorMessage"] = "first"
                }),
            // Second hardening from the same flight campaign: a LEADING newline. The
            // remaining survivor mutated the newline-boundary sentinel so that a text
            // beginning with a newline collapses into one segment — which changes both
            // outputs here, and nowhere else in the witness.
            new WitnessCase(
                new Dictionary<string, object> { ["logText"] = "\nERROR a\nERROR b" },
                new Dictionary<string, object>
                {
                    ["errorCount"] = 2,
                    ["firstErrorMessage"] = "a"
                })
        ]);

    public static BrickInput SampleInput() => new(
        new Dictionary<string, object>
        {
            ["logText"] = "2026-01-01 ERROR live invocation: probe"
        });

    public static string WriteCleanProjectFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"first-flight-{Guid.NewGuid():N}.csproj");
        File.WriteAllText(path, """
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Nexo.Brick.Contracts" Version="0.1.0" />
  </ItemGroup>
</Project>
""");
        return path;
    }
}
