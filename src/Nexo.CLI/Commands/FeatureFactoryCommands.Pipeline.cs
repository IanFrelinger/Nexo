using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Domain.Enums;
using Nexo.Feature.Factory.Domain.Models;
using Nexo.Feature.Factory.Domain.Entities;
using Nexo.Core.Pipeline;
using Nexo.Core.Contracts;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Pipeline command functionality for FeatureFactoryCommands.
    /// </summary>
    public static partial class FeatureFactoryCommands
    {
        /// <summary>
        /// Creates the pipeline command
        /// </summary>
        private static Command CreatePipelineCommand(IServiceProvider serviceProvider, ILogger logger)
        {
            var pipelineCommand = new Command("pipeline", "Run extension generation through the typed pipeline");
            var pipelineDescriptionOption = new Option<string>("--description", "Natural language description of the extension") { IsRequired = true };
            var pipelineNameOption = new Option<string>("--name", "Name of the extension");
            var pipelineTypeOption = new Option<string>("--type", () => "Custom", "Type of extension (Analyzer, Generator, Processor, etc.)");
            var pipelineNamespaceOption = new Option<string>("--namespace", "Target namespace for the extension");
            var pipelineVerboseOption = new Option<bool>("--verbose", "Enable verbose output");
            
            pipelineCommand.AddOption(pipelineDescriptionOption);
            pipelineCommand.AddOption(pipelineNameOption);
            pipelineCommand.AddOption(pipelineTypeOption);
            pipelineCommand.AddOption(pipelineNamespaceOption);
            pipelineCommand.AddOption(pipelineVerboseOption);
            
            pipelineCommand.SetHandler(async (description, name, type, namespace, verbose) =>
            {
                try
                {
                    await HandlePipelineCommand(serviceProvider, logger, description, name, type, namespace, verbose);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error running pipeline");
                    Console.WriteLine($"ERROR: {ex.Message}");
                    Environment.Exit(1);
                }
            }, pipelineDescriptionOption, pipelineNameOption, pipelineTypeOption, pipelineNamespaceOption, pipelineVerboseOption);
            
            return pipelineCommand;
        }

        /// <summary>
        /// Handles the pipeline command
        /// </summary>
        private static async Task HandlePipelineCommand(
            IServiceProvider serviceProvider, 
            ILogger logger, 
            string description, 
            string name, 
            string type, 
            string namespace, 
            bool verbose)
        {
            logger.LogInformation("Starting pipeline execution: {Description}", description);
            
            Console.WriteLine("Running Typed Extension Pipeline");
            Console.WriteLine("==============================");
            Console.WriteLine($"Description: {description}");
            Console.WriteLine($"Name: {name ?? "Auto-generated"}");
            Console.WriteLine($"Type: {type}");
            Console.WriteLine($"Namespace: {namespace ?? "Auto-generated"}");
            Console.WriteLine();
            
            // Create extension request
            var request = new ExtensionRequest
            {
                Name = name ?? $"Extension_{Guid.NewGuid():N}",
                Description = description,
                Type = Enum.TryParse<ExtensionType>(type, true, out var extensionType) ? extensionType : ExtensionType.Custom,
                TargetNamespace = namespace ?? "Nexo.Generated.Extensions",
                Author = "AI Generated",
                Version = "1.0.0",
                CreatedAt = DateTime.UtcNow
            };
            
            // Get the pipeline from DI
            var pipeline = serviceProvider.GetRequiredService<ExtensionPipeline<ExtensionRequest, GeneratedExtension>>();
            
            // Run the pipeline
            Console.WriteLine("List Starting pipeline execution...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var report = await pipeline.RunAsync(request, CancellationToken.None);
            
            stopwatch.Stop();
            
            // Display results
            Console.WriteLine("SUCCESS: Pipeline execution completed!");
            Console.WriteLine();
            Console.WriteLine("Stats Pipeline Summary:");
            Console.WriteLine($"   • Duration: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"   • Artifact ID: {report.ArtifactId}");
            Console.WriteLine($"   • Succeeded: {report.Succeeded}");
            Console.WriteLine($"   • Quality Score: {report.QualityScore:F2}");
            Console.WriteLine($"   • Notes: {report.Notes.Count}");
            Console.WriteLine();
            
            if (verbose && report.Notes.Any())
            {
                Console.WriteLine("List Pipeline Notes:");
                foreach (var note in report.Notes)
                {
                    Console.WriteLine($"   • {note}");
                }
                Console.WriteLine();
            }
            
            if (report.Succeeded)
            {
                Console.WriteLine("SUCCESS: Extension generated and validated successfully!");
            }
            else
            {
                Console.WriteLine("WARNING: Pipeline completed with issues. Check the notes above for details.");
            }
        }
    }
}
