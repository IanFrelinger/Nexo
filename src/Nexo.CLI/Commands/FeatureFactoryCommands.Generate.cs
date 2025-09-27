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
    /// Generate command functionality for FeatureFactoryCommands.
    /// </summary>
    public static partial class FeatureFactoryCommands
    {
        /// <summary>
        /// Creates the generate command
        /// </summary>
        private static Command CreateGenerateCommand(IServiceProvider serviceProvider, ILogger logger)
        {
            var generateCommand = new Command("generate", "Generate a feature from natural language description");
            var generateDescriptionOption = new Option<string>("--description", "Natural language description of the feature") { IsRequired = true };
            var generatePlatformOption = new Option<TargetPlatform>("--platform", () => TargetPlatform.DotNet, "Target platform for code generation");
            var generateOutputOption = new Option<string>("--output", "Output directory for generated code");
            var generateVerboseOption = new Option<bool>("--verbose", "Enable verbose output");
            var generateUseLocalOption = new Option<bool>("--use-local-models", "Use local Ollama models instead of cloud providers");
            var generateLocalModelOption = new Option<string>("--local-model", () => "llama3.2:latest", "Local model to use (e.g., llama3.2:latest)");
            var generateLocalEndpointOption = new Option<string>("--local-endpoint", () => "http://localhost:11434", "Local Ollama endpoint");
            
            generateCommand.AddOption(generateDescriptionOption);
            generateCommand.AddOption(generatePlatformOption);
            generateCommand.AddOption(generateOutputOption);
            generateCommand.AddOption(generateVerboseOption);
            generateCommand.AddOption(generateUseLocalOption);
            generateCommand.AddOption(generateLocalModelOption);
            generateCommand.AddOption(generateLocalEndpointOption);
            
            generateCommand.SetHandler(async (description, platform, output, verbose, useLocal, localModel, localEndpoint) =>
            {
                try
                {
                    await HandleGenerateCommand(serviceProvider, logger, description, platform, output, verbose, useLocal, localModel, localEndpoint);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error generating feature");
                    Console.WriteLine($"ERROR: Error: {ex.Message}");
                    Environment.Exit(1);
                }
            }, generateDescriptionOption, generatePlatformOption, generateOutputOption, generateVerboseOption, generateUseLocalOption, generateLocalModelOption, generateLocalEndpointOption);
            
            return generateCommand;
        }

        /// <summary>
        /// Handles the generate command
        /// </summary>
        private static async Task HandleGenerateCommand(
            IServiceProvider serviceProvider, 
            ILogger logger, 
            string description, 
            TargetPlatform platform, 
            string output, 
            bool verbose,
            bool useLocal,
            string localModel,
            string localEndpoint)
        {
            logger.LogInformation("Starting feature generation: {Description}", description);
            
            Console.WriteLine("Running AI-Native Feature Factory");
            Console.WriteLine("=============================");
            Console.WriteLine($"Description: {description}");
            Console.WriteLine($"Platform: {platform}");
            
            // Configure local model settings if requested
            if (useLocal)
            {
                Console.WriteLine($"AI Using Local Model: {localModel}");
                Console.WriteLine($"🔗 Local Endpoint: {localEndpoint}");
                
                // Set environment variables for local model usage
                Environment.SetEnvironmentVariable("NEXO_AI_PROVIDER", "ollama");
                Environment.SetEnvironmentVariable("NEXO_AI_MODEL", localModel);
                Environment.SetEnvironmentVariable("NEXO_AI_ENDPOINT", localEndpoint);
                Environment.SetEnvironmentVariable("NEXO_USE_LOCAL_MODELS", "true");
                
                // Test local model connection
                try
                {
                    using var httpClient = new System.Net.Http.HttpClient();
                    var testResponse = await httpClient.GetAsync($"{localEndpoint}/api/tags");
                    if (testResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine("SUCCESS: Local model connection verified");
                    }
                    else
                    {
                        Console.WriteLine("WARNING:  Local model connection failed, but continuing...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WARNING:  Could not verify local model connection: {ex.Message}");
                }
            }
            
            Console.WriteLine();
            
            var orchestrator = serviceProvider.GetRequiredService<IFeatureOrchestrator>();
            
            // Step 1: Generate the feature
            Console.WriteLine("List Analyzing feature requirements...");
            var result = await orchestrator.GenerateFeatureAsync(description, platform, CancellationToken.None);
            
            if (!result.IsSuccess)
            {
                Console.WriteLine("ERROR: Feature generation failed:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   • {error}");
                }
                return;
            }
            
            // Step 2: Display results
            Console.WriteLine("SUCCESS: Feature generation completed successfully!");
            Console.WriteLine();
            Console.WriteLine("Stats Generation Summary:");
            Console.WriteLine($"   • Duration: {result.Metadata.Duration.TotalSeconds:F2} seconds");
            Console.WriteLine($"   • Agents Used: {result.Metadata.AgentsUsed}");
            Console.WriteLine($"   • Execution Strategy: {result.Metadata.ExecutionStrategy}");
            Console.WriteLine($"   • Artifacts Generated: {result.CodeArtifacts.Count}");
            Console.WriteLine();
            
            // Step 3: Display specification details
            if (verbose && result.Specification != null)
            {
                Console.WriteLine("List Feature Specification:");
                Console.WriteLine($"   • Entities: {result.Specification.Entities.Count}");
                Console.WriteLine($"   • Value Objects: {result.Specification.ValueObjects.Count}");
                Console.WriteLine($"   • Business Rules: {result.Specification.BusinessRules.Count}");
                Console.WriteLine($"   • Validation Rules: {result.Specification.ValidationRules.Count}");
                Console.WriteLine();
                
                foreach (var entity in result.Specification.Entities)
                {
                    Console.WriteLine($"   Package Entity: {entity.Name}");
                    Console.WriteLine($"      Description: {entity.Description}");
                    Console.WriteLine($"      Properties: {entity.Properties.Count}");
                    Console.WriteLine($"      CRUD Operations: {entity.IncludeCrudOperations}");
                    Console.WriteLine();
                }
            }
            
            // Step 4: Display generated artifacts
            Console.WriteLine("Directory Generated Artifacts:");
            foreach (var artifact in result.CodeArtifacts)
            {
                Console.WriteLine($"   • {artifact.Name} ({artifact.Type})");
                Console.WriteLine($"     Path: {artifact.FilePath}");
                Console.WriteLine($"     Namespace: {artifact.Namespace}");
                Console.WriteLine();
            }
            
            // Step 5: Save artifacts to output directory
            if (!string.IsNullOrEmpty(output))
            {
                await SaveArtifactsToDirectory(result.CodeArtifacts, output, logger);
                Console.WriteLine($"💾 Artifacts saved to: {output}");
            }
            else
            {
                Console.WriteLine("Idea Use --output <directory> to save generated code to files");
            }
            
            Console.WriteLine();
            Console.WriteLine("SUCCESS Feature generation completed successfully!");
        }
    }
}
