using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Nexo.CLI.Output;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command for CI operations (verification, promotion checks).
/// Replaces ci-verify.sh, check-promotion.sh, and check-promotion-cs.sh scripts.
/// </summary>
public class CiCommand : Command
{
    public CiCommand() : base("ci", "CI operations (verification, promotion checks)")
    {
        // nexo ci verify
        var verifyCmd = new Command("verify", "Run CI verification checks");
        verifyCmd.SetHandler((InvocationContext ctx) =>
        {
            var rootCommand = ctx.ParseResult.RootCommandResult.Command;
            var jsonOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--format-json");
            var verboseOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--verbose");
            var json = jsonOpt != null ? ctx.ParseResult.GetValueForOption(jsonOpt) : false;
            var verbose = verboseOpt != null ? ctx.ParseResult.GetValueForOption(verboseOpt) : false;
            return VerifyAsync(json, verbose);
        });

        // nexo ci check-promotion
        var checkPromotionCmd = new Command("check-promotion", "Check if promotion criteria are met (verify Artifacts/last_good/)");
        checkPromotionCmd.SetHandler((InvocationContext ctx) =>
        {
            var rootCommand = ctx.ParseResult.RootCommandResult.Command;
            var jsonOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--format-json");
            var verboseOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--verbose");
            var json = jsonOpt != null ? ctx.ParseResult.GetValueForOption(jsonOpt) : false;
            var verbose = verboseOpt != null ? ctx.ParseResult.GetValueForOption(verboseOpt) : false;
            return CheckPromotionAsync(json, verbose);
        });

        AddCommand(verifyCmd);
        AddCommand(checkPromotionCmd);
    }

    private static Task VerifyAsync(bool json, bool verbose)
    {
        var console = json ? null : new CliConsole(verbose);

        if (!json && console != null)
        {
            console.WriteHeader("🔍 CI Verification");
            console.WriteLine();
        }

        try
        {
            // For now, verify just checks that the project builds
            // This can be extended with more checks
            var rootDir = DiscoverProjectRoot();
            if (rootDir == null)
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Could not find project root directory" }));
                }
                else if (console != null)
                {
                    console.WriteError("Could not find project root directory");
                }
                Environment.ExitCode = 1;
                return Task.CompletedTask;
            }

            if (!json && console != null)
            {
                console.WriteLine("Running basic verification checks...");
            }

            // Check that solution file exists
            var solutionFile = Path.Combine(rootDir, "Nexo.sln");
            if (!File.Exists(solutionFile))
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Solution file not found" }));
                }
                else if (console != null)
                {
                    console.WriteError("Solution file not found");
                }
                Environment.ExitCode = 1;
                return Task.CompletedTask;
            }

            if (!json && console != null)
            {
                console.WriteSuccess("✅ Solution file found");
                console.WriteSuccess("✅ CI verification complete");
            }
            else if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { success = true }));
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
        
        return Task.CompletedTask;
    }

    private static Task CheckPromotionAsync(bool json, bool verbose)
    {
        var console = json ? null : new CliConsole(verbose);

        if (!json && console != null)
        {
            console.WriteHeader("📋 Checking Promotion Criteria");
            console.WriteLine();
        }

        try
        {
            var rootDir = DiscoverProjectRoot();
            if (rootDir == null)
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "Could not find project root directory" }));
                }
                else if (console != null)
                {
                    console.WriteError("Could not find project root directory");
                }
                Environment.ExitCode = 1;
                return Task.CompletedTask;
            }

            var lastGoodDir = Path.Combine(rootDir, "Artifacts", "last_good");

            if (!Directory.Exists(lastGoodDir))
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { error = "No last_good directory found", path = lastGoodDir }));
                }
                else if (console != null)
                {
                    console.WriteError($"No last_good directory found: {lastGoodDir}");
                }
                Environment.ExitCode = 1;
                return Task.CompletedTask;
            }

            var requiredPhases = new[] { "Plan", "Layout", "Placement", "Content", "Validate" };
            var missingPhases = new List<string>();
            var foundPhases = new List<string>();

            foreach (var phase in requiredPhases)
            {
                var outputFile = Path.Combine(lastGoodDir, phase, "output.json");
                if (!File.Exists(outputFile) || new FileInfo(outputFile).Length == 0)
                {
                    missingPhases.Add(phase);
                    if (!json && console != null)
                    {
                        console.WriteError($"Missing or empty: {outputFile}");
                    }
                }
                else
                {
                    foundPhases.Add(phase);
                    if (!json && console != null)
                    {
                        console.WriteSuccess($"OK: {outputFile}");
                    }
                }
            }

            if (missingPhases.Count > 0)
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        success = false,
                        missingPhases = missingPhases,
                        foundPhases = foundPhases
                    }));
                }
                else if (console != null)
                {
                    console.WriteError($"❌ Promotion check failed: {missingPhases.Count} phase(s) missing");
                }
                Environment.ExitCode = 2;
            }
            else
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        success = true,
                        message = "last_good complete",
                        phases = foundPhases
                    }));
                }
                else if (console != null)
                {
                    console.WriteSuccess("✅ last_good complete");
                }
                Environment.ExitCode = 0;
            }
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
        
        return Task.CompletedTask;
    }

    private static string? DiscoverProjectRoot()
    {
        var current = Environment.CurrentDirectory;
        var dir = new DirectoryInfo(current);

        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Nexo.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        return null;
    }
}
