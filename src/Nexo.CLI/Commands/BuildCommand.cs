using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Text.Json;
using Nexo.CLI.Output;

namespace Nexo.CLI.Commands;

/// <summary>
/// Command for building portable (netstandard2.0) targets.
/// Replaces build-portable.sh script.
/// </summary>
public class BuildCommand : Command
{
    public BuildCommand() : base("build", "Build portable (netstandard2.0) targets")
    {
        var portableOption = new Option<bool>(
            "--portable",
            () => false,
            "Build portable (netstandard2.0) targets"
        );

        var configurationOption = new Option<string>(
            "--configuration",
            () => "Release",
            "Build configuration (Debug, Release)"
        );

        var frameworkOption = new Option<string>(
            "--framework",
            () => "netstandard2.0",
            "Target framework"
        );

        var verbosityOption = new Option<string>(
            "--verbosity",
            () => "minimal",
            "MSBuild verbosity level (quiet, minimal, normal, detailed, diagnostic)"
        );

        AddOption(portableOption);
        AddOption(configurationOption);
        AddOption(frameworkOption);
        AddOption(verbosityOption);

        this.SetHandler(async (InvocationContext ctx) =>
        {
            var portable = ctx.ParseResult.GetValueForOption(portableOption);
            var configuration = ctx.ParseResult.GetValueForOption(configurationOption) ?? "Release";
            var framework = ctx.ParseResult.GetValueForOption(frameworkOption) ?? "netstandard2.0";
            var verbosity = ctx.ParseResult.GetValueForOption(verbosityOption) ?? "minimal";
            
            // Get global options from root command
            var rootCommand = ctx.ParseResult.RootCommandResult.Command;
            var jsonOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--format-json");
            var verboseOpt = rootCommand.Options.OfType<Option<bool>>().FirstOrDefault(o => o.Name == "--verbose");
            var json = jsonOpt != null ? ctx.ParseResult.GetValueForOption(jsonOpt) : false;
            var verbose = verboseOpt != null ? ctx.ParseResult.GetValueForOption(verboseOpt) : false;

            await ExecuteAsync(portable, configuration, framework, verbosity, json, verbose);
        });
    }

    private static async Task ExecuteAsync(
        bool portable,
        string configuration,
        string framework,
        string verbosity,
        bool json,
        bool verbose)
    {
        var console = json ? null : new CliConsole(verbose);

        if (!json && console != null)
        {
            console.WriteHeader("🔨 Building Portable Targets");
            console.WritePair("Configuration", configuration);
            console.WritePair("Framework", framework);
            console.WritePair("Verbosity", verbosity);
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
                return;
            }

            var projects = new[]
            {
                Path.Combine(rootDir, "src", "Nexo.Abstractions", "Nexo.Abstractions.csproj"),
                Path.Combine(rootDir, "src", "Nexo.Core.Domain", "Nexo.Core.Domain.csproj"),
                Path.Combine(rootDir, "src", "Nexo.Core", "Nexo.Core.csproj"),
                Path.Combine(rootDir, "src", "Nexo.Core.Application", "Nexo.Core.Application.csproj"),
                Path.Combine(rootDir, "src", "Nexo.Policies", "Nexo.Policies.csproj"),
                Path.Combine(rootDir, "src", "Nexo.Runtime", "Nexo.Runtime.csproj"),
                Path.Combine(rootDir, "src", "Nexo.Adapters.Models", "Nexo.Adapters.Models.csproj"),
                Path.Combine(rootDir, "src", "Nexo.Core.UI", "Nexo.Core.UI.csproj"),
                Path.Combine(rootDir, "src", "Nexo.Core.UI.Unity", "Nexo.Core.UI.Unity.csproj")
            };

            var successCount = 0;
            var failCount = 0;

            foreach (var project in projects)
            {
                if (!File.Exists(project))
                {
                    if (!json && console != null)
                    {
                        console.WriteWarning($"Project not found: {project}");
                    }
                    continue;
                }

                if (!json && console != null)
                {
                    console.WriteLine($"==> {Path.GetFileName(project)}");
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{project}\" -c {configuration} -f {framework} -v {verbosity}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processStartInfo);
                if (process == null)
                {
                    if (json)
                    {
                        Console.WriteLine(JsonSerializer.Serialize(new { error = $"Failed to start build process for {project}" }));
                    }
                    else if (console != null)
                    {
                        console.WriteError($"Failed to start build process for {project}");
                    }
                    failCount++;
                    continue;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    successCount++;
                    if (verbose && !json && console != null && !string.IsNullOrWhiteSpace(output))
                    {
                        console.WriteLine(output);
                    }
                }
                else
                {
                    failCount++;
                    if (json)
                    {
                        Console.WriteLine(JsonSerializer.Serialize(new
                        {
                            project = Path.GetFileName(project),
                            exitCode = process.ExitCode,
                            error = error,
                            output = output
                        }));
                    }
                    else if (console != null)
                    {
                        console.WriteError($"Build failed for {Path.GetFileName(project)}");
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            console.WriteLine(error);
                        }
                    }
                }
            }

            if (!json && console != null)
            {
                console.WriteLine();
                if (failCount == 0)
                {
                    console.WriteSuccess($"✅ Portable build complete ({successCount} projects)");
                }
                else
                {
                    console.WriteError($"❌ Build completed with {failCount} failure(s) ({successCount} succeeded)");
                }
            }
            else if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = failCount == 0,
                    succeeded = successCount,
                    failed = failCount,
                    total = successCount + failCount
                }));
            }

            Environment.ExitCode = failCount == 0 ? 0 : 1;
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
