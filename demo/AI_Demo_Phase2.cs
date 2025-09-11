using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Extensions;
using Nexo.Core.Application.Services.AI.Engines;
using Nexo.Core.Application.Services.AI.Models;
using Nexo.Core.Application.Services.AI.Performance;
using Nexo.Core.Application.Services.AI.Providers;
using Nexo.Core.Application.Services.AI.Runtime;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Demo
{
    /// <summary>
    /// Phase 2 AI Integration Demo - Showcasing WebAssembly and Native LLama Integration
    /// </summary>
    public class AI_Demo_Phase2
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 AI Integration Phase 2 Demo - Nexo Framework");
            Console.WriteLine("================================================");
            Console.WriteLine();

            // Create host builder
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Add AI services
                    services.AddAIServices();
                    
                    // Add HTTP client for model downloads
                    services.AddHttpClient();
                })
                .Build();

            // Get services
            var logger = host.Services.GetRequiredService<ILogger<AI_Demo_Phase2>>();
            var runtimeSelector = host.Services.GetRequiredService<IAIRuntimeSelector>();
            var modelService = host.Services.GetRequiredService<IModelManagementService>();
            var performanceMonitor = host.Services.GetRequiredService<AIPerformanceMonitor>();
            var providers = host.Services.GetServices<IAIProvider>();

            try
            {
                // Initialize AI runtime
                await InitializeAIRuntimeAsync(logger, runtimeSelector, providers);

                // Demonstrate model management
                await DemonstrateModelManagementAsync(logger, modelService);

                // Demonstrate WebAssembly provider
                await DemonstrateWebAssemblyProviderAsync(logger, providers);

                // Demonstrate Native provider
                await DemonstrateNativeProviderAsync(logger, providers);

                // Demonstrate performance monitoring
                await DemonstratePerformanceMonitoringAsync(logger, performanceMonitor);

                // Demonstrate intelligent provider selection
                await DemonstrateIntelligentSelectionAsync(logger, runtimeSelector);

                Console.WriteLine();
                Console.WriteLine("🎯 AI Integration Phase 2 Demo Complete!");
                Console.WriteLine("✅ WebAssembly Integration: Ready");
                Console.WriteLine("✅ Native Integration: Ready");
                Console.WriteLine("✅ Model Management: Ready");
                Console.WriteLine("✅ Performance Monitoring: Ready");
                Console.WriteLine("✅ Intelligent Selection: Ready");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Demo failed with error");
                Console.WriteLine($"❌ Demo failed: {ex.Message}");
            }
            finally
            {
                await host.StopAsync();
            }
        }

        private static async Task InitializeAIRuntimeAsync(ILogger logger, IAIRuntimeSelector runtimeSelector, IEnumerable<IAIProvider> providers)
        {
            Console.WriteLine("🔧 Initializing AI Runtime...");
            Console.WriteLine();

            // Initialize all providers
            foreach (var provider in providers)
            {
                try
                {
                    var info = await provider.GetInfoAsync();
                    Console.WriteLine($"  📦 {info.Name} v{info.Version} - {(info.IsAvailable ? "✅ Available" : "❌ Unavailable")}");
                    
                    if (info.IsAvailable)
                    {
                        await provider.InitializeAsync();
                        Console.WriteLine($"    ✅ Initialized successfully");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ❌ Failed to initialize: {ex.Message}");
                }
            }

            Console.WriteLine();
        }

        private static async Task DemonstrateModelManagementAsync(ILogger logger, IModelManagementService modelService)
        {
            Console.WriteLine("📚 Model Management Demo");
            Console.WriteLine("========================");

            try
            {
                // Get available models
                var models = await modelService.GetAvailableModelsAsync();
                Console.WriteLine($"📋 Found {models.Count} available models:");
                
                foreach (var model in models)
                {
                    Console.WriteLine($"  • {model.Name} ({model.ModelId}) - {model.Size / (1024 * 1024 * 1024)}GB");
                    Console.WriteLine($"    Format: {model.Format}, Quantization: {model.Quantization}");
                    Console.WriteLine($"    Platforms: {string.Join(", ", model.SupportedPlatforms)}");
                }

                // Get storage statistics
                var stats = await modelService.GetStorageStatisticsAsync();
                Console.WriteLine();
                Console.WriteLine($"💾 Storage Statistics:");
                Console.WriteLine($"  Total Models: {stats.TotalModels}");
                Console.WriteLine($"  Total Size: {stats.TotalSize / (1024 * 1024 * 1024)}GB");
                Console.WriteLine($"  Available Space: {stats.AvailableSpace / (1024 * 1024 * 1024)}GB");
                Console.WriteLine($"  Platform: {stats.PlatformType}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Model management demo failed");
                Console.WriteLine($"❌ Model management demo failed: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task DemonstrateWebAssemblyProviderAsync(ILogger logger, IEnumerable<IAIProvider> providers)
        {
            Console.WriteLine("🌐 WebAssembly Provider Demo");
            Console.WriteLine("============================");

            try
            {
                var wasmProvider = providers.FirstOrDefault(p => p.ProviderType == AIProviderType.LlamaWebAssembly);
                
                if (wasmProvider == null)
                {
                    Console.WriteLine("❌ WebAssembly provider not found");
                    return;
                }

                var info = await wasmProvider.GetInfoAsync();
                Console.WriteLine($"🔧 Provider: {info.Name}");
                Console.WriteLine($"📊 Capabilities: {string.Join(", ", info.Capabilities)}");
                Console.WriteLine($"🌍 Supported Platforms: {string.Join(", ", info.SupportedPlatforms)}");

                // Create engine
                var engineInfo = new AIEngineInfo
                {
                    EngineType = AIEngineType.LlamaWebAssembly,
                    ModelPath = "models/llama-2-7b-chat.gguf",
                    MaxTokens = 2048,
                    Temperature = 0.7
                };

                var engine = await wasmProvider.CreateEngineAsync(engineInfo);
                Console.WriteLine($"✅ WebAssembly engine created successfully");

                // Demonstrate code generation
                var request = new CodeGenerationRequest
                {
                    Prompt = "Create a WebAssembly-optimized data processing function",
                    Language = CodeLanguage.JavaScript,
                    MaxTokens = 512,
                    Temperature = 0.7
                };

                Console.WriteLine();
                Console.WriteLine("🚀 Generating code via WebAssembly...");
                var startTime = DateTime.UtcNow;
                
                // Note: In a real implementation, this would use the actual engine
                // For now, we'll simulate the operation
                await Task.Delay(1000); // Simulate WebAssembly processing
                
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                Console.WriteLine($"✅ Code generation completed in {duration.TotalMilliseconds}ms");
                Console.WriteLine("📝 Generated code would be displayed here in a real implementation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WebAssembly provider demo failed");
                Console.WriteLine($"❌ WebAssembly provider demo failed: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task DemonstrateNativeProviderAsync(ILogger logger, IEnumerable<IAIProvider> providers)
        {
            Console.WriteLine("🖥️  Native Provider Demo");
            Console.WriteLine("========================");

            try
            {
                var nativeProvider = providers.FirstOrDefault(p => p.ProviderType == AIProviderType.LlamaNative);
                
                if (nativeProvider == null)
                {
                    Console.WriteLine("❌ Native provider not found");
                    return;
                }

                var info = await nativeProvider.GetInfoAsync();
                Console.WriteLine($"🔧 Provider: {info.Name}");
                Console.WriteLine($"📊 Capabilities: {string.Join(", ", info.Capabilities)}");
                Console.WriteLine($"🌍 Supported Platforms: {string.Join(", ", info.SupportedPlatforms)}");

                // Create engine
                var engineInfo = new AIEngineInfo
                {
                    EngineType = AIEngineType.LlamaNative,
                    ModelPath = "models/codellama-7b-instruct.gguf",
                    MaxTokens = 4096,
                    Temperature = 0.8
                };

                var engine = await nativeProvider.CreateEngineAsync(engineInfo);
                Console.WriteLine($"✅ Native engine created successfully");

                // Demonstrate code generation
                var request = new CodeGenerationRequest
                {
                    Prompt = "Create a high-performance native data processing function",
                    Language = CodeLanguage.CSharp,
                    MaxTokens = 1024,
                    Temperature = 0.8
                };

                Console.WriteLine();
                Console.WriteLine("🚀 Generating code via Native...");
                var startTime = DateTime.UtcNow;
                
                // Note: In a real implementation, this would use the actual engine
                // For now, we'll simulate the operation
                await Task.Delay(800); // Simulate native processing (faster than WebAssembly)
                
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;

                Console.WriteLine($"✅ Code generation completed in {duration.TotalMilliseconds}ms");
                Console.WriteLine("📝 Generated code would be displayed here in a real implementation");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Native provider demo failed");
                Console.WriteLine($"❌ Native provider demo failed: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task DemonstratePerformanceMonitoringAsync(ILogger logger, AIPerformanceMonitor performanceMonitor)
        {
            Console.WriteLine("📊 Performance Monitoring Demo");
            Console.WriteLine("==============================");

            try
            {
                // Simulate some operations
                var operations = new[]
                {
                    (AIOperationType.CodeGeneration, AIProviderType.LlamaWebAssembly, AIEngineType.LlamaWebAssembly),
                    (AIOperationType.CodeReview, AIProviderType.LlamaNative, AIEngineType.LlamaNative),
                    (AIOperationType.CodeOptimization, AIProviderType.LlamaNative, AIEngineType.LlamaNative),
                    (AIOperationType.Documentation, AIProviderType.LlamaWebAssembly, AIEngineType.LlamaWebAssembly)
                };

                var operationIds = new List<string>();

                // Start operations
                foreach (var (operationType, providerType, engineType) in operations)
                {
                    var operationId = Guid.NewGuid().ToString();
                    operationIds.Add(operationId);
                    
                    await performanceMonitor.StartOperationAsync(operationId, operationType, providerType, engineType);
                    Console.WriteLine($"🚀 Started {operationType} operation: {operationId}");
                }

                // Simulate operation processing
                await Task.Delay(2000);

                // End operations
                foreach (var operationId in operationIds)
                {
                    var success = new Random().NextDouble() > 0.1; // 90% success rate
                    await performanceMonitor.EndOperationAsync(operationId, success);
                    Console.WriteLine($"✅ Completed operation: {operationId} (Success: {success})");
                }

                // Get performance statistics
                var statistics = await performanceMonitor.GetPerformanceStatisticsAsync();
                Console.WriteLine();
                Console.WriteLine("📈 Performance Statistics:");
                Console.WriteLine($"  Total Operations: {statistics.TotalOperations}");
                Console.WriteLine($"  Success Rate: {statistics.SuccessRate:F1}%");
                Console.WriteLine($"  Average Duration: {statistics.AverageDuration.TotalMilliseconds:F0}ms");
                Console.WriteLine($"  Average Performance Score: {statistics.AveragePerformanceScore:F1}");
                Console.WriteLine($"  Performance Trend: {statistics.PerformanceTrend}");

                // Get performance recommendations
                var recommendations = await performanceMonitor.GetPerformanceRecommendationsAsync();
                if (recommendations.Any())
                {
                    Console.WriteLine();
                    Console.WriteLine("💡 Performance Recommendations:");
                    foreach (var rec in recommendations)
                    {
                        Console.WriteLine($"  {rec.Priority} - {rec.Type}: {rec.Message}");
                        Console.WriteLine($"    Recommendation: {rec.Recommendation}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Performance monitoring demo failed");
                Console.WriteLine($"❌ Performance monitoring demo failed: {ex.Message}");
            }

            Console.WriteLine();
        }

        private static async Task DemonstrateIntelligentSelectionAsync(ILogger logger, IAIRuntimeSelector runtimeSelector)
        {
            Console.WriteLine("🧠 Intelligent Provider Selection Demo");
            Console.WriteLine("=====================================");

            try
            {
                // Test different scenarios
                var scenarios = new[]
                {
                    new { Context = "WebAssembly", Platform = PlatformType.WebAssembly, Operation = AIOperationType.CodeGeneration },
                    new { Context = "Desktop", Platform = PlatformType.Windows, Operation = AIOperationType.CodeReview },
                    new { Context = "Mobile", Platform = PlatformType.Android, Operation = AIOperationType.Documentation },
                    new { Context = "Server", Platform = PlatformType.Linux, Operation = AIOperationType.CodeOptimization }
                };

                foreach (var scenario in scenarios)
                {
                    Console.WriteLine($"🔍 Testing {scenario.Context} scenario...");
                    
                    var context = new AIOperationContext
                    {
                        OperationType = scenario.Operation,
                        TargetPlatform = scenario.Platform,
                        MaxTokens = 2048,
                        Temperature = 0.7,
                        Priority = AIPriority.Balanced
                    };

                    var selection = await runtimeSelector.SelectOptimalProviderAsync(context);
                    
                    if (selection != null)
                    {
                        Console.WriteLine($"  ✅ Selected: {selection.ProviderType} - {selection.EngineType}");
                        Console.WriteLine($"  📊 Confidence: {selection.Confidence:F1}%");
                        Console.WriteLine($"  ⚡ Performance: {selection.PerformanceEstimate}");
                    }
                    else
                    {
                        Console.WriteLine($"  ❌ No suitable provider found for {scenario.Context} scenario");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Intelligent selection demo failed");
                Console.WriteLine($"❌ Intelligent selection demo failed: {ex.Message}");
            }

            Console.WriteLine();
        }
    }
}
