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
    /// Swift service generation functionality
    /// </summary>
    public partial class iOSCodeGenerator : IIOSCodeGenerator
    {
        /// <summary>
        /// Generates services from application logic.
        /// </summary>
        public async Task<IEnumerable<SwiftService>> GenerateServicesAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var services = new List<SwiftService>();

            try
            {
                foreach (var service in applicationLogic.Services)
                {
                    var swiftService = await GenerateServiceAsync(service, options, cancellationToken);
                    services.Add(swiftService);
                }

                return services;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating services");
                return services;
            }
        }

        private async Task<SwiftService> GenerateServiceAsync(
            Service service,
            iOSGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var swiftService = new SwiftService
            {
                Name = service.Name,
                ServiceName = service.Name,
                Description = service.Description
            };

            try
            {
                // Generate service using AI
                var prompt = $@"
Generate a Swift service for the following:
- Name: {service.Name}
- Description: {service.Description}
- Methods: {string.Join(", ", service.Methods.Select(m => $"{m.Name}()"))}

Requirements:
- Use protocol-oriented programming
- Include proper error handling
- Add dependency injection support
- Use async/await for network calls
- Include proper logging
- Follow SOLID principles
- Use modern Swift patterns

Generate complete, production-ready service code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                swiftService.Code = response.Response;
                swiftService.GeneratedAt = DateTimeOffset.UtcNow;
                swiftService.Success = true;

                return swiftService;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating service: {ServiceName}", service.Name);
                swiftService.Success = false;
                swiftService.ErrorMessage = ex.Message;
                return swiftService;
            }
        }
    }
}
