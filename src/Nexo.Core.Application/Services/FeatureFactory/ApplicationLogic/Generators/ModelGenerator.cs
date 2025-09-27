using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Application.Services.FeatureFactory.ApplicationLogic.Generators
{
    public partial class ModelGenerator
    {
        private readonly ILogger<ModelGenerator> _logger;

        public ModelGenerator(ILogger<ModelGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Model>> GenerateModelsAsync(List<string> entities, CancellationToken cancellationToken)
        {
            var models = new List<Model>();

            try
            {
                _logger.LogInformation("Generating models for {EntityCount} entities", entities.Count);

                foreach (var entity in entities)
                {
                    var model = new Model
                    {
                        Name = $"{entity}Model",
                        Entity = entity,
                        GeneratedAt = DateTime.UtcNow,
                        Code = GenerateModelCode(entity)
                    };

                    models.Add(model);
                }

                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating models");
                return models;
            }
        }

        private string GenerateModelCode(string entityName)
        {
            return $@"
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{{
    public partial class {entityName}Model
    {{
        public int Id {{ get; set; }}
        
        [Required]
        public string Name {{ get; set; }} = string.Empty;
        
        public string Description {{ get; set; }} = string.Empty;
        
        public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
        
        public DateTime? UpdatedAt {{ get; set; }}
    }}
}}";
        }
    }

    public partial class Model
    {
        public string Name { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }
}
