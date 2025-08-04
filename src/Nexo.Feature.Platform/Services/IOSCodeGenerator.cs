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
    /// Implementation of iOS code generator for native platform code generation.
    /// Part of Epic 6.1: Native Platform Code Generation, Story 6.1.1: iOS Native Implementation.
    /// </summary>
    public class IOSCodeGenerator : IIOSCodeGenerator
    {
        private readonly ILogger<IOSCodeGenerator> _logger;

        public IOSCodeGenerator(ILogger<IOSCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IOSCodeGenerationResult> GenerateSwiftUICodeAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken = default)
        {
            if (applicationLogic == null)
                throw new ArgumentNullException(nameof(applicationLogic));
            
            if (iosOptions == null)
                throw new ArgumentNullException(nameof(iosOptions));

            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting iOS SwiftUI code generation");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var result = new IOSCodeGenerationResult
                {
                    GeneratedCode = new IOSGeneratedCode()
                };

                if (iosOptions.EnableSwiftUI)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var swiftUIFiles = await GenerateSwiftUIFilesAsync(applicationLogic, iosOptions, cancellationToken);
                    result.GeneratedCode.SwiftUIFiles.AddRange(swiftUIFiles);
                }

                if (iosOptions.EnableCoreData)
                {
                    var coreDataFiles = await GenerateCoreDataFilesAsync(applicationLogic, iosOptions, cancellationToken);
                    result.GeneratedCode.CoreDataFiles.AddRange(coreDataFiles);
                }

                if (iosOptions.EnableMetalGraphics)
                {
                    var metalFiles = await GenerateMetalFilesAsync(applicationLogic, iosOptions, cancellationToken);
                    result.GeneratedCode.MetalFiles.AddRange(metalFiles);
                }

                if (iosOptions.EnablePerformanceOptimization)
                {
                    var optimizations = await GeneratePerformanceOptimizationsAsync(applicationLogic, iosOptions, cancellationToken);
                    result.GeneratedCode.AppliedOptimizations.AddRange(optimizations);
                }

                cancellationToken.ThrowIfCancellationRequested();
                
                var uiPatterns = await GenerateUIPatternsAsync(applicationLogic, iosOptions, cancellationToken);
                result.GeneratedCode.AppliedUIPatterns.AddRange(uiPatterns);

                var appConfig = await GenerateAppConfigurationAsync(applicationLogic, iosOptions, cancellationToken);
                result.GeneratedCode.AppConfiguration = appConfig;

                stopwatch.Stop();
                result.IsSuccess = true;
                result.Message = "iOS SwiftUI code generation completed successfully";
                result.GenerationTime = stopwatch.Elapsed;
                result.GenerationScore = CalculateGenerationScore(result.GeneratedCode);

                _logger.LogInformation("iOS SwiftUI code generation completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                _logger.LogInformation("iOS SwiftUI code generation was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during iOS SwiftUI code generation");
                return new IOSCodeGenerationResult
                {
                    IsSuccess = false,
                    Message = $"Error during iOS SwiftUI code generation: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    GenerationTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<CoreDataIntegrationResult> IntegrateCoreDataAsync(
            StandardizedApplicationLogic applicationLogic,
            CoreDataOptions coreDataOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting Core Data integration");

            try
            {
                var result = new CoreDataIntegrationResult();
                var coreDataFiles = await GenerateCoreDataFilesAsync(applicationLogic, new IOSGenerationOptions(), cancellationToken);
                
                result.GeneratedFiles.AddRange(coreDataFiles);
                stopwatch.Stop();
                result.IsSuccess = true;
                result.Message = "Core Data integration completed successfully";
                result.IntegrationTime = stopwatch.Elapsed;
                result.IntegrationScore = CalculateCoreDataScore(coreDataFiles);

                _logger.LogInformation("Core Data integration completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during Core Data integration");
                return new CoreDataIntegrationResult
                {
                    IsSuccess = false,
                    Message = $"Error during Core Data integration: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    IntegrationTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<MetalGraphicsResult> CreateMetalGraphicsOptimizationAsync(
            StandardizedApplicationLogic applicationLogic,
            MetalGraphicsOptions metalOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting Metal graphics optimization");

            try
            {
                var result = new MetalGraphicsResult();
                var generationOptions = new IOSGenerationOptions { EnableMetalGraphics = true };
                var metalFiles = await GenerateMetalFilesAsync(applicationLogic, generationOptions, cancellationToken);
                
                result.GeneratedFiles.AddRange(metalFiles);
                stopwatch.Stop();
                result.IsSuccess = true;
                result.Message = "Metal graphics optimization completed successfully";
                result.OptimizationTime = stopwatch.Elapsed;
                result.OptimizationScore = CalculateMetalScore(metalFiles);

                _logger.LogInformation("Metal graphics optimization completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during Metal graphics optimization");
                return new MetalGraphicsResult
                {
                    IsSuccess = false,
                    Message = $"Error during Metal graphics optimization: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    OptimizationTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<IOSUIPatternResult> GenerateIOSUIPatternsAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSUIPatternOptions uiPatternOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting iOS UI pattern generation");

            try
            {
                var result = new IOSUIPatternResult();
                var patterns = await GenerateUIPatternsAsync(applicationLogic, new IOSGenerationOptions(), cancellationToken);
                
                result.GeneratedPatterns.AddRange(patterns);
                stopwatch.Stop();
                result.IsSuccess = true;
                result.Message = "iOS UI pattern generation completed successfully";
                result.GenerationTime = stopwatch.Elapsed;
                result.PatternScore = CalculateUIPatternScore(patterns);

                _logger.LogInformation("iOS UI pattern generation completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during iOS UI pattern generation");
                return new IOSUIPatternResult
                {
                    IsSuccess = false,
                    Message = $"Error during iOS UI pattern generation: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    GenerationTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<IOSPerformanceResult> CreateIOSPerformanceOptimizationAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSPerformanceOptions performanceOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting iOS performance optimization");

            try
            {
                var result = new IOSPerformanceResult();
                var optimizations = await GeneratePerformanceOptimizationsAsync(applicationLogic, new IOSGenerationOptions(), cancellationToken);
                
                result.GeneratedOptimizations.AddRange(optimizations);
                stopwatch.Stop();
                result.IsSuccess = true;
                result.Message = "iOS performance optimization completed successfully";
                result.OptimizationTime = stopwatch.Elapsed;
                result.PerformanceScore = CalculatePerformanceScore(optimizations);

                _logger.LogInformation("iOS performance optimization completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during iOS performance optimization");
                return new IOSPerformanceResult
                {
                    IsSuccess = false,
                    Message = $"Error during iOS performance optimization: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    OptimizationTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<IOSAppConfigResult> GenerateIOSAppConfigurationAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSAppConfigOptions configOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting iOS app configuration generation");

            try
            {
                var result = new IOSAppConfigResult();
                var appConfig = await GenerateAppConfigurationAsync(applicationLogic, new IOSGenerationOptions(), cancellationToken);
                
                result.GeneratedConfiguration = appConfig;
                stopwatch.Stop();
                result.IsSuccess = true;
                result.Message = "iOS app configuration generation completed successfully";
                result.GenerationTime = stopwatch.Elapsed;
                result.ConfigurationScore = CalculateAppConfigScore(appConfig);

                _logger.LogInformation("iOS app configuration generation completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during iOS app configuration generation");
                return new IOSAppConfigResult
                {
                    IsSuccess = false,
                    Message = $"Error during iOS app configuration generation: {ex.Message}",
                    Errors = new List<string> { ex.Message },
                    GenerationTime = stopwatch.Elapsed
                };
            }
        }

        public async Task<IOSCodeValidationResult> ValidateIOSCodeAsync(
            IOSGeneratedCode iosCode,
            IOSValidationOptions validationOptions,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Starting iOS code validation");

            try
            {
                var result = new IOSCodeValidationResult();
                var validationErrors = new List<string>();
                var validationWarnings = new List<string>();

                if (validationOptions.ValidateSyntax)
                {
                    var syntaxErrors = ValidateSyntax(iosCode);
                    validationErrors.AddRange(syntaxErrors);
                }

                if (validationOptions.ValidateSemantics)
                {
                    var semanticErrors = ValidateSemantics(iosCode);
                    validationErrors.AddRange(semanticErrors);
                }

                if (validationOptions.ValidatePerformance)
                {
                    var performanceWarnings = ValidatePerformance(iosCode);
                    validationWarnings.AddRange(performanceWarnings);
                }

                if (validationOptions.ValidateSecurity)
                {
                    var securityErrors = ValidateSecurity(iosCode);
                    validationErrors.AddRange(securityErrors);
                }

                stopwatch.Stop();
                result.IsValid = !validationErrors.Any();
                result.Message = result.IsValid ? "iOS code validation passed" : "iOS code validation failed";
                result.ValidationErrors = validationErrors;
                result.ValidationWarnings = validationWarnings;
                result.ValidationTime = stopwatch.Elapsed;
                result.ValidationScore = CalculateValidationScore(validationErrors.Count, validationWarnings.Count);

                _logger.LogInformation("iOS code validation completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error during iOS code validation");
                return new IOSCodeValidationResult
                {
                    IsValid = false,
                    Message = $"Error during iOS code validation: {ex.Message}",
                    ValidationErrors = new List<string> { ex.Message },
                    ValidationTime = stopwatch.Elapsed
                };
            }
        }

        public IEnumerable<IOSUIPattern> GetSupportedIOSUIPatterns()
        {
            return new List<IOSUIPattern>
            {
                new IOSUIPattern { Name = "NavigationView", Type = IOSUIPatternType.Navigation, Description = "Standard iOS navigation pattern" },
                new IOSUIPattern { Name = "TabView", Type = IOSUIPatternType.TabBar, Description = "Tab bar navigation pattern" },
                new IOSUIPattern { Name = "List", Type = IOSUIPatternType.List, Description = "List view pattern" },
                new IOSUIPattern { Name = "Form", Type = IOSUIPatternType.Form, Description = "Form input pattern" },
                new IOSUIPattern { Name = "Detail", Type = IOSUIPatternType.Detail, Description = "Detail view pattern" },
                new IOSUIPattern { Name = "Modal", Type = IOSUIPatternType.Modal, Description = "Modal presentation pattern" }
            };
        }

        public IEnumerable<IOSPerformanceOptimization> GetSupportedIOSPerformanceOptimizations()
        {
            return new List<IOSPerformanceOptimization>
            {
                new IOSPerformanceOptimization { Name = "MemoryOptimization", Type = IOSPerformanceType.Memory, Description = "Memory usage optimization" },
                new IOSPerformanceOptimization { Name = "BatteryOptimization", Type = IOSPerformanceType.Battery, Description = "Battery life optimization" },
                new IOSPerformanceOptimization { Name = "NetworkOptimization", Type = IOSPerformanceType.Network, Description = "Network usage optimization" },
                new IOSPerformanceOptimization { Name = "UIOptimization", Type = IOSPerformanceType.UI, Description = "UI rendering optimization" }
            };
        }

        public IEnumerable<MetalGraphicsFeature> GetSupportedMetalGraphicsFeatures()
        {
            return new List<MetalGraphicsFeature>
            {
                new MetalGraphicsFeature { Name = "VertexProcessing", Type = MetalGraphicsFeatureType.VertexProcessing, Description = "Vertex shader processing" },
                new MetalGraphicsFeature { Name = "FragmentProcessing", Type = MetalGraphicsFeatureType.FragmentProcessing, Description = "Fragment shader processing" },
                new MetalGraphicsFeature { Name = "ComputeProcessing", Type = MetalGraphicsFeatureType.ComputeProcessing, Description = "Compute shader processing" },
                new MetalGraphicsFeature { Name = "TextureSampling", Type = MetalGraphicsFeatureType.TextureSampling, Description = "Texture sampling optimization" }
            };
        }

        // Private helper methods
        private async Task<List<SwiftUIFile>> GenerateSwiftUIFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken)
        {
            var files = new List<SwiftUIFile>();

            // Generate ContentView
            files.Add(new SwiftUIFile
            {
                FileName = "ContentView.swift",
                FilePath = "Views/ContentView.swift",
                Content = GenerateContentViewCode(applicationLogic),
                ViewType = SwiftUIViewType.ContentView,
                Dependencies = new List<string> { "SwiftUI" }
            });

            // Generate ListView if patterns exist
            if (applicationLogic.Patterns?.Any() == true)
            {
                files.Add(new SwiftUIFile
                {
                    FileName = "ListView.swift",
                    FilePath = "Views/ListView.swift",
                    Content = GenerateListViewCode(applicationLogic),
                    ViewType = SwiftUIViewType.ListView,
                    Dependencies = new List<string> { "SwiftUI" }
                });
            }

            return files;
        }

        private async Task<List<CoreDataFile>> GenerateCoreDataFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken)
        {
            var files = new List<CoreDataFile>();

            // Generate Core Data model
            files.Add(new CoreDataFile
            {
                FileName = "DataModel.xcdatamodeld",
                FilePath = "Data/DataModel.xcdatamodeld",
                Content = GenerateCoreDataModelCode(applicationLogic),
                FileType = CoreDataFileType.Model,
                Entities = GenerateCoreDataEntities(applicationLogic)
            });

            return files;
        }

        private async Task<List<MetalFile>> GenerateMetalFilesAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken)
        {
            var files = new List<MetalFile>();

            // Generate basic Metal shader if graphics features are enabled
            if (iosOptions.EnableMetalGraphics)
            {
                files.Add(new MetalFile
                {
                    FileName = "BasicShader.metal",
                    FilePath = "Shaders/BasicShader.metal",
                    Content = GenerateBasicMetalShaderCode(),
                    FileType = MetalFileType.VertexShader,
                    Features = new List<MetalGraphicsFeature> { GetSupportedMetalGraphicsFeatures().First() }
                });
            }

            return files;
        }

        private async Task<List<IOSPerformanceOptimization>> GeneratePerformanceOptimizationsAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken)
        {
            var optimizations = new List<IOSPerformanceOptimization>();

            // Add memory optimization
            optimizations.Add(new IOSPerformanceOptimization
            {
                Name = "MemoryOptimization",
                Type = IOSPerformanceType.Memory,
                Description = "Automatic memory management optimization",
                Implementation = "Lazy loading and weak references",
                PerformanceImpact = 0.8
            });

            // Add battery optimization
            optimizations.Add(new IOSPerformanceOptimization
            {
                Name = "BatteryOptimization",
                Type = IOSPerformanceType.Battery,
                Description = "Battery life optimization",
                Implementation = "Background task management",
                PerformanceImpact = 0.7
            });

            return optimizations;
        }

        private async Task<List<IOSUIPattern>> GenerateUIPatternsAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken)
        {
            var patterns = new List<IOSUIPattern>();

            // Add navigation pattern
            patterns.Add(new IOSUIPattern
            {
                Name = "NavigationView",
                Type = IOSUIPatternType.Navigation,
                Description = "Standard iOS navigation",
                Implementation = "NavigationView with NavigationLink"
            });

            // Add list pattern if patterns exist
            if (applicationLogic.Patterns?.Any() == true)
            {
                patterns.Add(new IOSUIPattern
                {
                    Name = "List",
                    Type = IOSUIPatternType.List,
                    Description = "List view for data display",
                    Implementation = "List with ForEach"
                });
            }

            return patterns;
        }

        private async Task<IOSAppConfiguration> GenerateAppConfigurationAsync(
            StandardizedApplicationLogic applicationLogic,
            IOSGenerationOptions iosOptions,
            CancellationToken cancellationToken)
        {
            return new IOSAppConfiguration
            {
                AppName = "GeneratedIOSApp",
                BundleIdentifier = "com.example.generatediosapp",
                Version = "1.0.0",
                BuildNumber = "1",
                SupportedDevices = iosOptions.SupportedDevices,
                RequiredPermissions = new List<string>(),
                InfoPlist = new Dictionary<string, object>
                {
                    ["CFBundleDisplayName"] = "Generated iOS App",
                    ["CFBundleIdentifier"] = "com.example.generatediosapp",
                    ["CFBundleVersion"] = "1.0.0",
                    ["CFBundleShortVersionString"] = "1.0.0"
                }
            };
        }

        private List<string> ValidateSyntax(IOSGeneratedCode iosCode)
        {
            var errors = new List<string>();

            // Basic syntax validation
            if (iosCode.SwiftUIFiles?.Any() == true)
            {
                foreach (var file in iosCode.SwiftUIFiles)
                {
                    if (string.IsNullOrEmpty(file.Content))
                    {
                        errors.Add($"Empty content in SwiftUI file: {file.FileName}");
                    }
                }
            }

            return errors;
        }

        private List<string> ValidateSemantics(IOSGeneratedCode iosCode)
        {
            var errors = new List<string>();

            // Basic semantic validation
            if (iosCode.AppConfiguration == null)
            {
                errors.Add("Missing app configuration");
            }

            return errors;
        }

        private List<string> ValidatePerformance(IOSGeneratedCode iosCode)
        {
            var warnings = new List<string>();

            // Performance warnings
            if (iosCode.MetalFiles?.Any() == true && iosCode.AppliedOptimizations?.Any(o => o.Type == IOSPerformanceType.Graphics) != true)
            {
                warnings.Add("Metal graphics enabled but no graphics performance optimization applied");
            }

            return warnings;
        }

        private List<string> ValidateSecurity(IOSGeneratedCode iosCode)
        {
            var errors = new List<string>();

            // Security validation
            if (iosCode.AppConfiguration?.RequiredPermissions?.Any() == true)
            {
                // Check for sensitive permissions
                var sensitivePermissions = new[] { "camera", "microphone", "location" };
                foreach (var permission in iosCode.AppConfiguration.RequiredPermissions)
                {
                    if (sensitivePermissions.Any(sp => permission.ToLower().Contains(sp)))
                    {
                        errors.Add($"Sensitive permission requested: {permission}");
                    }
                }
            }

            return errors;
        }

        // Code generation helper methods
        private string GenerateContentViewCode(StandardizedApplicationLogic applicationLogic)
        {
            var sb = new StringBuilder();
            sb.AppendLine("import SwiftUI");
            sb.AppendLine();
            sb.AppendLine("struct ContentView: View {");
            sb.AppendLine("    var body: some View {");
            sb.AppendLine("        NavigationView {");
            sb.AppendLine("            VStack {");
            sb.AppendLine("                Text(\"Generated iOS App\")");
            sb.AppendLine("                    .font(.largeTitle)");
            sb.AppendLine("                    .padding()");
            sb.AppendLine("                Spacer()");
            sb.AppendLine("            }");
            sb.AppendLine("            .navigationTitle(\"Home\")");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string GenerateListViewCode(StandardizedApplicationLogic applicationLogic)
        {
            var sb = new StringBuilder();
            sb.AppendLine("import SwiftUI");
            sb.AppendLine();
            sb.AppendLine("struct ListView: View {");
            sb.AppendLine("    var body: some View {");
            sb.AppendLine("        List {");
            sb.AppendLine("            ForEach(0..<10, id: \\.self) { index in");
            sb.AppendLine("                Text(\"Item \\(index)\")");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("        .navigationTitle(\"List\")");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private string GenerateCoreDataModelCode(StandardizedApplicationLogic applicationLogic)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.AppendLine("<model type=\"com.apple.IDECoreDataModeler.DataModel\" documentVersion=\"1.0\" lastSavedToolsVersion=\"21754\" systemVersion=\"22G120\" minimumToolsVersion=\"Automatic\" sourceLanguage=\"Swift\" userDefinedModelVersionIdentifier=\"\">");
            sb.AppendLine("    <entity name=\"GeneratedEntity\" representedClassName=\"GeneratedEntity\" syncable=\"YES\">");
            sb.AppendLine("        <attribute name=\"id\" optional=\"NO\" attributeType=\"UUID\" usesScalarValueType=\"NO\"/>");
            sb.AppendLine("        <attribute name=\"name\" optional=\"YES\" attributeType=\"String\"/>");
            sb.AppendLine("        <attribute name=\"createdDate\" optional=\"YES\" attributeType=\"Date\" usesScalarValueType=\"NO\"/>");
            sb.AppendLine("    </entity>");
            sb.AppendLine("</model>");
            return sb.ToString();
        }

        private List<CoreDataEntity> GenerateCoreDataEntities(StandardizedApplicationLogic applicationLogic)
        {
            return new List<CoreDataEntity>
            {
                new CoreDataEntity
                {
                    Name = "GeneratedEntity",
                    Attributes = new List<CoreDataAttribute>
                    {
                        new CoreDataAttribute { Name = "id", Type = CoreDataAttributeType.UUID, IsOptional = false },
                        new CoreDataAttribute { Name = "name", Type = CoreDataAttributeType.String, IsOptional = true },
                        new CoreDataAttribute { Name = "createdDate", Type = CoreDataAttributeType.Date, IsOptional = true }
                    }
                }
            };
        }

        private string GenerateBasicMetalShaderCode()
        {
            var sb = new StringBuilder();
            sb.AppendLine("#include <metal_stdlib>");
            sb.AppendLine("using namespace metal;");
            sb.AppendLine();
            sb.AppendLine("struct VertexIn {");
            sb.AppendLine("    float3 position [[attribute(0)]];");
            sb.AppendLine("};");
            sb.AppendLine();
            sb.AppendLine("struct VertexOut {");
            sb.AppendLine("    float4 position [[position]];");
            sb.AppendLine("};");
            sb.AppendLine();
            sb.AppendLine("vertex VertexOut vertex_main(VertexIn in [[stage_in]]) {");
            sb.AppendLine("    VertexOut out;");
            sb.AppendLine("    out.position = float4(in.position, 1.0);");
            sb.AppendLine("    return out;");
            sb.AppendLine("}");
            return sb.ToString();
        }

        // Scoring methods
        private double CalculateGenerationScore(IOSGeneratedCode generatedCode)
        {
            double score = 0.0;
            int totalComponents = 0;

            if (generatedCode.SwiftUIFiles?.Any() == true)
            {
                score += generatedCode.SwiftUIFiles.Count * 10;
                totalComponents += generatedCode.SwiftUIFiles.Count;
            }

            if (generatedCode.CoreDataFiles?.Any() == true)
            {
                score += generatedCode.CoreDataFiles.Count * 15;
                totalComponents += generatedCode.CoreDataFiles.Count;
            }

            if (generatedCode.MetalFiles?.Any() == true)
            {
                score += generatedCode.MetalFiles.Count * 20;
                totalComponents += generatedCode.MetalFiles.Count;
            }

            if (generatedCode.AppliedUIPatterns?.Any() == true)
            {
                score += generatedCode.AppliedUIPatterns.Count * 5;
                totalComponents += generatedCode.AppliedUIPatterns.Count;
            }

            if (generatedCode.AppliedOptimizations?.Any() == true)
            {
                score += generatedCode.AppliedOptimizations.Count * 8;
                totalComponents += generatedCode.AppliedOptimizations.Count;
            }

            return totalComponents > 0 ? score / totalComponents : 0.0;
        }

        private double CalculateCoreDataScore(List<CoreDataFile> coreDataFiles)
        {
            return coreDataFiles?.Count * 15.0 ?? 0.0;
        }

        private double CalculateMetalScore(List<MetalFile> metalFiles)
        {
            return metalFiles?.Count * 20.0 ?? 0.0;
        }

        private double CalculateUIPatternScore(List<IOSUIPattern> patterns)
        {
            return patterns?.Count * 5.0 ?? 0.0;
        }

        private double CalculatePerformanceScore(List<IOSPerformanceOptimization> optimizations)
        {
            return optimizations?.Sum(o => o.PerformanceImpact) ?? 0.0;
        }

        private double CalculateAppConfigScore(IOSAppConfiguration appConfig)
        {
            double score = 0.0;
            if (!string.IsNullOrEmpty(appConfig?.AppName)) score += 10;
            if (!string.IsNullOrEmpty(appConfig?.BundleIdentifier)) score += 10;
            if (!string.IsNullOrEmpty(appConfig?.Version)) score += 5;
            if (appConfig?.SupportedDevices?.Any() == true) score += 5;
            return score;
        }

        private double CalculateValidationScore(int errorCount, int warningCount)
        {
            double baseScore = 100.0;
            baseScore -= errorCount * 20.0; // Each error reduces score by 20
            baseScore -= warningCount * 5.0; // Each warning reduces score by 5
            return Math.Max(0.0, baseScore);
        }
    }
} 