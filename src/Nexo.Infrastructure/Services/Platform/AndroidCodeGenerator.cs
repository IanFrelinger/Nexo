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
    /// Android native code generator for Phase 6.
    /// Generates native Android code with Jetpack Compose, Room, and Kotlin coroutines.
    /// </summary>
    public class AndroidCodeGenerator : IAndroidCodeGenerator
    {
        private readonly ILogger<AndroidCodeGenerator> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;

        public AndroidCodeGenerator(
            ILogger<AndroidCodeGenerator> logger,
            IModelOrchestrator modelOrchestrator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
        }

        /// <summary>
        /// Generates native Android code from application logic.
        /// </summary>
        public async Task<AndroidGenerationResult> GenerateCodeAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting Android code generation for application: {ApplicationName}", 
                applicationLogic.ApplicationName);

            var result = new AndroidGenerationResult
            {
                Success = false,
                Message = "Starting Android code generation"
            };

            try
            {
                // 1. Generate Jetpack Compose UI
                if (options.UseCompose)
                {
                    var composeUI = await GenerateJetpackComposeUIAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(composeUI.Select(s => s.Name));
                }

                // 2. Generate Room Database
                if (options.UseRoom)
                {
                    var roomDatabase = await GenerateRoomDatabaseAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.Add(roomDatabase.Name);
                }

                // 3. Generate ViewModels
                if (options.UseViewModel)
                {
                    var viewModels = await GenerateViewModelsAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(viewModels.Select(v => v.Name));
                }

                // 4. Generate Repositories
                if (options.UseRepository)
                {
                    var repositories = await GenerateRepositoriesAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(repositories.Select(r => r.Name));
                }

                // 5. Generate Services
                if (options.UseService)
                {
                    var services = await GenerateServicesAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(services.Select(s => s.Name));
                }

                // 6. Generate Dependency Injection
                if (options.UseDependencyInjection)
                {
                    var dependencyInjection = await GenerateDependencyInjectionAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.Add(dependencyInjection.Name);
                }

                // 7. Generate App Configuration
                if (options.UseAppConfiguration)
                {
                    var appConfiguration = await GenerateAppConfigurationAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.Add(appConfiguration.Name);
                }

                // 8. Generate Tests
                if (options.UseTest)
                {
                    var tests = await GenerateTestsAsync(applicationLogic, options, cancellationToken);
                    result.GeneratedFiles.AddRange(tests.Select(t => t.Name));
                }

                result.Success = true;
                result.Message = "Android code generation completed successfully";

                _logger.LogInformation("Android code generation completed successfully with {FileCount} files generated", 
                    result.GeneratedFiles.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Android code generation");
                result.Success = false;
                result.Message = ex.Message;
                result.Errors.Add(ex.Message);
                return result;
            }
        }

        /// <summary>
        /// Generates Jetpack Compose UI from application logic.
        /// </summary>
        public async Task<IEnumerable<ComposeScreen>> GenerateJetpackComposeUIAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var screens = new List<ComposeScreen>();

            try
            {
                foreach (var feature in applicationLogic.Features)
                {
                    var screen = await GenerateComposeScreenForFeatureAsync(feature, options, cancellationToken);
                    screens.Add(screen);
                }

                return screens;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Jetpack Compose UI");
                return screens;
            }
        }

        /// <summary>
        /// Generates Room database from application logic.
        /// </summary>
        public async Task<RoomDatabase> GenerateRoomDatabaseAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var database = new RoomDatabase
            {
                Name = $"{applicationLogic.ApplicationName}Database"
            };

            try
            {
                // Generate entities
                var entities = new List<RoomEntity>();
                foreach (var entity in applicationLogic.Entities)
                {
                    var roomEntity = await GenerateRoomEntityAsync(entity, options, cancellationToken);
                    entities.Add(roomEntity);
                }

                // Generate DAOs
                var daos = new List<RoomDAO>();
                foreach (var entity in applicationLogic.Entities)
                {
                    var dao = await GenerateRoomDAOAsync(entity, options, cancellationToken);
                    daos.Add(dao);
                }

                // Generate database class
                var databaseCode = await GenerateRoomDatabaseClassAsync(entities, daos, options, cancellationToken);

                database.Entities = entities.Select(e => e.EntityName).ToList();

                return database;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Room database");
                return database;
            }
        }

        /// <summary>
        /// Generates ViewModels from application logic.
        /// </summary>
        public async Task<IEnumerable<AndroidViewModel>> GenerateViewModelsAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var viewModels = new List<AndroidViewModel>();

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
        /// Generates repositories from application logic.
        /// </summary>
        public async Task<IEnumerable<AndroidRepository>> GenerateRepositoriesAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var repositories = new List<AndroidRepository>();

            try
            {
                foreach (var entity in applicationLogic.Entities)
                {
                    var repository = await GenerateRepositoryForEntityAsync(entity, options, cancellationToken);
                    repositories.Add(repository);
                }

                return repositories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating repositories");
                return repositories;
            }
        }

        /// <summary>
        /// Generates services from application logic.
        /// </summary>
        public async Task<IEnumerable<AndroidService>> GenerateServicesAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var services = new List<AndroidService>();

            try
            {
                foreach (var service in applicationLogic.Services)
                {
                    var androidService = await GenerateServiceAsync(service, options, cancellationToken);
                    services.Add(androidService);
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
        /// Generates dependency injection configuration.
        /// </summary>
        public async Task<DependencyInjectionConfig> GenerateDependencyInjectionAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var config = new DependencyInjectionConfig
            {
                Name = "DependencyInjectionConfig"
            };

            try
            {
                // Generate Hilt modules
                var modules = await GenerateHiltModulesAsync(applicationLogic, options, cancellationToken);
                
                // Generate Application class
                var applicationClass = await GenerateApplicationClassAsync(applicationLogic, options, cancellationToken);

                config.Content = $"Generated {modules.Count} modules and application class";

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dependency injection");
                config.Content = $"Error: {ex.Message}";
                return config;
            }
        }

        /// <summary>
        /// Generates app configuration files.
        /// </summary>
        public Task<AndroidAppConfiguration> GenerateAppConfigurationAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var configuration = new AndroidAppConfiguration
                {
                    Name = "AndroidAppConfiguration",
                    Content = $"Generated configuration for {applicationLogic.ApplicationName}"
                };

                return Task.FromResult(configuration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating app configuration");
                return Task.FromResult(new AndroidAppConfiguration());
            }
        }

        /// <summary>
        /// Generates unit tests for Android code.
        /// </summary>
        public async Task<IEnumerable<AndroidTest>> GenerateTestsAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var tests = new List<AndroidTest>();

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

        private async Task<ComposeScreen> GenerateComposeScreenForFeatureAsync(
            Nexo.Core.Application.Interfaces.Platform.Feature feature,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var screen = new ComposeScreen
            {
                Name = $"{feature.Name}Screen"
            };

            try
            {
                // Generate Jetpack Compose code using AI
                var prompt = $@"
Generate a Jetpack Compose screen for the following feature:
- Name: {feature.Name}
- Description: {feature.Description}
- Requirements: {string.Join(", ", feature.Requirements)}

Requirements:
- Use Jetpack Compose with Material 3
- Follow MVVM pattern
- Include proper state management
- Add accessibility support
- Include error handling
- Use modern Kotlin syntax
- Follow Android Material Design guidelines
- Use coroutines for async operations

Generate complete, production-ready Jetpack Compose code.
";

                var request = new Nexo.Feature.AI.Models.ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                screen.Content = response.Response;

                return screen;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Compose screen for feature: {FeatureName}", feature.Name);
                return screen;
            }
        }

        private async Task<RoomEntity> GenerateRoomEntityAsync(
            Entity entity,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var roomEntity = new RoomEntity
            {
                EntityName = entity.Name,
                TableName = entity.Name.ToLower()
            };

            try
            {
                // Generate Room entity using AI
                var prompt = $@"
Generate a Room entity for the following:
- Name: {entity.Name}
- Description: {entity.Description}
- Properties: {string.Join(", ", entity.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Requirements:
- Use Room annotations (@Entity, @PrimaryKey, @ColumnInfo)
- Include proper data types
- Add validation annotations
- Use modern Kotlin data classes
- Follow Room best practices

Generate complete, production-ready Room entity code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                // Add fields based on entity properties
                foreach (var property in entity.Properties)
                {
                    roomEntity.Fields.Add(new EntityField
                    {
                        FieldName = property.Name,
                        FieldType = property.Type,
                        IsPrimaryKey = property.Name == "Id",
                        IsNullable = property.Type.Contains("?")
                    });
                }

                return roomEntity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Room entity for: {EntityName}", entity.Name);
                return roomEntity;
            }
        }

        private async Task<RoomDAO> GenerateRoomDAOAsync(
            Entity entity,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var dao = new RoomDAO
            {
                DaoName = $"{entity.Name}DAO",
                EntityName = entity.Name
            };

            try
            {
                // Generate Room DAO using AI
                var prompt = $@"
Generate a Room DAO for the following entity:
- Entity Name: {entity.Name}
- Properties: {string.Join(", ", entity.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Requirements:
- Use Room annotations (@Dao, @Query, @Insert, @Update, @Delete)
- Include CRUD operations
- Add proper query methods
- Use suspend functions for coroutines
- Include transaction support
- Follow Room DAO best practices

Generate complete, production-ready Room DAO code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                // Add basic query methods
                dao.QueryMethods.Add($"SELECT * FROM {entity.Name.ToLower()}");
                dao.InsertMethods.Add($"INSERT INTO {entity.Name.ToLower()}");
                dao.UpdateMethods.Add($"UPDATE {entity.Name.ToLower()}");
                dao.DeleteMethods.Add($"DELETE FROM {entity.Name.ToLower()}");

                return dao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Room DAO for: {EntityName}", entity.Name);
                return dao;
            }
        }

        private async Task<string> GenerateRoomDatabaseClassAsync(
            List<RoomEntity> entities,
            List<RoomDAO> daos,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                var prompt = $@"
Generate a Room database class with the following:
- Entities: {string.Join(", ", entities.Select(e => e.EntityName))}
- DAOs: {string.Join(", ", daos.Select(d => d.DaoName))}

Requirements:
- Use @Database annotation
- Include all entities and DAOs
- Add migration support
- Include proper versioning
- Use modern Room patterns
- Follow Room database best practices

Generate complete, production-ready Room database code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                return response.Response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Room database class");
                return string.Empty;
            }
        }

        private async Task<AndroidViewModel> GenerateViewModelForFeatureAsync(
            Nexo.Core.Application.Interfaces.Platform.Feature feature,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var viewModel = new AndroidViewModel
            {
                Name = $"{feature.Name}ViewModel"
            };

            try
            {
                // Generate ViewModel using AI
                var prompt = $@"
Generate an Android ViewModel for the following feature:
- Name: {feature.Name}
- Description: {feature.Description}
- Requirements: {string.Join(", ", feature.Requirements)}

Requirements:
- Use AndroidX ViewModel
- Include proper state management
- Use StateFlow/Flow for reactive programming
- Include proper error handling
- Use coroutines for async operations
- Follow MVVM pattern
- Include unit testable code
- Use Hilt for dependency injection

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
                return viewModel;
            }
        }

        private async Task<AndroidRepository> GenerateRepositoryForEntityAsync(
            Entity entity,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var repository = new AndroidRepository
            {
                Name = $"{entity.Name}Repository"
            };

            try
            {
                // Generate Repository using AI
                var prompt = $@"
Generate an Android Repository for the following entity:
- Entity Name: {entity.Name}
- Properties: {string.Join(", ", entity.Properties.Select(p => $"{p.Name}: {p.Type}"))}

Requirements:
- Use repository pattern
- Include data source abstraction
- Use coroutines for async operations
- Include proper error handling
- Use Flow for reactive data
- Follow clean architecture
- Use Hilt for dependency injection

Generate complete, production-ready Repository code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                repository.Content = response.Response;

                return repository;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Repository for: {EntityName}", entity.Name);
                return repository;
            }
        }

        private async Task<AndroidService> GenerateServiceAsync(
            Service service,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var androidService = new AndroidService
            {
                Name = service.Name
            };

            try
            {
                // Generate service using AI
                var prompt = $@"
Generate an Android service for the following:
- Name: {service.Name}
- Description: {service.Description}
- Methods: {string.Join(", ", service.Methods.Select(m => $"{m.Name}()"))}

Requirements:
- Use modern Android service patterns
- Include proper lifecycle management
- Use coroutines for async operations
- Include proper error handling
- Use Hilt for dependency injection
- Follow Android service best practices

Generate complete, production-ready service code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                androidService.Content = response.Response;

                return androidService;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating service: {ServiceName}", service.Name);
                return androidService;
            }
        }

        private Task<List<HiltModule>> GenerateHiltModulesAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var modules = new List<HiltModule>();

            try
            {
                // Generate database module
                var databaseModule = new HiltModule
                {
                    ModuleName = "DatabaseModule"
                };
                modules.Add(databaseModule);

                // Generate repository module
                var repositoryModule = new HiltModule
                {
                    ModuleName = "RepositoryModule"
                };
                modules.Add(repositoryModule);

                return Task.FromResult(modules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Hilt modules");
                return Task.FromResult(modules);
            }
        }

        private async Task<string> GenerateDatabaseModuleAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                var prompt = $@"
Generate a Hilt database module for Android with the following entities:
- Entities: {string.Join(", ", applicationLogic.Entities.Select(e => e.Name))}

Requirements:
- Use @Module and @InstallIn annotations
- Provide database instance
- Provide DAO instances
- Use proper scoping
- Follow Hilt best practices

Generate complete, production-ready Hilt module code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                return response.Response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating database module");
                return string.Empty;
            }
        }

        private async Task<string> GenerateRepositoryModuleAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                var prompt = $@"
Generate a Hilt repository module for Android with the following repositories:
- Repositories: {string.Join(", ", applicationLogic.Entities.Select(e => $"{e.Name}Repository"))}

Requirements:
- Use @Module and @InstallIn annotations
- Provide repository instances
- Use proper scoping
- Follow Hilt best practices

Generate complete, production-ready Hilt module code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                return response.Response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating repository module");
                return string.Empty;
            }
        }

        private async Task<string> GenerateApplicationClassAsync(
            ApplicationLogic applicationLogic,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                var prompt = $@"
Generate an Android Application class with Hilt for:
- App Name: {applicationLogic.ApplicationName}

Requirements:
- Use @HiltAndroidApp annotation
- Include proper initialization
- Follow Hilt best practices

Generate complete, production-ready Application class code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                return response.Response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Application class");
                return string.Empty;
            }
        }

        private async Task<IEnumerable<AndroidTest>> GenerateTestsForFeatureAsync(
            Nexo.Core.Application.Interfaces.Platform.Feature feature,
            AndroidGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var tests = new List<AndroidTest>();

            try
            {
                // Generate unit tests using AI
                var prompt = $@"
Generate comprehensive unit tests for the following Android feature:
- Name: {feature.Name}
- Description: {feature.Description}
- Requirements: {string.Join(", ", feature.Requirements)}

Requirements:
- Use JUnit 5 and MockK
- Include unit tests for all methods
- Add integration tests
- Include UI tests with Compose Testing
- Test error scenarios
- Use proper mocking
- Follow Android testing best practices

Generate complete, production-ready test code.
";

                var request = new ModelRequest { Input = prompt };
                var response = await _modelOrchestrator.ProcessAsync(request, cancellationToken);
                
                tests.Add(new AndroidTest
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

        private string[] GetRequiredPermissions(ApplicationLogic applicationLogic)
        {
            var permissions = new List<string>
            {
                "android.permission.INTERNET",
                "android.permission.ACCESS_NETWORK_STATE"
            };

            if (applicationLogic.Features.Any(f => f.RequiresCamera))
                permissions.Add("android.permission.CAMERA");
            
            if (applicationLogic.Features.Any(f => f.RequiresLocation))
            {
                permissions.Add("android.permission.ACCESS_FINE_LOCATION");
                permissions.Add("android.permission.ACCESS_COARSE_LOCATION");
            }
            
            if (applicationLogic.Features.Any(f => f.RequiresMicrophone))
                permissions.Add("android.permission.RECORD_AUDIO");
            
            if (applicationLogic.Features.Any(f => f.RequiresBluetooth))
                permissions.Add("android.permission.BLUETOOTH");

            return permissions.ToArray();
        }

        private Dictionary<string, object> GenerateManifestSettings(ApplicationLogic applicationLogic)
        {
            return new Dictionary<string, object>
            {
                ["android:label"] = applicationLogic.ApplicationName,
                ["android:theme"] = "@style/Theme.AppCompat.Light.NoActionBar",
                ["android:allowBackup"] = true,
                ["android:supportsRtl"] = true
            };
        }

        private Dictionary<string, string> GenerateBuildGradleSettings(AndroidGenerationOptions options)
        {
            return new Dictionary<string, string>
            {
                ["compileSdk"] = "34",
                ["targetSdk"] = "34",
                ["minSdk"] = "24",
                ["kotlinVersion"] = "1.9.0",
                ["composeVersion"] = "1.5.0",
                ["hiltVersion"] = "2.48",
                ["roomVersion"] = "2.5.0"
            };
        }

        #endregion
    }
}
