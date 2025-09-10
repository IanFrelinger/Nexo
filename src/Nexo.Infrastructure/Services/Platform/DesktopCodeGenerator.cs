using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.Platform;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Infrastructure.Services.Platform
{
    /// <summary>
    /// Desktop code generator for Phase 6.
    /// Generates native desktop applications for Windows, Mac, and Linux.
    /// </summary>
    public class DesktopCodeGenerator : IDesktopCodeGenerator
    {
        private readonly ILogger<DesktopCodeGenerator> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public DesktopCodeGenerator(
            ILogger<DesktopCodeGenerator> logger,
            IModelOrchestrator modelOrchestrator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        /// <summary>
        /// Generates desktop application code from application logic.
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
                ApplicationName = applicationLogic.ApplicationName,
                StartTime = DateTimeOffset.UtcNow,
                Options = options
            };

            try
            {
                // 1. Generate UI Components
                if (options.GenerateUIComponents)
                {
                    result.UIComponents = await GenerateUIComponentsAsync(applicationLogic, options, cancellationToken);
                }

                // 2. Generate ViewModels
                if (options.GenerateViewModels)
                {
                    result.ViewModels = await GenerateViewModelsAsync(applicationLogic, options, cancellationToken);
                }

                // 3. Generate Services
                if (options.GenerateServices)
                {
                    result.Services = await GenerateServicesAsync(applicationLogic, options, cancellationToken);
                }

                // 4. Generate Data Access Layer
                if (options.GenerateDataAccess)
                {
                    result.DataAccess = await GenerateDataAccessAsync(applicationLogic, options, cancellationToken);
                }

                // 5. Generate Configuration
                if (options.GenerateConfiguration)
                {
                    result.Configuration = await GenerateConfigurationAsync(applicationLogic, options, cancellationToken);
                }

                // 6. Generate Platform-Specific Code
                if (options.GeneratePlatformSpecific)
                {
                    result.PlatformSpecific = await GeneratePlatformSpecificAsync(applicationLogic, options, cancellationToken);
                }

                // 7. Generate Build Configuration
                if (options.GenerateBuildConfiguration)
                {
                    result.BuildConfiguration = await GenerateBuildConfigurationAsync(applicationLogic, options, cancellationToken);
                }

                // 8. Generate Tests
                if (options.GenerateTests)
                {
                    result.Tests = await GenerateTestsAsync(applicationLogic, options, cancellationToken);
                }

                result.EndTime = DateTimeOffset.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                result.Success = true;

                _logger.LogInformation("Desktop code generation completed successfully in {Duration}ms", 
                    result.Duration.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during desktop code generation");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTimeOffset.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                return result;
            }
        }

        /// <summary>
        /// Generates UI components from application logic.
        /// </summary>
        public async Task<IEnumerable<DesktopUIComponent>> GenerateUIComponentsAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var components = new List<DesktopUIComponent>();

            try
            {
                foreach (var feature in applicationLogic.Features)
                {
                    var component = await GenerateUIComponentForFeatureAsync(feature, options, cancellationToken);
                    components.Add(component);
                }

                return components;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating UI components");
                return components;
            }
        }

        /// <summary>
        /// Generates ViewModels from application logic.
        /// </summary>
        public async Task<IEnumerable<DesktopViewModel>> GenerateViewModelsAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var viewModels = new List<DesktopViewModel>();

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

        /// <summary>
        /// Generates services from application logic.
        /// </summary>
        public async Task<IEnumerable<DesktopService>> GenerateServicesAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var services = new List<DesktopService>();

            try
            {
                foreach (var service in applicationLogic.Services)
                {
                    var desktopService = await GenerateServiceAsync(service, options, cancellationToken);
                    services.Add(desktopService);
                }

                return services;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating services");
                return services;
            }
        }

        /// <summary>
        /// Generates data access layer from application logic.
        /// </summary>
        public async Task<DesktopDataAccess> GenerateDataAccessAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var dataAccess = new DesktopDataAccess
            {
                ApplicationName = applicationLogic.ApplicationName,
                DatabaseType = options.DatabaseType
            };

            try
            {
                // Generate entities
                var entities = new List<DesktopEntity>();
                foreach (var entity in applicationLogic.Entities)
                {
                    var desktopEntity = await GenerateEntityAsync(entity, options, cancellationToken);
                    entities.Add(desktopEntity);
                }

                // Generate repositories
                var repositories = new List<DesktopRepository>();
                foreach (var entity in applicationLogic.Entities)
                {
                    var repository = await GenerateRepositoryAsync(entity, options, cancellationToken);
                    repositories.Add(repository);
                }

                // Generate database context
                var context = await GenerateDatabaseContextAsync(entities, options, cancellationToken);

                dataAccess.Entities = entities;
                dataAccess.Repositories = repositories;
                dataAccess.DatabaseContext = context;
                dataAccess.GeneratedAt = DateTimeOffset.UtcNow;
                dataAccess.Success = true;

                return dataAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating data access layer");
                dataAccess.Success = false;
                dataAccess.ErrorMessage = ex.Message;
                return dataAccess;
            }
        }

        /// <summary>
        /// Generates configuration from application logic.
        /// </summary>
        public async Task<DesktopConfiguration> GenerateConfigurationAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var configuration = new DesktopConfiguration
            {
                ApplicationName = applicationLogic.ApplicationName,
                Platform = options.Platform
            };

            try
            {
                // Generate app settings
                var appSettings = await GenerateAppSettingsAsync(applicationLogic, options, cancellationToken);
                
                // Generate dependency injection
                var dependencyInjection = await GenerateDependencyInjectionAsync(applicationLogic, options, cancellationToken);
                
                // Generate logging configuration
                var loggingConfig = await GenerateLoggingConfigurationAsync(applicationLogic, options, cancellationToken);

                configuration.AppSettings = appSettings;
                configuration.DependencyInjection = dependencyInjection;
                configuration.LoggingConfiguration = loggingConfig;
                configuration.GeneratedAt = DateTimeOffset.UtcNow;
                configuration.Success = true;

                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating configuration");
                configuration.Success = false;
                configuration.ErrorMessage = ex.Message;
                return configuration;
            }
        }

        /// <summary>
        /// Generates platform-specific code from application logic.
        /// </summary>
        public async Task<IEnumerable<PlatformSpecificCode>> GeneratePlatformSpecificAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var platformCode = new List<PlatformSpecificCode>();

            try
            {
                // Generate code for each target platform
                foreach (var platform in options.TargetPlatforms)
                {
                    var code = await GeneratePlatformCodeAsync(platform, applicationLogic, options, cancellationToken);
                    platformCode.Add(code);
                }

                return platformCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating platform-specific code");
                return platformCode;
            }
        }

        /// <summary>
        /// Generates build configuration from application logic.
        /// </summary>
        public async Task<DesktopBuildConfiguration> GenerateBuildConfigurationAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var buildConfig = new DesktopBuildConfiguration
            {
                ApplicationName = applicationLogic.ApplicationName,
                Platform = options.Platform
            };

            try
            {
                // Generate project file
                var projectFile = await GenerateProjectFileAsync(applicationLogic, options, cancellationToken);
                
                // Generate solution file
                var solutionFile = await GenerateSolutionFileAsync(applicationLogic, options, cancellationToken);
                
                // Generate build scripts
                var buildScripts = await GenerateBuildScriptsAsync(applicationLogic, options, cancellationToken);

                buildConfig.ProjectFile = projectFile;
                buildConfig.SolutionFile = solutionFile;
                buildConfig.BuildScripts = buildScripts;
                buildConfig.GeneratedAt = DateTimeOffset.UtcNow;
                buildConfig.Success = true;

                return buildConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating build configuration");
                buildConfig.Success = false;
                buildConfig.ErrorMessage = ex.Message;
                return buildConfig;
            }
        }

        /// <summary>
        /// Generates unit tests for desktop code.
        /// </summary>
        public async Task<IEnumerable<DesktopTest>> GenerateTestsAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var tests = new List<DesktopTest>();

            try
            {
                // Generate tests for each component
                foreach (var feature in applicationLogic.Features)
                {
                    var featureTests = await GenerateTestsForFeatureAsync(feature, options, cancellationToken);
                    tests.AddRange(featureTests);
                }

                return tests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tests");
                return tests;
            }
        }

        #region Private Methods

        private async Task<DesktopUIComponent> GenerateUIComponentForFeatureAsync(
            Nexo.Core.Application.Interfaces.Platform.Feature feature,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var component = new DesktopUIComponent
            {
                Name = $"{feature.Name}View",
                FeatureName = feature.Name,
                Description = feature.Description,
                Platform = options.Platform
            };

            try
            {
                // Generate UI component using AI
                var prompt = $@"
Generate a {options.Platform} UI component for the following feature:
- Name: {feature.Name}
- Description: {feature.Description}
- Requirements: {string.Join(", ", feature.Requirements)}

Requirements:
- Use {options.Platform} UI framework best practices
- Include proper MVVM pattern
- Add data binding
- Include accessibility features
- Add error handling
- Use modern {options.Platform} patterns
- Include responsive design
- Add loading states

Generate complete, production-ready UI component code.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                component.Code = response.Content;
                component.GeneratedAt = DateTimeOffset.UtcNow;
                component.Success = true;

                return component;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating UI component for feature: {FeatureName}", feature.Name);
                component.Success = false;
                component.ErrorMessage = ex.Message;
                return component;
            }
        }

        private async Task<DesktopViewModel> GenerateViewModelForFeatureAsync(
            Nexo.Core.Application.Interfaces.Platform.Feature feature,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var viewModel = new DesktopViewModel
            {
                Name = $"{feature.Name}ViewModel",
                FeatureName = feature.Name,
                Description = feature.Description,
                Platform = options.Platform
            };

            try
            {
                // Generate ViewModel using AI
                var prompt = $@"
Generate a {options.Platform} ViewModel for the following feature:
- Name: {feature.Name}
- Description: {feature.Description}
- Requirements: {string.Join(", ", feature.Requirements)}

Requirements:
- Use {options.Platform} ViewModel patterns
- Include proper data binding
- Add command patterns
- Include proper error handling
- Use async/await patterns
- Follow MVVM best practices
- Include unit testable code
- Use dependency injection

Generate complete, production-ready ViewModel code.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                viewModel.Code = response.Content;
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

        private async Task<DesktopService> GenerateServiceAsync(
            Service service,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var desktopService = new DesktopService
            {
                Name = service.Name,
                ServiceName = service.Name,
                Description = service.Description,
                Platform = options.Platform
            };

            try
            {
                // Generate service using AI
                var prompt = $@"
Generate a {options.Platform} service for the following:
- Name: {service.Name}
- Description: {service.Description}
- Methods: {string.Join(", ", service.Methods.Select(m => $"{m.Name}()"))}

Requirements:
- Use {options.Platform} service patterns
- Include proper lifecycle management
- Use async/await patterns
- Include proper error handling
- Use dependency injection
- Follow {options.Platform} best practices

Generate complete, production-ready service code.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                desktopService.Code = response.Content;
                desktopService.GeneratedAt = DateTimeOffset.UtcNow;
                desktopService.Success = true;

                return desktopService;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating service: {ServiceName}", service.Name);
                desktopService.Success = false;
                desktopService.ErrorMessage = ex.Message;
                return desktopService;
            }
        }

        private async Task<DesktopEntity> GenerateEntityAsync(
            Entity entity,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var desktopEntity = new DesktopEntity
            {
                Name = entity.Name,
                EntityName = entity.Name,
                Description = entity.Description,
                Platform = options.Platform
            };

            try
            {
                // Generate entity using AI
                var prompt = $@"
Generate a {options.Platform} entity for the following:
- Name: {entity.Name}
- Description: {entity.Description}
- Properties: {string.Join(", ", entity.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Requirements:
- Use {options.Platform} entity patterns
- Include proper data annotations
- Add validation attributes
- Use modern {options.Platform} patterns
- Follow {options.Platform} best practices

Generate complete, production-ready entity code.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                desktopEntity.Code = response.Content;
                desktopEntity.GeneratedAt = DateTimeOffset.UtcNow;
                desktopEntity.Success = true;

                return desktopEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating entity for: {EntityName}", entity.Name);
                desktopEntity.Success = false;
                desktopEntity.ErrorMessage = ex.Message;
                return desktopEntity;
            }
        }

        private async Task<DesktopRepository> GenerateRepositoryAsync(
            Entity entity,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var repository = new DesktopRepository
            {
                Name = $"{entity.Name}Repository",
                EntityName = entity.Name,
                Description = $"Repository for {entity.Name}",
                Platform = options.Platform
            };

            try
            {
                // Generate repository using AI
                var prompt = $@"
Generate a {options.Platform} repository for the following entity:
- Entity Name: {entity.Name}
- Properties: {string.Join(", ", entity.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Requirements:
- Use {options.Platform} repository patterns
- Include CRUD operations
- Use async/await patterns
- Include proper error handling
- Use dependency injection
- Follow {options.Platform} best practices

Generate complete, production-ready repository code.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                repository.Code = response.Content;
                repository.GeneratedAt = DateTimeOffset.UtcNow;
                repository.Success = true;

                return repository;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating repository for: {EntityName}", entity.Name);
                repository.Success = false;
                repository.ErrorMessage = ex.Message;
                return repository;
            }
        }

        private async Task<DesktopDatabaseContext> GenerateDatabaseContextAsync(
            List<DesktopEntity> entities,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var context = new DesktopDatabaseContext
            {
                Name = $"{options.Platform}DbContext",
                Platform = options.Platform
            };

            try
            {
                // Generate database context using AI
                var prompt = $@"
Generate a {options.Platform} database context with the following entities:
- Entities: {string.Join(", ", entities.Select(e => e.Name))}

Requirements:
- Use {options.Platform} database context patterns
- Include all entities
- Add proper configuration
- Use modern {options.Platform} patterns
- Follow {options.Platform} best practices

Generate complete, production-ready database context code.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                context.Code = response.Content;
                context.GeneratedAt = DateTimeOffset.UtcNow;
                context.Success = true;

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating database context");
                context.Success = false;
                context.ErrorMessage = ex.Message;
                return context;
            }
        }

        private async Task<DesktopAppSettings> GenerateAppSettingsAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var appSettings = new DesktopAppSettings
            {
                ApplicationName = applicationLogic.ApplicationName,
                Platform = options.Platform
            };

            try
            {
                // Generate app settings using AI
                var prompt = $@"
Generate {options.Platform} app settings for the following application:
- App Name: {applicationLogic.ApplicationName}
- Platform: {options.Platform}

Requirements:
- Use {options.Platform} configuration patterns
- Include all necessary settings
- Use modern {options.Platform} patterns
- Follow {options.Platform} best practices

Generate complete, production-ready app settings code.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                appSettings.Code = response.Content;
                appSettings.GeneratedAt = DateTimeOffset.UtcNow;
                appSettings.Success = true;

                return appSettings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating app settings");
                appSettings.Success = false;
                appSettings.ErrorMessage = ex.Message;
                return appSettings;
            }
        }

        private async Task<DesktopDependencyInjection> GenerateDependencyInjectionAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var di = new DesktopDependencyInjection
            {
                ApplicationName = applicationLogic.ApplicationName,
                Platform = options.Platform
            };

            try
            {
                // Generate dependency injection using AI
                var prompt = $@"
Generate {options.Platform} dependency injection configuration for the following application:
- App Name: {applicationLogic.ApplicationName}
- Platform: {options.Platform}

Requirements:
- Use {options.Platform} DI patterns
- Include all services
- Use modern {options.Platform} patterns
- Follow {options.Platform} best practices

Generate complete, production-ready dependency injection code.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                di.Code = response.Content;
                di.GeneratedAt = DateTimeOffset.UtcNow;
                di.Success = true;

                return di;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dependency injection");
                di.Success = false;
                di.ErrorMessage = ex.Message;
                return di;
            }
        }

        private async Task<DesktopLoggingConfiguration> GenerateLoggingConfigurationAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var loggingConfig = new DesktopLoggingConfiguration
            {
                ApplicationName = applicationLogic.ApplicationName,
                Platform = options.Platform
            };

            try
            {
                // Generate logging configuration using AI
                var prompt = $@"
Generate {options.Platform} logging configuration for the following application:
- App Name: {applicationLogic.ApplicationName}
- Platform: {options.Platform}

Requirements:
- Use {options.Platform} logging patterns
- Include all necessary logging
- Use modern {options.Platform} patterns
- Follow {options.Platform} best practices

Generate complete, production-ready logging configuration code.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                loggingConfig.Code = response.Content;
                loggingConfig.GeneratedAt = DateTimeOffset.UtcNow;
                loggingConfig.Success = true;

                return loggingConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating logging configuration");
                loggingConfig.Success = false;
                loggingConfig.ErrorMessage = ex.Message;
                return loggingConfig;
            }
        }

        private async Task<PlatformSpecificCode> GeneratePlatformCodeAsync(
            string platform,
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var platformCode = new PlatformSpecificCode
            {
                Platform = platform,
                ApplicationName = applicationLogic.ApplicationName
            };

            try
            {
                // Generate platform-specific code using AI
                var prompt = $@"
Generate {platform} specific code for the following application:
- App Name: {applicationLogic.ApplicationName}
- Platform: {platform}

Requirements:
- Use {platform} specific patterns
- Include platform-specific features
- Use modern {platform} patterns
- Follow {platform} best practices

Generate complete, production-ready platform-specific code.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                platformCode.Code = response.Content;
                platformCode.GeneratedAt = DateTimeOffset.UtcNow;
                platformCode.Success = true;

                return platformCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating platform-specific code for: {Platform}", platform);
                platformCode.Success = false;
                platformCode.ErrorMessage = ex.Message;
                return platformCode;
            }
        }

        private async Task<DesktopProjectFile> GenerateProjectFileAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var projectFile = new DesktopProjectFile
            {
                ApplicationName = applicationLogic.ApplicationName,
                Platform = options.Platform
            };

            try
            {
                // Generate project file using AI
                var prompt = $@"
Generate a {options.Platform} project file for the following application:
- App Name: {applicationLogic.ApplicationName}
- Platform: {options.Platform}

Requirements:
- Use {options.Platform} project patterns
- Include all necessary references
- Use modern {options.Platform} patterns
- Follow {options.Platform} best practices

Generate complete, production-ready project file.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                projectFile.Code = response.Content;
                projectFile.GeneratedAt = DateTimeOffset.UtcNow;
                projectFile.Success = true;

                return projectFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating project file");
                projectFile.Success = false;
                projectFile.ErrorMessage = ex.Message;
                return projectFile;
            }
        }

        private async Task<DesktopSolutionFile> GenerateSolutionFileAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var solutionFile = new DesktopSolutionFile
            {
                ApplicationName = applicationLogic.ApplicationName,
                Platform = options.Platform
            };

            try
            {
                // Generate solution file using AI
                var prompt = $@"
Generate a {options.Platform} solution file for the following application:
- App Name: {applicationLogic.ApplicationName}
- Platform: {options.Platform}

Requirements:
- Use {options.Platform} solution patterns
- Include all necessary projects
- Use modern {options.Platform} patterns
- Follow {options.Platform} best practices

Generate complete, production-ready solution file.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                solutionFile.Code = response.Content;
                solutionFile.GeneratedAt = DateTimeOffset.UtcNow;
                solutionFile.Success = true;

                return solutionFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating solution file");
                solutionFile.Success = false;
                solutionFile.ErrorMessage = ex.Message;
                return solutionFile;
            }
        }

        private async Task<IEnumerable<DesktopBuildScript>> GenerateBuildScriptsAsync(
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var buildScripts = new List<DesktopBuildScript>();

            try
            {
                // Generate build scripts for each platform
                foreach (var platform in options.TargetPlatforms)
                {
                    var script = await GenerateBuildScriptForPlatformAsync(platform, applicationLogic, options, cancellationToken);
                    buildScripts.Add(script);
                }

                return buildScripts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating build scripts");
                return buildScripts;
            }
        }

        private async Task<DesktopBuildScript> GenerateBuildScriptForPlatformAsync(
            string platform,
            ApplicationLogic applicationLogic,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var buildScript = new DesktopBuildScript
            {
                Platform = platform,
                ApplicationName = applicationLogic.ApplicationName
            };

            try
            {
                // Generate build script using AI
                var prompt = $@"
Generate a {platform} build script for the following application:
- App Name: {applicationLogic.ApplicationName}
- Platform: {platform}

Requirements:
- Use {platform} build patterns
- Include all necessary build steps
- Use modern {platform} patterns
- Follow {platform} best practices

Generate complete, production-ready build script.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                buildScript.Code = response.Content;
                buildScript.GeneratedAt = DateTimeOffset.UtcNow;
                buildScript.Success = true;

                return buildScript;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating build script for: {Platform}", platform);
                buildScript.Success = false;
                buildScript.ErrorMessage = ex.Message;
                return buildScript;
            }
        }

        private async Task<IEnumerable<DesktopTest>> GenerateTestsForFeatureAsync(
            Nexo.Core.Application.Interfaces.Platform.Feature feature,
            DesktopGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var tests = new List<DesktopTest>();

            try
            {
                // Generate tests using AI
                var prompt = $@"
Generate comprehensive tests for the following {options.Platform} feature:
- Name: {feature.Name}
- Description: {feature.Description}
- Requirements: {string.Join(", ", feature.Requirements)}

Requirements:
- Use {options.Platform} testing frameworks
- Include unit tests for all methods
- Add integration tests
- Include UI tests
- Test error scenarios
- Use proper mocking
- Follow {options.Platform} testing best practices

Generate complete, production-ready test code.
";

                var response = await _modelOrchestrator.GenerateResponseAsync(prompt, cancellationToken);
                
                tests.Add(new DesktopTest
                {
                    Name = $"{feature.Name}Tests",
                    FeatureName = feature.Name,
                    Code = response.Content,
                    GeneratedAt = DateTimeOffset.UtcNow,
                    Success = true
                });

                return tests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tests for feature: {FeatureName}", feature.Name);
                return tests;
            }
        }

        #endregion
    }
}
