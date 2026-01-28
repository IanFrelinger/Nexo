using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Nexo.CLI.Output;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command for comparing artifacts and outputs.
/// Replaces artifact-diff.sh script.
/// </summary>
public class DiffCommand : Command
{
    public DiffCommand() : base("diff", "Compare artifacts and outputs")
    {
        // nexo diff artifacts
        var artifactsCmd = new Command("artifacts", "Compare two artifact runs' phase outputs");
        var runAOpt = new Option<DirectoryInfo>(
            "--run-a",
            "Path to first run folder"
        ) { IsRequired = true };
        var runBOpt = new Option<DirectoryInfo>(
            "--run-b",
            "Path to second run folder"
        ) { IsRequired = true };
        var phasesOpt = new Option<string[]>(
            "--phases",
            () => new[] { "Plan", "Layout", "Placement", "Content", "Validate" },
            "Phases to compare"
        )
        {
            AllowMultipleArgumentsPerToken = true
        };

        artifactsCmd.AddOption(runAOpt);
        artifactsCmd.AddOption(runBOpt);
        artifactsCmd.AddOption(phasesOpt);

        artifactsCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var runA = ctx.ParseResult.GetValueForOption(runAOpt) ?? throw new InvalidOperationException("--run-a is required");
            var runB = ctx.ParseResult.GetValueForOption(runBOpt) ?? throw new InvalidOperationException("--run-b is required");
            var phases = ctx.ParseResult.GetValueForOption(phasesOpt) ?? new[] { "Plan", "Layout", "Placement", "Content", "Validate" };
            var rootCommand = ctx.ParseResult.RootCommandResult.Command;
            var jsonOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--format-json");
            var verboseOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--verbose");
            var json = jsonOpt != null ? ctx.ParseResult.GetValueForOption(jsonOpt) : false;
            var verbose = verboseOpt != null ? ctx.ParseResult.GetValueForOption(verboseOpt) : false;
            await CompareArtifactsAsync(runA, runB, phases, json, verbose);
        });

        AddCommand(artifactsCmd);
    }

    private static async Task CompareArtifactsAsync(
        DirectoryInfo runA,
        DirectoryInfo runB,
        string[] phases,
        bool json,
        bool verbose)
    {
        var console = json ? null : new CliConsole(verbose);

        if (!json && console != null)
        {
            console.WriteHeader("🔍 Comparing Artifacts");
            console.WritePair("Run A", runA.FullName);
            console.WritePair("Run B", runB.FullName);
            console.WriteLine();
        }

        try
        {
            if (!runA.Exists)
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = $"Run A directory not found: {runA.FullName}" }));
                }
                else if (console != null)
                {
                    console.WriteError($"Run A directory not found: {runA.FullName}");
                }
                Environment.ExitCode = 1;
                return;
            }

            if (!runB.Exists)
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = $"Run B directory not found: {runB.FullName}" }));
                }
                else if (console != null)
                {
                    console.WriteError($"Run B directory not found: {runB.FullName}");
                }
                Environment.ExitCode = 1;
                return;
            }

            var differences = new List<object>();

            foreach (var phase in phases)
            {
                var fileA = Path.Combine(runA.FullName, phase, "output.json");
                var fileB = Path.Combine(runB.FullName, phase, "output.json");

                if (!json && console != null)
                {
                    console.WriteLine($"=== Phase: {phase} ===");
                }

                if (!File.Exists(fileA) || !File.Exists(fileB))
                {
                    var missing = new List<string>();
                    if (!File.Exists(fileA)) missing.Add($"Run A: {fileA}");
                    if (!File.Exists(fileB)) missing.Add($"Run B: {fileB}");

                    if (json)
                    {
                        differences.Add(new
                        {
                            phase = phase,
                            status = "missing",
                            missing = missing
                        });
                    }
                    else if (console != null)
                    {
                        console.WriteWarning($"Missing {phase} output in one of the runs:");
                        foreach (var m in missing)
                        {
                            console.WriteLine($"  - {m}");
                        }
                    }
                    continue;
                }

                // Read and sort JSON files for comparison
                var jsonA = await File.ReadAllTextAsync(fileA);
                var jsonB = await File.ReadAllTextAsync(fileB);

                // Simple string comparison (could be enhanced with deep diff)
                if (jsonA == jsonB)
                {
                    if (!json && console != null)
                    {
                        console.WriteSuccess($"✅ {phase}: No differences");
                    }
                    else if (json)
                    {
                        differences.Add(new
                        {
                            phase = phase,
                            status = "identical"
                        });
                    }
                }
                else
                {
                    if (json)
                    {
                        differences.Add(new
                        {
                            phase = phase,
                            status = "different",
                            fileA = fileA,
                            fileB = fileB
                        });
                    }
                    else if (console != null)
                    {
                        console.WriteWarning($"⚠️  {phase}: Files differ");
                        if (verbose)
                        {
                            // Could use a JSON diff library here for better output
                            console.WriteLine($"  Run A: {fileA}");
                            console.WriteLine($"  Run B: {fileB}");
                        }
                    }
                }
            }

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    runA = runA.FullName,
                    runB = runB.FullName,
                    phases = phases,
                    differences = differences
                }, new JsonSerializerOptions { WriteIndented = true }));
            }

            Environment.ExitCode = 0;
        }
        catch (Exception ex)
        {
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            else if (console != null)
            {
                console.WriteError($"Error: {ex.Message}");
            }
            Environment.ExitCode = 1;
        }
    }
}
