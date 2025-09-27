using System;
using System.CommandLine;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.CLI.Commands.Project.Handlers
{
    public partial class ProjectTestHandler
    {
        private readonly ILogger _logger;

        public ProjectTestHandler(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Command CreateTestCommand()
        {
            var testCommand = new Command("test", "Run project tests");
            var testFilterOption = new Option<string>("--filter", "Test filter expression") { IsRequired = false };
            var testLoggerOption = new Option<string>("--logger", "Test logger (console, trx)") { IsRequired = false };
            var testCollectOption = new Option<string>("--collect", "Data collector (coverage)") { IsRequired = false };
            var testVerboseOption = new Option<bool>("--verbose", "Verbose output") { IsRequired = false };
            
            testCommand.AddOption(testFilterOption);
            testCommand.AddOption(testLoggerOption);
            testCommand.AddOption(testCollectOption);
            testCommand.AddOption(testVerboseOption);
            
            testCommand.SetHandler(async (filter, logger, collect, verbose) =>
            {
                await HandleTestAsync(filter, logger, collect, verbose);
            }, testFilterOption, testLoggerOption, testCollectOption, testVerboseOption);
            
            return testCommand;
        }

        private async Task HandleTestAsync(string filter, string logger, string collect, bool verbose)
        {
            try
            {
                _logger.LogInformation("Running project tests");
                
                Console.WriteLine("Running tests...");
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "test",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                
                if (!string.IsNullOrEmpty(filter))
                {
                    process.StartInfo.Arguments += $" --filter \"{filter}\"";
                }
                
                if (!string.IsNullOrEmpty(logger))
                {
                    process.StartInfo.Arguments += $" --logger {logger}";
                }
                
                if (!string.IsNullOrEmpty(collect))
                {
                    process.StartInfo.Arguments += $" --collect {collect}";
                }
                
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
                    Console.WriteLine("All tests passed");
                    if (verbose && !string.IsNullOrEmpty(outputText))
                    {
                        Console.WriteLine(outputText);
                    }
                }
                else
                {
                    Console.WriteLine($"Tests failed with exit code {process.ExitCode}");
                    if (!string.IsNullOrEmpty(errorText))
                    {
                        Console.WriteLine(errorText);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running tests");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
