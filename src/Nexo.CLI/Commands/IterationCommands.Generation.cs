using System;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Services.Iteration;
using Nexo.Core.Domain.Entities.Iteration;

namespace Nexo.CLI.Commands;

/// <summary>
/// Code generation command functionality
/// </summary>
public partial class IterationCommands
{
    /// <summary>
    /// Create the iteration generate command
    /// </summary>
    public Command CreateIterationGenerateCommand()
    {
        var command = new Command("generate", "Generate optimized iteration code");
        
        var descriptionOption = new Option<string>(
            name: "--description",
            description: "Description of the iteration requirements");
        descriptionOption.IsRequired = true;
        command.AddOption(descriptionOption);
        
        var platformOption = new Option<string>(
            name: "--platform",
            description: "Target platform for code generation");
        platformOption.SetDefaultValue("auto");
        command.AddOption(platformOption);
        
        var dataSizeOption = new Option<int>(
            name: "--data-size",
            description: "Estimated data size");
        dataSizeOption.SetDefaultValue(1000);
        command.AddOption(dataSizeOption);
        
        var outputOption = new Option<string>(
            name: "--output",
            description: "Output file path (optional)");
        command.AddOption(outputOption);
        
        command.SetHandler(async (string description, string platform, int dataSize, string? output) =>
        {
            await GenerateOptimizedIteration(description, platform, dataSize, output);
        }, descriptionOption, platformOption, dataSizeOption, outputOption);
        
        return command;
    }

    private async Task GenerateOptimizedIteration(string description, string platform, int dataSize, string? output)
    {
        try
        {
            Console.WriteLine("Running Generating optimized iteration code...");
            Console.WriteLine("=========================================");
            
            var generator = _serviceProvider.GetRequiredService<IIterationCodeGenerator>();
            
            var request = new IterationCodeRequest
            {
                Description = description,
                TargetPlatform = ParsePlatform(platform),
                EstimatedDataSize = dataSize
            };
            
            var code = await generator.GenerateOptimalIterationAsync(request);
            
            Console.WriteLine("Generated Code:");
            Console.WriteLine("--------------");
            Console.WriteLine(code);
            
            if (!string.IsNullOrEmpty(output))
            {
                await System.IO.File.WriteAllTextAsync(output, code);
                Console.WriteLine($"SUCCESS: Code saved to: {output}");
            }
            
            Console.WriteLine("SUCCESS: Code generation completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating iteration code");
            Console.WriteLine($"ERROR: Error generating iteration code: {ex.Message}");
        }
    }
}
