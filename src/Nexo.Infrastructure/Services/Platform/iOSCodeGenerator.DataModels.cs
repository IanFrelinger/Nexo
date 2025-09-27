using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Interfaces.Platform;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.Platform
{
    /// <summary>
    /// Core Data model generation functionality
    /// </summary>
    public partial class iOSCodeGenerator : IIOSCodeGenerator
    {
        /// <summary>
        /// Generates Core Data models from application logic.
        /// </summary>
        public async Task<IEnumerable<CoreDataModel>> GenerateCoreDataModelsAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var models = new List<CoreDataModel>();

            try
            {
                foreach (var entity in applicationLogic.Entities)
                {
                    var model = await GenerateCoreDataModelForEntityAsync(entity, options, cancellationToken);
                    models.Add(model);
                }

                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Core Data models");
                return models;
            }
        }

        private async Task<CoreDataModel> GenerateCoreDataModelForEntityAsync(
            Entity entity,
            iOSGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var model = new CoreDataModel
            {
                Name = entity.Name,
                EntityName = entity.Name,
                Description = entity.Description
            };

            try
            {
                // Generate Core Data model using AI
                var prompt = $@"
Generate a Core Data model for the following entity:
- Name: {entity.Name}
- Description: {entity.Description}
- Properties: {string.Join(", ", entity.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Requirements:
- Use Core Data with iOS 15+ features
- Include proper relationships
- Add validation rules
- Include migration support
- Use modern Core Data patterns
- Follow Apple's Core Data guidelines

Generate complete, production-ready Core Data model code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                model.Code = response.Response;
                model.GeneratedAt = DateTimeOffset.UtcNow;
                model.Success = true;

                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Core Data model for entity: {EntityName}", entity.Name);
                model.Success = false;
                model.ErrorMessage = ex.Message;
                return model;
            }
        }
    }
}
