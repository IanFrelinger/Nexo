using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Application.Services.FeatureFactory.ApplicationLogic.Generators
{
    public class ViewGenerator
    {
        private readonly ILogger<ViewGenerator> _logger;

        public ViewGenerator(ILogger<ViewGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<View>> GenerateViewsAsync(List<string> entities, CancellationToken cancellationToken)
        {
            var views = new List<View>();

            try
            {
                _logger.LogInformation("Generating views for {EntityCount} entities", entities.Count);

                foreach (var entity in entities)
                {
                    var view = new View
                    {
                        Name = $"{entity}View",
                        Entity = entity,
                        GeneratedAt = DateTime.UtcNow,
                        Code = GenerateViewCode(entity)
                    };

                    views.Add(view);
                }

                return views;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating views");
                return views;
            }
        }

        private string GenerateViewCode(string entityName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>{entityName} Management</title>
</head>
<body>
    <h1>{entityName} Management</h1>
    
    <div class=""{entityName.ToLower()}-list"">
        <!-- {entityName} list will be displayed here -->
    </div>
    
    <div class=""{entityName.ToLower()}-form"">
        <!-- {entityName} form will be displayed here -->
    </div>
    
    <script>
        // JavaScript for {entityName} management
        console.log('{entityName} view loaded');
    </script>
</body>
</html>";
        }
    }

    public class View
    {
        public string Name { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }
}
