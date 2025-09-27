using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Application.Interfaces.Platform;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Services.Platform
{
    /// <summary>
    /// Swift ViewModel generation functionality
    /// </summary>
    public partial class iOSCodeGenerator : IIOSCodeGenerator
    {
        /// <summary>
        /// Generates ViewModels from application logic.
        /// </summary>
        public async Task<IEnumerable<SwiftViewModel>> GenerateViewModelsAsync(
            ApplicationLogic applicationLogic,
            iOSGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var viewModels = new List<SwiftViewModel>();

            try
            {
                foreach (var feature in applicationLogic.Features)
                {
                    var viewModel = await GenerateViewModelForFeatureAsync(feature, options, cancellationToken);
                    viewModels.Add(viewModel);
                }

                return viewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ViewModels");
                return viewModels;
            }
        }

        private async Task<SwiftViewModel> GenerateViewModelForFeatureAsync(
            Nexo.Core.Application.Interfaces.Platform.Feature feature,
            iOSGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var viewModel = new SwiftViewModel
            {
                Name = $"{feature.Name}ViewModel",
                FeatureName = feature.Name,
                Description = feature.Description
            };

            try
            {
                // Generate ViewModel using AI
                var prompt = $@"
Generate a Swift ViewModel for the following feature:
- Name: {feature.Name}
- Description: {feature.Description}
- Requirements: {string.Join(", ", feature.Requirements)}

Requirements:
- Use ObservableObject for state management
- Include proper error handling
- Add loading states
- Use async/await for network calls
- Follow MVVM pattern
- Include unit testable code
- Use modern Swift concurrency

Generate complete, production-ready ViewModel code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                viewModel.Code = response.Response;
                viewModel.GeneratedAt = DateTimeOffset.UtcNow;
                viewModel.Success = true;

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ViewModel for feature: {FeatureName}", feature.Name);
                viewModel.Success = false;
                viewModel.ErrorMessage = ex.Message;
                return viewModel;
            }
        }
    }
}
