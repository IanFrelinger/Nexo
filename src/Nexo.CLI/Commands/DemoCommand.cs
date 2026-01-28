using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Nexo.Infrastructure.Execution;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command handler for demo operations showcasing Universal Testing Agent and Autonomous Development Agent.
/// </summary>
public class DemoCommand
{
    /// <summary>
    /// Creates the demo command group with all subcommands.
    /// </summary>
    public static Command CreateCommand(IServiceProvider serviceProvider, Option<bool> jsonOpt, Option<bool> verboseOpt)
    {
        var demoCmd = new Command("demo", "Demo operations for Universal Testing and Autonomous Development");
        
        // Use the new command classes
        demoCmd.AddCommand(new UniversalTestCommand());
        demoCmd.AddCommand(new AutonomousDevCommand());
        demoCmd.AddCommand(new DemoSelfExtendCommand());
        
        // nexo demo smoke-test
        var smokeTestCmd = new Command("smoke-test", "Run smoke tests to validate demo scripts for portability");
        smokeTestCmd.SetHandler(async (InvocationContext ctx) =>
        {
            var rootCommand = ctx.ParseResult.RootCommandResult.Command;
            var jsonOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--format-json");
            var verboseOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--verbose");
            var json = jsonOpt != null ? ctx.ParseResult.GetValueForOption(jsonOpt) : false;
            var verbose = verboseOpt != null ? ctx.ParseResult.GetValueForOption(verboseOpt) : false;
            await RunSmokeTestAsync(json, verbose);
        });
        demoCmd.AddCommand(smokeTestCmd);

        // Auto-register demo-generated commands if any are present.
        // These are created by `nexo demo self-extend` and are intentionally ignored by git.
        foreach (var type in typeof(DemoCommand).Assembly.GetTypes())
        {
            if (type.IsAbstract) continue;
            if (type.Namespace != "Nexo.CLI.Commands.DemoGenerated") continue;
            if (!typeof(Command).IsAssignableFrom(type)) continue;

            try
            {
                if (Activator.CreateInstance(type) is Command cmd)
                {
                    demoCmd.AddCommand(cmd);
                }
            }
            catch
            {
                // ignore broken demo-generated commands
            }
        }

        return demoCmd;
    }

    private static async Task RunSmokeTestAsync(bool json, bool verbose)
    {
        var console = json ? null : new Nexo.CLI.Output.CliConsole(verbose);
        
        if (!json && console != null)
        {
            console.WriteHeader("🔍 Demo Script Smoke Test");
            console.WriteLine();
        }

        try
        {
            var rootDir = DiscoverProjectRoot();
            if (rootDir == null)
            {
                if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { error = "Could not find project root directory" }));
                }
                else if (console != null)
                {
                    console.WriteError("Could not find project root directory");
                }
                Environment.ExitCode = 1;
                return;
            }

            var scriptsDir = Path.Combine(rootDir, "scripts");
            if (!Directory.Exists(scriptsDir))
            {
                if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { error = "Scripts directory not found" }));
                }
                else if (console != null)
                {
                    console.WriteError("Scripts directory not found");
                }
                Environment.ExitCode = 1;
                return;
            }

            var errors = 0;
            var warnings = 0;
            var unityBinCount = 0;
            var scriptDirCount = 0;

            // Check Unity-related scripts
            var unityScripts = Directory.GetFiles(scriptsDir, "*unity*.sh", SearchOption.TopDirectoryOnly);

            if (!json && console != null)
            {
                console.WriteLine("Checking for hardcoded user paths...");
                console.WriteLine();
            }

            foreach (var scriptPath in unityScripts)
            {
                var scriptName = Path.GetFileName(scriptPath);
                var content = await File.ReadAllTextAsync(scriptPath);

                // Check for hardcoded user paths
                if (content.Contains("/Users/") && content.Contains("ianfrelinger"))
                {
                    errors++;
                    if (!json && console != null)
                    {
                        console.WriteError($"❌ FAIL: {scriptName} contains hardcoded user path");
                    }
                }
                else
                {
                    if (!json && console != null && verbose)
                    {
                        console.WriteSuccess($"✅ PASS: {scriptName} - No hardcoded user paths");
                    }
                }

                // Check for UNITY_BIN support
                if (content.Contains("UNITY_BIN") || content.Contains("${UNITY_BIN"))
                {
                    unityBinCount++;
                }

                // Check for SCRIPT_DIR pattern
                if (content.Contains("SCRIPT_DIR="))
                {
                    scriptDirCount++;
                }
            }

            if (!json && console != null)
            {
                console.WriteLine();
                console.WriteLine("Checking for UNITY_BIN environment variable support...");
                console.WritePair("Scripts with UNITY_BIN support", unityBinCount.ToString());
                console.WriteLine();
                console.WriteLine("Checking for portable path detection pattern...");
                console.WritePair("Scripts using SCRIPT_DIR pattern", scriptDirCount.ToString());
                console.WriteLine();
                console.WriteHeader("Summary");
                console.WritePair("Scripts checked", unityScripts.Length.ToString());
                console.WritePair("Scripts with UNITY_BIN support", unityBinCount.ToString());
                console.WritePair("Scripts with SCRIPT_DIR pattern", scriptDirCount.ToString());
                console.WritePair("Errors (hardcoded paths)", errors.ToString());
                console.WritePair("Warnings", warnings.ToString());
                console.WriteLine();
            }

            if (errors == 0)
            {
                if (!json && console != null)
                {
                    console.WriteSuccess("✅ SUCCESS: No hardcoded user paths found");
                }
                else if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        success = true,
                        scriptsChecked = unityScripts.Length,
                        unityBinSupport = unityBinCount,
                        scriptDirPattern = scriptDirCount,
                        errors = errors,
                        warnings = warnings
                    }));
                }
                Environment.ExitCode = 0;
            }
            else
            {
                if (!json && console != null)
                {
                    console.WriteError($"❌ FAILURE: Found {errors} script(s) with hardcoded paths");
                }
                else if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        success = false,
                        scriptsChecked = unityScripts.Length,
                        errors = errors,
                        warnings = warnings
                    }));
                }
                Environment.ExitCode = 1;
            }
        }
        catch (Exception ex)
        {
            if (json)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message }));
            }
            else if (console != null)
            {
                console.WriteError($"Error: {ex.Message}");
            }
            Environment.ExitCode = 1;
        }
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
