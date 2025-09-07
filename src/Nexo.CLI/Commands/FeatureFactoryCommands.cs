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
    /// CLI commands for the AI-native feature factory.
    /// </summary>
    public static class FeatureFactoryCommands
    {
        /// <summary>
        /// Creates the feature factory command structure.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency injection</param>
        /// <param name="logger">The logger instance</param>
        /// <returns>The configured command</returns>
        public static Command CreateFeatureFactoryCommand(IServiceProvider serviceProvider, ILogger logger)
        {
            var featureFactoryCommand = new Command("feature", "AI-native feature factory commands");
            
            // Generate command
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
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    Environment.Exit(1);
                }
            }, generateDescriptionOption, generatePlatformOption, generateOutputOption, generateVerboseOption, generateUseLocalOption, generateLocalModelOption, generateLocalEndpointOption);
            
            featureFactoryCommand.AddCommand(generateCommand);
            
            // Analyze command
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
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    Environment.Exit(1);
                }
            }, analyzeDescriptionOption, analyzePlatformOption, analyzeOutputOption);
            
            featureFactoryCommand.AddCommand(analyzeCommand);
            
            return featureFactoryCommand;
        }
        
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
            
            Console.WriteLine("🚀 AI-Native Feature Factory");
            Console.WriteLine("=============================");
            Console.WriteLine($"Description: {description}");
            Console.WriteLine($"Platform: {platform}");
            
            // Configure local model settings if requested
            if (useLocal)
            {
                Console.WriteLine($"🤖 Using Local Model: {localModel}");
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
                        Console.WriteLine("✅ Local model connection verified");
                    }
                    else
                    {
                        Console.WriteLine("⚠️  Local model connection failed, but continuing...");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Could not verify local model connection: {ex.Message}");
                }
            }
            
            Console.WriteLine();
            
            var orchestrator = serviceProvider.GetRequiredService<IFeatureOrchestrator>();
            
            // Step 1: Generate the feature
            Console.WriteLine("📋 Analyzing feature requirements...");
            var result = await orchestrator.GenerateFeatureAsync(description, platform, CancellationToken.None);
            
            if (!result.IsSuccess)
            {
                Console.WriteLine("❌ Feature generation failed:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   • {error}");
                }
                return;
            }
            
            // Step 2: Display results
            Console.WriteLine("✅ Feature generation completed successfully!");
            Console.WriteLine();
            Console.WriteLine("📊 Generation Summary:");
            Console.WriteLine($"   • Duration: {result.Metadata.Duration.TotalSeconds:F2} seconds");
            Console.WriteLine($"   • Agents Used: {result.Metadata.AgentsUsed}");
            Console.WriteLine($"   • Execution Strategy: {result.Metadata.ExecutionStrategy}");
            Console.WriteLine($"   • Artifacts Generated: {result.CodeArtifacts.Count}");
            Console.WriteLine();
            
            // Step 3: Display specification details
            if (verbose && result.Specification != null)
            {
                Console.WriteLine("📋 Feature Specification:");
                Console.WriteLine($"   • Entities: {result.Specification.Entities.Count}");
                Console.WriteLine($"   • Value Objects: {result.Specification.ValueObjects.Count}");
                Console.WriteLine($"   • Business Rules: {result.Specification.BusinessRules.Count}");
                Console.WriteLine($"   • Validation Rules: {result.Specification.ValidationRules.Count}");
                Console.WriteLine();
                
                foreach (var entity in result.Specification.Entities)
                {
                    Console.WriteLine($"   📦 Entity: {entity.Name}");
                    Console.WriteLine($"      Description: {entity.Description}");
                    Console.WriteLine($"      Properties: {entity.Properties.Count}");
                    Console.WriteLine($"      CRUD Operations: {entity.IncludeCrudOperations}");
                    Console.WriteLine();
                }
            }
            
            // Step 4: Display generated artifacts
            Console.WriteLine("📁 Generated Artifacts:");
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
                Console.WriteLine("💡 Use --output <directory> to save generated code to files");
            }
            
            Console.WriteLine();
            Console.WriteLine("🎉 Feature generation completed successfully!");
        }
        
        private static async Task HandleAnalyzeCommand(
            IServiceProvider serviceProvider, 
            ILogger logger, 
            string description, 
            TargetPlatform platform, 
            string output)
        {
            logger.LogInformation("Starting feature analysis: {Description}", description);
            
            Console.WriteLine("🔍 Feature Analysis");
            Console.WriteLine("===================");
            Console.WriteLine($"Description: {description}");
            Console.WriteLine($"Platform: {platform}");
            Console.WriteLine();
            
            var orchestrator = serviceProvider.GetRequiredService<IFeatureOrchestrator>();
            
            // Analyze the feature
            Console.WriteLine("📋 Analyzing feature requirements...");
            var specification = await orchestrator.AnalyzeFeatureAsync(description, platform, CancellationToken.None);
            
            // Display results
            Console.WriteLine("✅ Feature analysis completed!");
            Console.WriteLine();
            Console.WriteLine("📊 Analysis Summary:");
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
                Console.WriteLine("📦 Entities:");
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
                Console.WriteLine("🔧 Value Objects:");
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
                Console.WriteLine("📋 Business Rules:");
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
                Console.WriteLine("✅ Validation Rules:");
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
                Console.WriteLine("💡 Use --output <file> to save the specification to a JSON file");
            }
            
            Console.WriteLine();
            Console.WriteLine("🎉 Feature analysis completed successfully!");
        }
        
        private static async Task SaveArtifactsToDirectory(
            IReadOnlyList<CodeArtifact> artifacts, 
            string outputDirectory, 
            ILogger logger)
        {
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            
            foreach (var artifact in artifacts)
            {
                var fullPath = Path.Combine(outputDirectory, artifact.FilePath);
                var directory = Path.GetDirectoryName(fullPath);
                
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                await File.WriteAllTextAsync(fullPath, artifact.Content);
                logger.LogInformation("Saved artifact: {FilePath}", fullPath);
            }
        }
        
        private static async Task SaveSpecificationToFile(
            Nexo.Feature.Factory.Domain.Entities.FeatureSpecification specification, 
            string outputFile, 
            ILogger logger)
        {
            // Create a simplified JSON representation of the specification
            var specificationJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                Id = specification.Id.Value,
                Description = specification.Description,
                TargetPlatform = specification.TargetPlatform.ToString(),
                Status = specification.Status.ToString(),
                ExecutionStrategy = specification.ExecutionStrategy.ToString(),
                CreatedAt = specification.CreatedAt,
                Entities = specification.Entities.Select(e => new
                {
                    e.Name,
                    e.Description,
                    e.Namespace,
                    e.IncludeCrudOperations,
                    e.IncludeValidation,
                    Properties = e.Properties.Select(p => new
                    {
                        p.Name,
                        p.Type,
                        p.Description,
                        p.IsRequired,
                        p.IsUnique
                    }).ToArray()
                }).ToArray(),
                ValueObjects = specification.ValueObjects.Select(vo => new
                {
                    vo.Name,
                    vo.Description,
                    vo.Namespace,
                    vo.IncludeValidation,
                    Properties = vo.Properties.Select(p => new
                    {
                        p.Name,
                        p.Type,
                        p.Description,
                        p.IsRequired
                    }).ToArray()
                }).ToArray(),
                BusinessRules = specification.BusinessRules.Select(br => new
                {
                    br.Name,
                    br.Description,
                    br.Condition,
                    br.Action,
                    Priority = br.Priority.ToString(),
                    br.AppliesTo
                }).ToArray(),
                ValidationRules = specification.ValidationRules.Select(vr => new
                {
                    vr.Name,
                    vr.Description,
                    Type = vr.Type.ToString(),
                    vr.Expression,
                    vr.ErrorMessage,
                    Severity = vr.Severity.ToString(),
                    vr.AppliesTo
                }).ToArray()
            }, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(outputFile, specificationJson);
            logger.LogInformation("Saved specification to: {FilePath}", outputFile);
        }
    }
}
