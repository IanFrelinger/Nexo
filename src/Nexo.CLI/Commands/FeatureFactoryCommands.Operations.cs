using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Factory.Domain.Models;
using Nexo.Feature.Factory.Domain.Entities;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// File operations functionality for FeatureFactoryCommands.
    /// </summary>
    public static partial class FeatureFactoryCommands
    {
        /// <summary>
        /// Saves artifacts to directory
        /// </summary>
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

        /// <summary>
        /// Saves specification to file
        /// </summary>
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
