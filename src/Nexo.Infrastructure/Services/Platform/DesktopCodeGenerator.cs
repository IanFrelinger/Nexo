using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Platform;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Services.Platform.Generators;

namespace Nexo.Infrastructure.Services.Platform
{
    /// <summary>
    /// Desktop code generator for Phase 6 - orchestrates specialized generators
    /// Generates native desktop applications for Windows, Mac, and Linux.
    /// </summary>
    public class DesktopCodeGenerator : IDesktopCodeGenerator
    {
        private readonly ILogger<DesktopCodeGenerator> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly DesktopUIGenerator _uiGenerator;
        private readonly DesktopServiceGenerator _serviceGenerator;
        private readonly DesktopPlatformGenerator _platformGenerator;
        private readonly DesktopTestGenerator _testGenerator;

        public DesktopCodeGenerator(
            ILogger<DesktopCodeGenerator> logger,
            IModelOrchestrator modelOrchestrator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            
            // Initialize specialized generators
            _uiGenerator = new DesktopUIGenerator(logger, modelOrchestrator);
            _serviceGenerator = new DesktopServiceGenerator(logger, modelOrchestrator);
            _platformGenerator = new DesktopPlatformGenerator(logger, modelOrchestrator);
            _testGenerator = new DesktopTestGenerator(logger, modelOrchestrator);
        }

        /// <summary>
        /// Generates desktop application code from application logic by delegating to specialized generators.
        /// </summary>
        public async Task<DesktopGenerationResult> GenerateCodeAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting desktop code generation for application: {ApplicationName}", 
                applicationLogic.ApplicationName);

            var result = new DesktopGenerationResult
            {
                Success = false,
                Message = "Generation started",
                GeneratedFiles = new List<string>(),
                Errors = new List<string>()
            };

            try
            {
                // 1. Generate UI Components
                if (options.UseWPF || options.UseWinUI || options.UseAvalonia)
                {
                    var uiComponents = await _uiGenerator.GenerateUIComponentsAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(uiComponents.Select(c => $"{c.Name}.xaml"));
                }

                // 2. Generate ViewModels
                if (options.UseWPF || options.UseWinUI || options.UseAvalonia)
                {
                    var viewModels = await _uiGenerator.GenerateViewModelsAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(viewModels.Select(vm => $"{vm.Name}.cs"));
                }

                // 3. Generate Services
                if (options.UseDataAccess)
                {
                    var services = await _serviceGenerator.GenerateServicesAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(services.Select(s => $"{s.Name}.cs"));
                }

                // 4. Generate Data Access Layer
                if (options.UseDataAccess)
                {
                    var dataAccess = await _serviceGenerator.GenerateDataAccessAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.Add($"{dataAccess.Name}DataAccess.cs");
                }

                // 5. Generate Configuration
                if (options.UseConfiguration)
                {
                    var configuration = await _serviceGenerator.GenerateConfigurationAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.Add($"{configuration.Name}Configuration.cs");
                }

                // 6. Generate Platform-Specific Code
                if (options.UsePlatformSpecificCode)
                {
                    var platformCode = await _platformGenerator.GeneratePlatformSpecificAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(platformCode.Select(pc => $"{pc.Name}Code.cs"));
                }

                // 7. Generate Build Configuration
                if (options.UseBuildConfiguration)
                {
                    var buildConfig = await _platformGenerator.GenerateBuildConfigurationAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.Add($"{buildConfig.Name}.csproj");
                }

                // 8. Generate Tests
                if (options.UseTest)
                {
                    var tests = await _testGenerator.GenerateTestsAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(tests.Select(t => $"{t.Name}.cs"));
                }

                result.Success = true;
                result.Message = $"Successfully generated {result.GeneratedFiles.Count} files";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during desktop code generation");
                result.Success = false;
                result.Message = ex.Message;
                result.Errors.Add(ex.Message);
                return result;
            }

            return result;
        }

        /// <summary>
        /// Generates UI components from application logic by delegating to UI generator.
        /// </summary>
        public async Task<IEnumerable<DesktopUIComponent>> GenerateUIComponentsAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _uiGenerator.GenerateUIComponentsAsync(applicationLogic, options, cancellationToken);
        }

        /// <summary>
        /// Generates view models from application logic by delegating to UI generator.
        /// </summary>
        public async Task<IEnumerable<DesktopViewModel>> GenerateViewModelsAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _uiGenerator.GenerateViewModelsAsync(applicationLogic, options, cancellationToken);
        }

        /// <summary>
        /// Generates services from application logic by delegating to service generator.
        /// </summary>
        public async Task<IEnumerable<DesktopService>> GenerateServicesAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _serviceGenerator.GenerateServicesAsync(applicationLogic, options, cancellationToken);
        }

        /// <summary>
        /// Generates data access layer from application logic by delegating to service generator.
        /// </summary>
        public async Task<DesktopDataAccess> GenerateDataAccessAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _serviceGenerator.GenerateDataAccessAsync(applicationLogic, options, cancellationToken);
        }

        /// <summary>
        /// Generates configuration from application logic by delegating to service generator.
        /// </summary>
        public async Task<DesktopConfiguration> GenerateConfigurationAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _serviceGenerator.GenerateConfigurationAsync(applicationLogic, options, cancellationToken);
        }

        /// <summary>
        /// Generates platform-specific code by delegating to platform generator.
        /// </summary>
        public async Task<IEnumerable<PlatformSpecificCode>> GeneratePlatformSpecificAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _platformGenerator.GeneratePlatformSpecificAsync(applicationLogic, options, cancellationToken);
        }

        /// <summary>
        /// Generates build configuration by delegating to platform generator.
        /// </summary>
        public async Task<DesktopBuildConfiguration> GenerateBuildConfigurationAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _platformGenerator.GenerateBuildConfigurationAsync(applicationLogic, options, cancellationToken);
        }

        /// <summary>
        /// Generates tests by delegating to test generator.
        /// </summary>
        public async Task<IEnumerable<DesktopTest>> GenerateTestsAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _testGenerator.GenerateTestsAsync(applicationLogic, options, cancellationToken);
        }
    }
}
