using System;
using System.CommandLine;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands.Project.Handlers
{
    public partial class ProjectBuildHandler
    {
        private readonly ILogger _logger;

        public ProjectBuildHandler(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Command CreateBuildCommand()
        {
            var buildCommand = new Command("build", "Build the project");
            var buildConfigOption = new Option<string>("--config", "Build configuration (Debug, Release)") { IsRequired = false };
            var buildOutputOption = new Option<string>("--output", "Output directory") { IsRequired = false };
            var buildVerboseOption = new Option<bool>("--verbose", "Verbose output") { IsRequired = false };
            
            buildCommand.AddOption(buildConfigOption);
            buildCommand.AddOption(buildOutputOption);
            buildCommand.AddOption(buildVerboseOption);
            
            buildCommand.SetHandler(async (config, output, verbose) =>
            {
                await HandleBuildAsync(config, output, verbose);
            }, buildConfigOption, buildOutputOption, buildVerboseOption);
            
            return buildCommand;
        }

        private async Task HandleBuildAsync(string config, string output, bool verbose)
        {
            try
            {
                _logger.LogInformation("Building project with configuration: {Config}", config ?? "Debug");
                
                var configValue = config ?? "Debug";
                var outputValue = output ?? "bin";
                
                Console.WriteLine($"Building project in {configValue} configuration...");
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build --configuration {configValue} --output {outputValue}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                
                if (verbose)
                {
                    process.StartInfo.Arguments += " --verbosity normal";
                }
                
                process.Start();
                
                var outputText = await process.StandardOutput.ReadToEndAsync();
                var errorText = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Build completed successfully");
                    if (verbose && !string.IsNullOrEmpty(outputText))
                    {
                        Console.WriteLine(outputText);
                    }
                }
                else
                {
                    Console.WriteLine($"Build failed with exit code {process.ExitCode}");
                    if (!string.IsNullOrEmpty(errorText))
                    {
                        Console.WriteLine(errorText);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building project");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
