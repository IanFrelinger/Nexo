using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Core.Application.Services.FeatureFactory.ApplicationLogic.Generators
{
    public partial class ControllerGenerator
    {
        private readonly ILogger<ControllerGenerator> _logger;

        public ControllerGenerator(ILogger<ControllerGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Controller>> GenerateControllersAsync(List<string> entities, CancellationToken cancellationToken)
        {
            var controllers = new List<Controller>();

            try
            {
                _logger.LogInformation("Generating controllers for {EntityCount} entities", entities.Count);

                foreach (var entity in entities)
                {
                    var controller = new Controller
                    {
                        Name = $"{entity}Controller",
                        Entity = entity,
                        GeneratedAt = DateTime.UtcNow,
                        Code = GenerateControllerCode(entity)
                    };

                    controllers.Add(controller);
                }

                return controllers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating controllers");
                return controllers;
            }
        }

        private string GenerateControllerCode(string entityName)
        {
            return $@"
using Microsoft.AspNetCore.Mvc;

namespace Controllers
{{
    [ApiController]
    [Route(""api/[controller]"")]
    public partial class {entityName}Controller : ControllerBase
    {{
        [HttpGet]
        public IActionResult Get()
        {{
            return Ok();
        }}

        [HttpPost]
        public IActionResult Create()
        {{
            return Ok();
        }}

        [HttpPut(""{{id}}"")]
        public IActionResult Update(int id)
        {{
            return Ok();
        }}

        [HttpDelete(""{{id}}"")]
        public IActionResult Delete(int id)
        {{
            return Ok();
        }}
    }}
}}";
        }
    }

    public partial class Controller
    {
        public string Name { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }
}
