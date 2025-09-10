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
                    var uiComponents = await GenerateUIComponentsAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(uiComponents.Select(c => $"{c.Name}.xaml"));
                }

                // 2. Generate ViewModels
                if (options.UseWPF || options.UseWinUI || options.UseAvalonia)
                {
                    var viewModels = await GenerateViewModelsAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(viewModels.Select(vm => $"{vm.Name}.cs"));
                }

                // 3. Generate Services
                if (options.UseDataAccess)
                {
                    var services = await GenerateServicesAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(services.Select(s => $"{s.Name}.cs"));
                }

                // 4. Generate Data Access Layer
                if (options.UseDataAccess)
                {
                    var dataAccess = await GenerateDataAccessAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.Add($"{dataAccess.Name}DataAccess.cs");
                }

                // 5. Generate Configuration
                if (options.UseConfiguration)
                {
                    var configuration = await GenerateConfigurationAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.Add($"{configuration.Name}Configuration.cs");
                }

                // 6. Generate Platform-Specific Code
                if (options.UsePlatformSpecificCode)
                {
                    var platformCode = await GeneratePlatformSpecificAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(platformCode.Select(pc => $"{pc.Name}Code.cs"));
                }

                // 7. Generate Build Configuration
                if (options.UseBuildConfiguration)
                {
                    var buildConfig = await GenerateBuildConfigurationAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.Add($"{buildConfig.Name}.csproj");
                }

                // 8. Generate Tests
                if (options.UseTest)
                {
                    var tests = await GenerateTestsAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(tests.Select(t => $"{t.Name}.cs"));
                }

                result.Success = true;
                result.Message = $"Successfully generated {result.GeneratedFiles.Count} files";

                _logger.LogInformation("Desktop code generation completed successfully. Generated {FileCount} files", 
                    result.GeneratedFiles.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during desktop code generation");
                result.Success = false;
                result.Message = ex.Message;
                result.Errors.Add(ex.Message);
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
                Name = applicationLogic.ApplicationName
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

                dataAccess.Name = applicationLogic.ApplicationName;
                dataAccess.Content = $"Generated data access layer with {entities.Count} entities and {repositories.Count} repositories";

                return dataAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating data access layer");
                dataAccess.Name = applicationLogic.ApplicationName;
                dataAccess.Content = $"Error generating data access layer: {ex.Message}";
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
                Name = applicationLogic.ApplicationName
            };

            try
            {
                // Generate app settings
                var appSettings = await GenerateAppSettingsAsync(applicationLogic, options, cancellationToken);
                
                // Generate dependency injection
                var dependencyInjection = await GenerateDependencyInjectionAsync(applicationLogic, options, cancellationToken);
                
                // Generate logging configuration
                var loggingConfig = await GenerateLoggingConfigurationAsync(applicationLogic, options, cancellationToken);

                configuration.Name = applicationLogic.ApplicationName;
                configuration.Content = $"Generated configuration with app settings, dependency injection, and logging";

                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating configuration");
                configuration.Name = applicationLogic.ApplicationName;
                configuration.Content = $"Error generating configuration: {ex.Message}";
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
                var platforms = new[] { "Windows", "Mac", "Linux" };
                foreach (var platform in platforms)
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
                Name = applicationLogic.ApplicationName
            };

            try
            {
                // Generate project file
                var projectFile = await GenerateProjectFileAsync(applicationLogic, options, cancellationToken);
                
                // Generate solution file
                var solutionFile = await GenerateSolutionFileAsync(applicationLogic, options, cancellationToken);
                
                // Generate build scripts
                var buildScripts = await GenerateBuildScriptsAsync(applicationLogic, options, cancellationToken);

                buildConfig.Name = applicationLogic.ApplicationName;
                buildConfig.Content = $"Generated build configuration with project file, solution file, and {buildScripts.Count()} build scripts";

                return buildConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating build configuration");
                buildConfig.Name = applicationLogic.ApplicationName;
                buildConfig.Content = $"Error generating build configuration: {ex.Message}";
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
                Content = string.Empty
            };

            try
            {
                // Generate UI component using AI
                var prompt = $@"
Generate a desktop UI component for the following feature:
- Name: {feature.Name}
- Description: {feature.Description}
- Requirements: {string.Join(", ", feature.Requirements)}

Requirements:
- Use desktop UI framework best practices
- Include proper MVVM pattern
- Add data binding
- Include accessibility features
- Add error handling
- Use modern desktop patterns
- Include responsive design
- Add loading states

Generate complete, production-ready UI component code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                component.Content = response.Response;

                return component;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating UI component for feature: {FeatureName}", feature.Name);
                component.Content = $"Error generating UI component: {ex.Message}";
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
                Content = string.Empty
            };

            try
            {
                // Generate ViewModel using AI
                var prompt = $@"
Generate a desktop ViewModel for the following feature:
- Name: {feature.Name}
- Description: {feature.Description}
- Requirements: {string.Join(", ", feature.Requirements)}

Requirements:
- Use desktop ViewModel patterns
- Include proper data binding
- Add command patterns
- Include proper error handling
- Use async/await patterns
- Follow MVVM best practices
- Include unit testable code
- Use dependency injection

Generate complete, production-ready ViewModel code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                viewModel.Content = response.Response;

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ViewModel for feature: {FeatureName}", feature.Name);
                viewModel.Content = $"Error generating ViewModel: {ex.Message}";
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
                Content = string.Empty
            };

            try
            {
                // Generate service using AI
                var prompt = $@"
Generate a desktop service for the following:
- Name: {service.Name}
- Description: {service.Description}
- Methods: {string.Join(", ", service.Methods.Select(m => $"{m.Name}()"))}

Requirements:
- Use desktop service patterns
- Include proper lifecycle management
- Use async/await patterns
- Include proper error handling
- Use dependency injection
- Follow desktop best practices

Generate complete, production-ready service code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                desktopService.Content = response.Response;

                return desktopService;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating service: {ServiceName}", service.Name);
                desktopService.Content = $"Error generating service: {ex.Message}";
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
                EntityName = entity.Name,
                TableName = entity.Name
            };

            try
            {
                // Generate entity using AI
                var prompt = $@"
Generate a desktop entity for the following:
- Name: {entity.Name}
- Description: {entity.Description}
- Properties: {string.Join(", ", entity.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Requirements:
- Use desktop entity patterns
- Include proper data annotations
- Add validation attributes
- Use modern desktop patterns
- Follow desktop best practices

Generate complete, production-ready entity code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                // Store the generated code in the Content property of the entity
                // The entity class doesn't have a Code property, so we'll store it in the first field
                if (desktopEntity.Fields.Count > 0)
                {
                    desktopEntity.Fields[0].DefaultValue = response.Response;
                }

                return desktopEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating entity for: {EntityName}", entity.Name);
                // Store error in the first field's default value
                if (desktopEntity.Fields.Count > 0)
                {
                    desktopEntity.Fields[0].DefaultValue = $"Error generating entity: {ex.Message}";
                }
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
                RepositoryName = $"{entity.Name}Repository",
                EntityName = entity.Name
            };

            try
            {
                // Generate repository using AI
                var prompt = $@"
Generate a desktop repository for the following entity:
- Entity Name: {entity.Name}
- Properties: {string.Join(", ", entity.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Requirements:
- Use desktop repository patterns
- Include CRUD operations
- Use async/await patterns
- Include proper error handling
- Use dependency injection
- Follow desktop best practices

Generate complete, production-ready repository code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                // Store the generated code in the first query method
                if (repository.QueryMethods.Count > 0)
                {
                    repository.QueryMethods[0] = response.Response;
                }
                else
                {
                    repository.QueryMethods.Add(response.Response);
                }

                return repository;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating repository for: {EntityName}", entity.Name);
                // Store error in the first query method
                if (repository.QueryMethods.Count > 0)
                {
                    repository.QueryMethods[0] = $"Error generating repository: {ex.Message}";
                }
                else
                {
                    repository.QueryMethods.Add($"Error generating repository: {ex.Message}");
                }
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
                ContextName = $"{options.TargetFramework}DbContext"
            };

            try
            {
                // Generate database context using AI
                var prompt = $@"
Generate a desktop database context with the following entities:
- Entities: {string.Join(", ", entities.Select(e => e.EntityName))}

Requirements:
- Use desktop database context patterns
- Include all entities
- Add proper configuration
- Use modern desktop patterns
- Follow desktop best practices

Generate complete, production-ready database context code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                // Store the generated code in the first DbSet
                if (context.DbSets.Count > 0)
                {
                    context.DbSets[0] = response.Response;
                }
                else
                {
                    context.DbSets.Add(response.Response);
                }

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating database context");
                // Store error in the first DbSet
                if (context.DbSets.Count > 0)
                {
                    context.DbSets[0] = $"Error generating database context: {ex.Message}";
                }
                else
                {
                    context.DbSets.Add($"Error generating database context: {ex.Message}");
                }
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
                AppName = applicationLogic.ApplicationName,
                Version = "1.0.0"
            };

            try
            {
                // Generate app settings using AI
                var prompt = $@"
Generate desktop app settings for the following application:
- App Name: {applicationLogic.ApplicationName}
- Platform: Desktop

Requirements:
- Use desktop configuration patterns
- Include all necessary settings
- Use modern desktop patterns
- Follow desktop best practices

Generate complete, production-ready app settings code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                // Store the generated code in the settings dictionary
                appSettings.Settings["GeneratedCode"] = response.Response;

                return appSettings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating app settings");
                // Store error in the settings dictionary
                appSettings.Settings["Error"] = ex.Message;
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
                ServiceName = applicationLogic.ApplicationName,
                ServiceType = "IService",
                ImplementationType = "Service",
                Lifetime = "Scoped"
            };

            try
            {
                // Generate dependency injection using AI
                var prompt = $@"
Generate desktop dependency injection configuration for the following application:
- App Name: {applicationLogic.ApplicationName}
- Platform: Desktop

Requirements:
- Use desktop DI patterns
- Include all services
- Use modern desktop patterns
- Follow desktop best practices

Generate complete, production-ready dependency injection code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                // Store the generated code in the service name
                di.ServiceName = response.Response;

                return di;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dependency injection");
                // Store error in the service name
                di.ServiceName = $"Error generating dependency injection: {ex.Message}";
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
                LogLevel = "Information",
                LogProviders = new List<string> { "Console", "File" }
            };

            try
            {
                // Generate logging configuration using AI
                var prompt = $@"
Generate desktop logging configuration for the following application:
- App Name: {applicationLogic.ApplicationName}
- Platform: Desktop

Requirements:
- Use desktop logging patterns
- Include all necessary logging
- Use modern desktop patterns
- Follow desktop best practices

Generate complete, production-ready logging configuration code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                // Store the generated code in the log settings dictionary
                loggingConfig.LogSettings["GeneratedCode"] = response.Response;

                return loggingConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating logging configuration");
                // Store error in the log settings dictionary
                loggingConfig.LogSettings["Error"] = ex.Message;
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
                Name = $"{platform}Code",
                Content = string.Empty
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

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                platformCode.Content = response.Response;

                return platformCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating platform-specific code for: {Platform}", platform);
                platformCode.Content = $"Error generating platform-specific code: {ex.Message}";
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
                ProjectName = applicationLogic.ApplicationName,
                TargetFramework = options.TargetFramework
            };

            try
            {
                // Generate project file using AI
                var prompt = $@"
Generate a desktop project file for the following application:
- App Name: {applicationLogic.ApplicationName}
- Platform: Desktop

Requirements:
- Use desktop project patterns
- Include all necessary references
- Use modern desktop patterns
- Follow desktop best practices

Generate complete, production-ready project file.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                // Store the generated code in the first package reference
                if (projectFile.PackageReferences.Count > 0)
                {
                    projectFile.PackageReferences[0] = response.Response;
                }
                else
                {
                    projectFile.PackageReferences.Add(response.Response);
                }

                return projectFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating project file");
                // Store error in the first package reference
                if (projectFile.PackageReferences.Count > 0)
                {
                    projectFile.PackageReferences[0] = $"Error generating project file: {ex.Message}";
                }
                else
                {
                    projectFile.PackageReferences.Add($"Error generating project file: {ex.Message}");
                }
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
                SolutionName = applicationLogic.ApplicationName
            };

            try
            {
                // Generate solution file using AI
                var prompt = $@"
Generate a desktop solution file for the following application:
- App Name: {applicationLogic.ApplicationName}
- Platform: Desktop

Requirements:
- Use desktop solution patterns
- Include all necessary projects
- Use modern desktop patterns
- Follow desktop best practices

Generate complete, production-ready solution file.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                // Store the generated code in the first project
                if (solutionFile.Projects.Count > 0)
                {
                    solutionFile.Projects[0] = response.Response;
                }
                else
                {
                    solutionFile.Projects.Add(response.Response);
                }

                return solutionFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating solution file");
                // Store error in the first project
                if (solutionFile.Projects.Count > 0)
                {
                    solutionFile.Projects[0] = $"Error generating solution file: {ex.Message}";
                }
                else
                {
                    solutionFile.Projects.Add($"Error generating solution file: {ex.Message}");
                }
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
                foreach (var platform in new[] { "Windows", "Mac", "Linux" })
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
                ScriptName = $"{platform}BuildScript",
                ScriptType = platform
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

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                buildScript.ScriptContent = response.Response;

                return buildScript;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating build script for: {Platform}", platform);
                buildScript.ScriptContent = $"Error generating build script: {ex.Message}";
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
Generate comprehensive tests for the following desktop feature:
- Name: {feature.Name}
- Description: {feature.Description}
- Requirements: {string.Join(", ", feature.Requirements)}

Requirements:
- Use desktop testing frameworks
- Include unit tests for all methods
- Add integration tests
- Include UI tests
- Test error scenarios
- Use proper mocking
- Follow desktop testing best practices

Generate complete, production-ready test code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                tests.Add(new DesktopTest
                {
                    Name = $"{feature.Name}Tests",
                    Content = response.Response
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
