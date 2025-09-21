using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Validation;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Command for validating FeatureSpec files
    /// </summary>
    public static class SpecValidationCommand
    {
        /// <summary>
        /// Create the spec validation command
        /// </summary>
        public static Command CreateSpecValidationCommand(IServiceProvider serviceProvider)
        {
            var specCommand = new Command("spec", "FeatureSpec validation commands");
            
            var validateCommand = new Command("validate", "Validate a FeatureSpec file");
            var fileArgument = new Argument<string>("file", "Path to the FeatureSpec file (.yaml, .yml, or .json)");
            var verboseOption = new Option<bool>("--verbose", "Enable verbose output") { IsRequired = false };
            
            validateCommand.AddArgument(fileArgument);
            validateCommand.AddOption(verboseOption);
            
            validateCommand.SetHandler(async (file, verbose) =>
            {
                try
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<SpecValidationCommand>>();
                    var validator = serviceProvider.GetRequiredService<FeatureSpecValidator>();
                    
                    if (verbose)
                    {
                        logger.LogInformation("Validating FeatureSpec file: {File}", file);
                    }
                    
                    if (!File.Exists(file))
                    {
                        Console.WriteLine($"❌ Error: File not found: {file}");
                        Environment.Exit(1);
                        return;
                    }
                    
                    var result = await validator.ValidateFileAsync(file);
                    
                    if (result.Passed)
                    {
                        Console.WriteLine("✅ FeatureSpec validation successful");
                        if (verbose)
                        {
                            Console.WriteLine($"Validated: {file}");
                        }
                        Environment.Exit(0);
                    }
                    else
                    {
                        Console.WriteLine("❌ FeatureSpec validation failed:");
                        foreach (var finding in result.Findings)
                        {
                            Console.WriteLine($"  - {finding}");
                        }
                        Environment.Exit(1);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error during validation: {ex.Message}");
                    if (verbose)
                    {
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                    Environment.Exit(1);
                }
            }, fileArgument, verboseOption);
            
            specCommand.AddCommand(validateCommand);
            return specCommand;
        }
    }
}
