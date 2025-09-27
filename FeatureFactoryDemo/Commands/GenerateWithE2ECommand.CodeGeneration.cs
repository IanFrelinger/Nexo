using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Analysis.Models;

namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Code generation functionality for E2E generation command.
    /// </summary>
    public partial class GenerateWithE2ECommand
    {
        private string GenerateInitialCode(string description, string platform)
        {
            return $@"
// Generated feature for {platform}
// Description: {description}
// Generated at: {DateTime.UtcNow}

using System;
using System.ComponentModel.DataAnnotations;

namespace Nexo.FeatureFactory.Generated
{{
    public class GeneratedEntity
    {{
        [Key]
        public int Id {{ get; set; }}
        
        [Required]
        [StringLength(100)]
        public string Name {{ get; set; }} = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string Description {{ get; set; }} = string.Empty;
        
        public bool IsActive {{ get; set; }} = true;
        
        public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
        public DateTime? UpdatedAt {{ get; set; }}
    }}

    public interface IGeneratedEntityRepository
    {{
        Task<GeneratedEntity?> GetByIdAsync(int id);
        Task<IEnumerable<GeneratedEntity>> GetAllAsync();
        Task<GeneratedEntity> CreateAsync(GeneratedEntity entity);
        Task<GeneratedEntity> UpdateAsync(GeneratedEntity entity);
        Task DeleteAsync(int id);
    }}

    public class GeneratedEntityService
    {{
        private readonly IGeneratedEntityRepository _repository;
        private readonly ILogger<GeneratedEntityService> _logger;

        public GeneratedEntityService(IGeneratedEntityRepository repository, ILogger<GeneratedEntityService> logger)
        {{
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }}

        public async Task<GeneratedEntity> CreateEntityAsync(string name, string description)
        {{
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(""Name cannot be null or empty"", nameof(name));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException(""Description cannot be null or empty"", nameof(description));

            var newEntity = new GeneratedEntity
            {{
                Name = name,
                Description = description,
                IsActive = true
            }};

            return await _repository.CreateAsync(newEntity);
        }}

        public async Task<GeneratedEntity?> GetEntityAsync(int id)
        {{
            return await _repository.GetByIdAsync(id);
        }}

        public async Task<IEnumerable<GeneratedEntity>> GetAllEntitiesAsync()
        {{
            return await _repository.GetAllAsync();
        }}

        public async Task<GeneratedEntity> UpdateEntityAsync(int id, string name, string description, bool isActive)
        {{
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                throw new ArgumentException(""Entity not found"");

            entity.Name = name;
            entity.Description = description;
            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;

            return await _repository.UpdateAsync(entity);
        }}

        public async Task DeleteEntityAsync(int id)
        {{
            await _repository.DeleteAsync(id);
        }}
    }}
}}";
        }

        private string GetImprovementDescription(int iteration, int score)
        {
            var improvements = new[]
            {
                "Initial code with basic structure",
                "Fixed variable naming conventions",
                "Added input validation and error handling",
                "Implemented comprehensive logging",
                "Added XML documentation and improved error messages",
                "Enhanced with comprehensive validation and logging",
                "Perfect code quality achieved!"
            };

            return iteration <= improvements.Length ? improvements[iteration - 1] : "Final optimizations applied";
        }

        private string ImproveCodeBasedOnViolations(string code, List<CodingStandardViolation> violations)
        {
            // Simulate code improvement based on violations
            var improvedCode = code;
            
            foreach (var violation in violations.Take(3)) // Limit to 3 improvements per iteration
            {
                switch (violation.RuleName)
                {
                    case "NamingConvention":
                        improvedCode = improvedCode.Replace("var entity", "var generatedEntity");
                        break;
                    case "Documentation":
                        improvedCode = improvedCode.Replace("public class GeneratedEntity", "/// <summary>\n    /// Represents a generated entity\n    /// </summary>\n    public class GeneratedEntity");
                        break;
                    case "ErrorHandling":
                        improvedCode = improvedCode.Replace("return await _repository.CreateAsync(newEntity);", "try\n            {\n                return await _repository.CreateAsync(newEntity);\n            }\n            catch (Exception ex)\n            {\n                _logger.LogError(ex, \"Error creating entity\");\n                throw;\n            }");
                        break;
                }
            }

            return improvedCode;
        }
    }
}
