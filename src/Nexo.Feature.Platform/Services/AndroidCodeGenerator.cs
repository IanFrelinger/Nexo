using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Platform.Services
{
    /// <summary>
    /// Implementation of Android code generator for native platform code generation.
    /// Part of Epic 6.1: Native Platform Code Generation, Story 6.1.2: Android Native Implementation.
    /// </summary>
    public class AndroidCodeGenerator : IAndroidCodeGenerator
    {
        private readonly ILogger<AndroidCodeGenerator> _logger;

        public AndroidCodeGenerator(ILogger<AndroidCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AndroidCodeGenerationResult> GenerateJetpackComposeCodeAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken = default)
        {
            if (applicationLogic == null)
                throw new ArgumentNullException(nameof(applicationLogic));
            
            if (androidOptions == null)
                throw new ArgumentNullException(nameof(androidOptions));

            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting Android Jetpack Compose code generation");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var result = new AndroidCodeGenerationResult
                {
                    GeneratedCode = new AndroidGeneratedCode()
                };

                if (androidOptions.EnableJetpackCompose)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var composeFiles = await GenerateComposeFilesAsync(applicationLogic, androidOptions, cancellationToken);
                    result.GeneratedCode.ComposeFiles.AddRange(composeFiles);
                }

                if (androidOptions.EnableRoomDatabase)
                {
                    var roomFiles = await GenerateRoomFilesAsync(applicationLogic, androidOptions, cancellationToken);
                    result.GeneratedCode.RoomFiles.AddRange(roomFiles);
                }

                if (androidOptions.EnableKotlinCoroutines)
                {
                    var coroutinesFiles = await GenerateCoroutinesFilesAsync(applicationLogic, androidOptions, cancellationToken);
                    result.GeneratedCode.CoroutinesFiles.AddRange(coroutinesFiles);
                }

                if (androidOptions.EnablePerformanceOptimization)
                {
                    var optimizations = await GeneratePerformanceOptimizationsAsync(applicationLogic, androidOptions, cancellationToken);
                    result.GeneratedCode.AppliedOptimizations.AddRange(optimizations);
                }

                cancellationToken.ThrowIfCancellationRequested();
                
                var uiPatterns = await GenerateUIPatternsAsync(applicationLogic, androidOptions, cancellationToken);
                result.GeneratedCode.AppliedUIPatterns.AddRange(uiPatterns);

                var appConfig = await GenerateAppConfigurationAsync(applicationLogic, androidOptions, cancellationToken);
                result.GeneratedCode.AppConfiguration = appConfig;

                stopwatch.Stop();
                result.IsSuccess = true;
                result.Message = "Android Jetpack Compose code generation completed successfully";
                result.GenerationTime = stopwatch.Elapsed;
                result.GenerationScore = CalculateGenerationScore(result.GeneratedCode);

                _logger.LogInformation("Android Jetpack Compose code generation completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogInformation("Android Jetpack Compose code generation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during Android Jetpack Compose code generation");
                return new AndroidCodeGenerationResult
                {
                    IsSuccess = false,
                    Message = $"Error during Android Jetpack Compose code generation: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    GenerationTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<RoomDatabaseResult> IntegrateRoomDatabaseAsync(
            StandardizedApplicationLogic applicationLogic,
            RoomDatabaseOptions roomOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting Room database integration");

            try
            {
                var result = new RoomDatabaseResult();
                var roomFiles = await GenerateRoomFilesAsync(applicationLogic, new AndroidGenerationOptions(), cancellationToken);
                
                result.GeneratedFiles.AddRange(roomFiles);
                stopwatch.Stop();
                result.IsSuccess = true;
                result.Message = "Room database integration completed successfully";
                result.IntegrationTime = stopwatch.Elapsed;
                result.IntegrationScore = CalculateRoomScore(roomFiles);

                _logger.LogInformation("Room database integration completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during Room database integration");
                return new RoomDatabaseResult
                {
                    IsSuccess = false,
                    Message = $"Error during Room database integration: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    IntegrationTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<KotlinCoroutinesResult> CreateKotlinCoroutinesOptimizationAsync(
            StandardizedApplicationLogic applicationLogic,
            KotlinCoroutinesOptions coroutinesOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting Kotlin coroutines optimization");

            try
            {
                var result = new KotlinCoroutinesResult();
                var generationOptions = new AndroidGenerationOptions { EnableKotlinCoroutines = true };
                var coroutinesFiles = await GenerateCoroutinesFilesAsync(applicationLogic, generationOptions, cancellationToken);
                
                result.GeneratedFiles.AddRange(coroutinesFiles);
                stopwatch.Stop();
                result.IsSuccess = true;
                result.Message = "Kotlin coroutines optimization completed successfully";
                result.OptimizationTime = stopwatch.Elapsed;
                result.OptimizationScore = CalculateCoroutinesScore(coroutinesFiles);

                _logger.LogInformation("Kotlin coroutines optimization completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during Kotlin coroutines optimization");
                return new KotlinCoroutinesResult
                {
                    IsSuccess = false,
                    Message = $"Error during Kotlin coroutines optimization: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    OptimizationTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<AndroidUIPatternResult> GenerateAndroidUIPatternsAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidUIPatternOptions uiPatternOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting Android UI patterns generation");

            try
            {
                var result = new AndroidUIPatternResult();
                var patterns = await GenerateUIPatternsAsync(applicationLogic, new AndroidGenerationOptions(), cancellationToken);
                
                result.GeneratedPatterns.AddRange(patterns);
                stopwatch.Stop();
                result.IsSuccess = true;
                result.Message = "Android UI patterns generation completed successfully";
                result.GenerationTime = stopwatch.Elapsed;
                result.PatternScore = CalculateUIPatternScore(patterns);

                _logger.LogInformation("Android UI patterns generation completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during Android UI patterns generation");
                return new AndroidUIPatternResult
                {
                    IsSuccess = false,
                    Message = $"Error during Android UI patterns generation: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    GenerationTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<AndroidPerformanceResult> CreateAndroidPerformanceOptimizationAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidPerformanceOptions performanceOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting Android performance optimization");

            try
            {
                var result = new AndroidPerformanceResult();
                var optimizations = await GeneratePerformanceOptimizationsAsync(applicationLogic, new AndroidGenerationOptions(), cancellationToken);
                
                result.GeneratedOptimizations.AddRange(optimizations);
                stopwatch.Stop();
                result.IsSuccess = true;
                result.Message = "Android performance optimization completed successfully";
                result.OptimizationTime = stopwatch.Elapsed;
                result.PerformanceScore = CalculatePerformanceScore(optimizations);

                _logger.LogInformation("Android performance optimization completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during Android performance optimization");
                return new AndroidPerformanceResult
                {
                    IsSuccess = false,
                    Message = $"Error during Android performance optimization: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    OptimizationTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<AndroidAppConfigResult> GenerateAndroidAppConfigurationAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidAppConfigOptions configOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting Android app configuration generation");

            try
            {
                var result = new AndroidAppConfigResult();
                var appConfig = await GenerateAppConfigurationAsync(applicationLogic, new AndroidGenerationOptions(), cancellationToken);
                
                result.GeneratedConfiguration = appConfig;
                stopwatch.Stop();
                result.IsSuccess = true;
                result.Message = "Android app configuration generation completed successfully";
                result.GenerationTime = stopwatch.Elapsed;
                result.ConfigurationScore = CalculateAppConfigScore(appConfig);

                _logger.LogInformation("Android app configuration generation completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during Android app configuration generation");
                return new AndroidAppConfigResult
                {
                    IsSuccess = false,
                    Message = $"Error during Android app configuration generation: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    GenerationTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<AndroidCodeValidationResult> ValidateAndroidCodeAsync(
            AndroidGeneratedCode androidCode,
            AndroidValidationOptions validationOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting Android code validation");

            try
            {
                var result = new AndroidCodeValidationResult();
                var errors = new List<string>();
                var warnings = new List<string>();

                if (validationOptions.ValidateSyntax)
                {
                    errors.AddRange(ValidateSyntax(androidCode));
                }

                if (validationOptions.ValidateSemantics)
                {
                    errors.AddRange(ValidateSemantics(androidCode));
                }

                if (validationOptions.ValidatePerformance)
                {
                    warnings.AddRange(ValidatePerformance(androidCode));
                }

                if (validationOptions.ValidateSecurity)
                {
                    warnings.AddRange(ValidateSecurity(androidCode));
                }

                stopwatch.Stop();
                result.IsValid = !errors.Any();
                result.Message = result.IsValid ? "Android code validation passed" : "Android code validation failed";
                result.ValidationErrors = errors;
                result.ValidationWarnings = warnings;
                result.ValidationTime = stopwatch.Elapsed;
                result.ValidationScore = CalculateValidationScore(errors.Count, warnings.Count);

                _logger.LogInformation("Android code validation completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during Android code validation");
                return new AndroidCodeValidationResult
                {
                    IsValid = false,
                    Message = $"Error during Android code validation: {ex.Message}",
                    ValidationErrors = new List<string> { ex.Message },
                    ValidationTime = stopwatch.Elapsed
                };
            }
        }

        public IEnumerable<AndroidUIPattern> GetSupportedAndroidUIPatterns()
        {
            return new List<AndroidUIPattern>
            {
                new AndroidUIPattern
                {
                    Name = "Navigation",
                    Description = "Android Navigation Component",
                    Type = AndroidUIPatternType.Navigation,
                    Implementation = "Navigation Component with NavController"
                },
                new AndroidUIPattern
                {
                    Name = "BottomNavigation",
                    Description = "Bottom Navigation Pattern",
                    Type = AndroidUIPatternType.BottomNavigation,
                    Implementation = "BottomNavigationView with Navigation"
                },
                new AndroidUIPattern
                {
                    Name = "MaterialDesign",
                    Description = "Material Design Components",
                    Type = AndroidUIPatternType.MaterialDesign,
                    Implementation = "Material Design 3 components"
                }
            };
        }

        public IEnumerable<AndroidPerformanceOptimization> GetSupportedAndroidPerformanceOptimizations()
        {
            return new List<AndroidPerformanceOptimization>
            {
                new AndroidPerformanceOptimization
                {
                    Name = "MemoryOptimization",
                    Type = AndroidPerformanceType.Memory,
                    Description = "Memory management optimization",
                    Implementation = "Lazy loading and weak references",
                    PerformanceImpact = 0.8
                },
                new AndroidPerformanceOptimization
                {
                    Name = "BatteryOptimization",
                    Type = AndroidPerformanceType.Battery,
                    Description = "Battery life optimization",
                    Implementation = "Background task management",
                    PerformanceImpact = 0.7
                }
            };
        }

        public IEnumerable<KotlinCoroutinesFeature> GetSupportedKotlinCoroutinesFeatures()
        {
            return new List<KotlinCoroutinesFeature>
            {
                new KotlinCoroutinesFeature
                {
                    Name = "Launch",
                    Type = KotlinCoroutinesFeatureType.Launch,
                    Description = "Fire and forget coroutines",
                    Implementation = "launch { }"
                },
                new KotlinCoroutinesFeature
                {
                    Name = "Async",
                    Type = KotlinCoroutinesFeatureType.Async,
                    Description = "Async coroutines with result",
                    Implementation = "async { }"
                },
                new KotlinCoroutinesFeature
                {
                    Name = "Flow",
                    Type = KotlinCoroutinesFeatureType.Flow,
                    Description = "Reactive streams",
                    Implementation = "flow { }"
                }
            };
        }

        // Private helper methods
        private async Task<List<ComposeFile>> GenerateComposeFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken)
        {
            var files = new List<ComposeFile>();

            // Generate main screen
            files.Add(new ComposeFile
            {
                FileName = "MainScreen.kt",
                FilePath = "app/src/main/java/com/example/app/ui/screens/MainScreen.kt",
                Content = GenerateMainScreenCode(applicationLogic),
                ViewType = ComposeViewType.Screen
            });

            // Generate list screen if patterns exist
            if (applicationLogic.Patterns?.Any() == true)
            {
                files.Add(new ComposeFile
                {
                    FileName = "ListScreen.kt",
                    FilePath = "app/src/main/java/com/example/app/ui/screens/ListScreen.kt",
                    Content = GenerateListScreenCode(applicationLogic),
                    ViewType = ComposeViewType.List
                });
            }

            return files;
        }

        private async Task<List<RoomFile>> GenerateRoomFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken)
        {
            var files = new List<RoomFile>();

            // Generate database
            files.Add(new RoomFile
            {
                FileName = "AppDatabase.kt",
                FilePath = "app/src/main/java/com/example/app/data/database/AppDatabase.kt",
                Content = GenerateRoomDatabaseCode(applicationLogic),
                FileType = RoomFileType.Database,
                Entities = GenerateRoomEntities(applicationLogic)
            });

            return files;
        }

        private async Task<List<CoroutinesFile>> GenerateCoroutinesFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken)
        {
            var files = new List<CoroutinesFile>();

            // Generate coroutines utilities
            files.Add(new CoroutinesFile
            {
                FileName = "CoroutinesUtils.kt",
                FilePath = "app/src/main/java/com/example/app/utils/CoroutinesUtils.kt",
                Content = GenerateCoroutinesUtilsCode(),
                FileType = CoroutinesFileType.CoroutineScope,
                Features = new List<KotlinCoroutinesFeature> { GetSupportedKotlinCoroutinesFeatures().First() }
            });

            return files;
        }

        private async Task<List<AndroidUIPattern>> GenerateUIPatternsAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken)
        {
            var patterns = new List<AndroidUIPattern>();

            // Add navigation pattern
            patterns.Add(new AndroidUIPattern
            {
                Name = "Navigation",
                Type = AndroidUIPatternType.Navigation,
                Description = "Android Navigation Component",
                Implementation = "Navigation Component with NavController"
            });

            // Add list pattern if patterns exist
            if (applicationLogic.Patterns?.Any() == true)
            {
                patterns.Add(new AndroidUIPattern
                {
                    Name = "List",
                    Type = AndroidUIPatternType.List,
                    Description = "List view for data display",
                    Implementation = "LazyColumn with items"
                });
            }

            return patterns;
        }

        private async Task<List<AndroidPerformanceOptimization>> GeneratePerformanceOptimizationsAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken)
        {
            var optimizations = new List<AndroidPerformanceOptimization>();

            // Add memory optimization
            optimizations.Add(new AndroidPerformanceOptimization
            {
                Name = "MemoryOptimization",
                Type = AndroidPerformanceType.Memory,
                Description = "Memory management optimization",
                Implementation = "Lazy loading and weak references",
                PerformanceImpact = 0.8
            });

            // Add battery optimization
            optimizations.Add(new AndroidPerformanceOptimization
            {
                Name = "BatteryOptimization",
                Type = AndroidPerformanceType.Battery,
                Description = "Battery life optimization",
                Implementation = "Background task management",
                PerformanceImpact = 0.7
            });

            return optimizations;
        }

        private async Task<AndroidAppConfiguration> GenerateAppConfigurationAsync(
            StandardizedApplicationLogic applicationLogic,
            AndroidGenerationOptions androidOptions,
            CancellationToken cancellationToken)
        {
            return new AndroidAppConfiguration
            {
                AppName = "GeneratedAndroidApp",
                PackageName = "com.example.generatedandroidapp",
                VersionName = "1.0.0",
                VersionCode = 1,
                MinSdkVersion = 24,
                TargetSdkVersion = 34,
                Permissions = new List<string> { "INTERNET", "ACCESS_NETWORK_STATE" }
            };
        }

        private List<string> ValidateSyntax(AndroidGeneratedCode androidCode)
        {
            var errors = new List<string>();

            // Basic syntax validation
            if (androidCode.ComposeFiles?.Any(f => string.IsNullOrEmpty(f.Content)) == true)
            {
                errors.Add("Empty Compose file content detected");
            }

            if (androidCode.RoomFiles?.Any(f => string.IsNullOrEmpty(f.Content)) == true)
            {
                errors.Add("Empty Room file content detected");
            }

            return errors;
        }

        private List<string> ValidateSemantics(AndroidGeneratedCode androidCode)
        {
            var errors = new List<string>();

            // Basic semantic validation
            if (androidCode.AppConfiguration == null)
            {
                errors.Add("Missing Android app configuration");
            }

            return errors;
        }

        private List<string> ValidatePerformance(AndroidGeneratedCode androidCode)
        {
            var warnings = new List<string>();

            // Performance validation
            if (androidCode.ComposeFiles?.Count > 10)
            {
                warnings.Add("Large number of Compose files may impact build time");
            }

            return warnings;
        }

        private List<string> ValidateSecurity(AndroidGeneratedCode androidCode)
        {
            var warnings = new List<string>();

            // Security validation
            if (androidCode.AppConfiguration?.Permissions?.Contains("INTERNET") == true)
            {
                warnings.Add("Internet permission detected - ensure proper network security");
            }

            return warnings;
        }

        private string GenerateMainScreenCode(StandardizedApplicationLogic applicationLogic)
        {
            return @"package com.example.app.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

@Composable
fun MainScreen() {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(
            text = ""Welcome to Generated Android App"",
            style = MaterialTheme.typography.headlineMedium
        )
        Spacer(modifier = Modifier.height(16.dp))
        Text(
            text = ""Generated with Nexo Framework"",
            style = MaterialTheme.typography.bodyLarge
        )
    }
}";
        }

        private string GenerateListScreenCode(StandardizedApplicationLogic applicationLogic)
        {
            return @"package com.example.app.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp

@Composable
fun ListScreen() {
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(16.dp)
    ) {
        items(10) { index ->
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(vertical = 4.dp)
            ) {
                Text(
                    text = ""Item $index"",
                    modifier = Modifier.padding(16.dp)
                )
            }
        }
    }
}";
        }

        private string GenerateRoomDatabaseCode(StandardizedApplicationLogic applicationLogic)
        {
            return @"package com.example.app.data.database

import androidx.room.Database
import androidx.room.RoomDatabase

@Database(
    entities = [],
    version = 1
)
abstract class AppDatabase : RoomDatabase() {
    // DAOs will be added here
}";
        }

        private string GenerateCoroutinesUtilsCode()
        {
            return @"package com.example.app.utils

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob

object CoroutinesUtils {
    val ioScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    val mainScope = CoroutineScope(SupervisorJob() + Dispatchers.Main)
}";
        }

        private List<RoomEntity> GenerateRoomEntities(StandardizedApplicationLogic applicationLogic)
        {
            return new List<RoomEntity>
            {
                new RoomEntity
                {
                    Name = "User",
                    Columns = new List<RoomColumn>
                    {
                        new RoomColumn { Name = "id", Type = RoomColumnType.Long, IsPrimaryKey = true },
                        new RoomColumn { Name = "name", Type = RoomColumnType.String },
                        new RoomColumn { Name = "email", Type = RoomColumnType.String }
                    }
                }
            };
        }

        private double CalculateGenerationScore(AndroidGeneratedCode generatedCode)
        {
            double score = 0.0;
            int totalComponents = 0;

            // Score Compose files
            if (generatedCode.ComposeFiles?.Any() == true)
            {
                score += generatedCode.ComposeFiles.Count * 0.2;
                totalComponents += generatedCode.ComposeFiles.Count;
            }

            // Score Room files
            if (generatedCode.RoomFiles?.Any() == true)
            {
                score += generatedCode.RoomFiles.Count * 0.15;
                totalComponents += generatedCode.RoomFiles.Count;
            }

            // Score Coroutines files
            if (generatedCode.CoroutinesFiles?.Any() == true)
            {
                score += generatedCode.CoroutinesFiles.Count * 0.1;
                totalComponents += generatedCode.CoroutinesFiles.Count;
            }

            // Score UI patterns
            if (generatedCode.AppliedUIPatterns?.Any() == true)
            {
                score += generatedCode.AppliedUIPatterns.Count * 0.1;
                totalComponents += generatedCode.AppliedUIPatterns.Count;
            }

            // Score optimizations
            if (generatedCode.AppliedOptimizations?.Any() == true)
            {
                score += generatedCode.AppliedOptimizations.Count * 0.1;
                totalComponents += generatedCode.AppliedOptimizations.Count;
            }

            // Score app configuration
            if (generatedCode.AppConfiguration != null)
            {
                score += 0.2;
                totalComponents++;
            }

            return totalComponents > 0 ? Math.Min(score, 1.0) : 0.0;
        }

        private double CalculateRoomScore(List<RoomFile> roomFiles)
        {
            return roomFiles?.Count > 0 ? Math.Min(roomFiles.Count * 0.3, 1.0) : 0.0;
        }

        private double CalculateCoroutinesScore(List<CoroutinesFile> coroutinesFiles)
        {
            return coroutinesFiles?.Count > 0 ? Math.Min(coroutinesFiles.Count * 0.4, 1.0) : 0.0;
        }

        private double CalculateUIPatternScore(List<AndroidUIPattern> patterns)
        {
            return patterns?.Count > 0 ? Math.Min(patterns.Count * 0.25, 1.0) : 0.0;
        }

        private double CalculatePerformanceScore(List<AndroidPerformanceOptimization> optimizations)
        {
            return optimizations?.Count > 0 ? Math.Min(optimizations.Count * 0.3, 1.0) : 0.0;
        }

        private double CalculateAppConfigScore(AndroidAppConfiguration appConfig)
        {
            return appConfig != null ? 1.0 : 0.0;
        }

        private double CalculateValidationScore(int errorCount, int warningCount)
        {
            if (errorCount > 0) return 0.0;
            if (warningCount > 5) return 0.5;
            if (warningCount > 0) return 0.8;
            return 1.0;
        }
    }
} 