using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Application.Interfaces;
using Nexo.Feature.Factory.Domain.Enums;
using Nexo.Feature.Factory.Domain.Models;
using Nexo.Feature.Factory.Domain.Entities;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Analyze command functionality for FeatureFactoryCommands.
    /// </summary>
    public static partial class FeatureFactoryCommands
    {
        /// <summary>
        /// Creates the analyze command
        /// </summary>
        private static Command CreateAnalyzeCommand(IServiceProvider serviceProvider, ILogger logger)
        {
            var analyzeCommand = new Command("analyze", "Analyze a natural language description and create a feature specification");
            var analyzeDescriptionOption = new Option<string>("--description", "Natural language description of the feature") { IsRequired = true };
            var analyzePlatformOption = new Option<TargetPlatform>("--platform", () => TargetPlatform.DotNet, "Target platform for analysis");
            var analyzeOutputOption = new Option<string>("--output", "Output file for the feature specification (JSON)");
            
            analyzeCommand.AddOption(analyzeDescriptionOption);
            analyzeCommand.AddOption(analyzePlatformOption);
            analyzeCommand.AddOption(analyzeOutputOption);
            
            analyzeCommand.SetHandler(async (description, platform, output) =>
            {
                try
                {
                    await HandleAnalyzeCommand(serviceProvider, logger, description, platform, output);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error analyzing feature");
                    Console.WriteLine($"ERROR: Error: {ex.Message}");
                    Environment.Exit(1);
                }
            }, analyzeDescriptionOption, analyzePlatformOption, analyzeOutputOption);
            
            return analyzeCommand;
        }

        /// <summary>
        /// Handles the analyze command
        /// </summary>
        private static async Task HandleAnalyzeCommand(
            IServiceProvider serviceProvider, 
            ILogger logger, 
            string description, 
            TargetPlatform platform, 
            string output)
        {
            logger.LogInformation("Starting feature analysis: {Description}", description);
            
            Console.WriteLine("Search Feature Analysis");
            Console.WriteLine("===================");
            Console.WriteLine($"Description: {description}");
            Console.WriteLine($"Platform: {platform}");
            Console.WriteLine();
            
            var orchestrator = serviceProvider.GetRequiredService<IFeatureOrchestrator>();
            
            // Analyze the feature
            Console.WriteLine("List Analyzing feature requirements...");
            var specification = await orchestrator.AnalyzeFeatureAsync(description, platform, CancellationToken.None);
            
            // Display results
            Console.WriteLine("SUCCESS: Feature analysis completed!");
            Console.WriteLine();
            Console.WriteLine("Stats Analysis Summary:");
            Console.WriteLine($"   • Specification ID: {specification.Id}");
            Console.WriteLine($"   • Status: {specification.Status}");
            Console.WriteLine($"   • Entities: {specification.Entities.Count}");
            Console.WriteLine($"   • Value Objects: {specification.ValueObjects.Count}");
            Console.WriteLine($"   • Business Rules: {specification.BusinessRules.Count}");
            Console.WriteLine($"   • Validation Rules: {specification.ValidationRules.Count}");
            Console.WriteLine();
            
            // Display detailed analysis
            if (specification.Entities.Count > 0)
            {
                Console.WriteLine("Package Entities:");
                foreach (var entity in specification.Entities)
                {
                    Console.WriteLine($"   • {entity.Name}");
                    Console.WriteLine($"     Description: {entity.Description}");
                    Console.WriteLine($"     Namespace: {entity.Namespace}");
                    Console.WriteLine($"     Properties: {entity.Properties.Count}");
                    Console.WriteLine($"     CRUD Operations: {entity.IncludeCrudOperations}");
                    Console.WriteLine($"     Validation: {entity.IncludeValidation}");
                    
                    if (entity.Properties.Count > 0)
                    {
                        Console.WriteLine("     Properties:");
                        foreach (var property in entity.Properties)
                        {
                            Console.WriteLine($"       - {property.Name} ({property.Type}): {property.Description}");
                            if (property.IsRequired) Console.WriteLine("         Required: Yes");
                            if (property.IsUnique) Console.WriteLine("         Unique: Yes");
                        }
                    }
                    Console.WriteLine();
                }
            }
            
            if (specification.ValueObjects.Count > 0)
            {
                Console.WriteLine("Tool Value Objects:");
                foreach (var valueObject in specification.ValueObjects)
                {
                    Console.WriteLine($"   • {valueObject.Name}");
                    Console.WriteLine($"     Description: {valueObject.Description}");
                    Console.WriteLine($"     Namespace: {valueObject.Namespace}");
                    Console.WriteLine($"     Properties: {valueObject.Properties.Count}");
                    Console.WriteLine();
                }
            }
            
            if (specification.BusinessRules.Count > 0)
            {
                Console.WriteLine("List Business Rules:");
                foreach (var rule in specification.BusinessRules)
                {
                    Console.WriteLine($"   • {rule.Name}");
                    Console.WriteLine($"     Description: {rule.Description}");
                    Console.WriteLine($"     Condition: {rule.Condition}");
                    Console.WriteLine($"     Action: {rule.Action}");
                    Console.WriteLine($"     Priority: {rule.Priority}");
                    if (!string.IsNullOrEmpty(rule.AppliesTo))
                        Console.WriteLine($"     Applies To: {rule.AppliesTo}");
                    Console.WriteLine();
                }
            }
            
            if (specification.ValidationRules.Count > 0)
            {
                Console.WriteLine("SUCCESS: Validation Rules:");
                foreach (var rule in specification.ValidationRules)
                {
                    Console.WriteLine($"   • {rule.Name}");
                    Console.WriteLine($"     Description: {rule.Description}");
                    Console.WriteLine($"     Type: {rule.Type}");
                    Console.WriteLine($"     Expression: {rule.Expression}");
                    Console.WriteLine($"     Error Message: {rule.ErrorMessage}");
                    Console.WriteLine($"     Severity: {rule.Severity}");
                    if (!string.IsNullOrEmpty(rule.AppliesTo))
                        Console.WriteLine($"     Applies To: {rule.AppliesTo}");
                    Console.WriteLine();
                }
            }
            
            // Save specification to file if output is specified
            if (!string.IsNullOrEmpty(output))
            {
                await SaveSpecificationToFile(specification, output, logger);
                Console.WriteLine($"💾 Specification saved to: {output}");
            }
            else
            {
                Console.WriteLine("Idea Use --output <file> to save the specification to a JSON file");
            }
            
            Console.WriteLine();
            Console.WriteLine("SUCCESS Feature analysis completed successfully!");
        }
    }
}
