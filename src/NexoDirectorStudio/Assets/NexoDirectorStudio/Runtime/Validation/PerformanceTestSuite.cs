using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Validation
{
    /// <summary>
    /// Test suite for performance validation of generated components.
    /// </summary>
    public class PerformanceTestSuite : IComponentTest
    {
        public string Name => "PerformanceTestSuite";

        public async Task<TestSuiteResult> RunTestsAsync(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var result = new TestSuiteResult
            {
                TestSuiteName = Name,
                ValidationStartTime = DateTimeOffset.UtcNow
            };

            try
            {
                var tests = new List<Func<Task<TestResult>>>
                {
                    () => TestComponentCount(contentBundle, gamePlan, cancellationToken),
                    () => TestMemoryUsage(contentBundle, gamePlan, cancellationToken),
                    () => TestRenderingPerformance(contentBundle, gamePlan, cancellationToken),
                    () => TestTriangleCount(contentBundle, gamePlan, cancellationToken),
                    () => TestDrawCalls(contentBundle, gamePlan, cancellationToken)
                };

                foreach (var test in tests)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var testResult = await test();
                    result.TestResults.Add(testResult);
                }

                result.Passed = result.TestResults.All(t => t.Passed);
                result.ValidationEndTime = DateTimeOffset.UtcNow;
                result.Duration = result.ValidationEndTime - result.ValidationStartTime;
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.ErrorMessage = ex.Message;
                result.ValidationEndTime = DateTimeOffset.UtcNow;
                result.Duration = result.ValidationEndTime - result.ValidationStartTime;
            }

            return result;
        }

        private async Task<TestResult> TestComponentCount(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var componentCount = contentBundle.Assets.Count;
                var maxComponents = GetMaxComponentCount(gamePlan.Genre);
                var score = maxComponents > 0 ? Math.Max(0, 100f - (componentCount - maxComponents) * 2f) : 100f;
                var passed = componentCount <= maxComponents;

                return new TestResult
                {
                    TestName = "ComponentCount",
                    Passed = passed,
                    Message = passed ?
                        $"Component count within limits ({componentCount}/{maxComponents})" :
                        $"Component count exceeds limits ({componentCount}/{maxComponents})",
                    Score = Math.Max(0, score),
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = passed ? new List<string>() : new List<string> { $"Too many components: {componentCount} > {maxComponents}" },
                    Suggestions = passed ? new List<string>() : new List<string> { "Reduce component count or optimize components" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "ComponentCount",
                    Passed = false,
                    Message = $"Component count test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review component count validation" }
                };
            }
        }

        private async Task<TestResult> TestMemoryUsage(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var estimatedMemory = EstimateMemoryUsage(contentBundle);
                var maxMemory = GetMaxMemoryUsage(gamePlan.Genre);
                var score = maxMemory > 0 ? Math.Max(0, 100f - (estimatedMemory - maxMemory) / maxMemory * 100f) : 100f;
                var passed = estimatedMemory <= maxMemory;

                return new TestResult
                {
                    TestName = "MemoryUsage",
                    Passed = passed,
                    Message = passed ?
                        $"Memory usage within limits ({estimatedMemory}MB/{maxMemory}MB)" :
                        $"Memory usage exceeds limits ({estimatedMemory}MB/{maxMemory}MB)",
                    Score = Math.Max(0, score),
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = passed ? new List<string>() : new List<string> { $"Memory usage too high: {estimatedMemory}MB > {maxMemory}MB" },
                    Suggestions = passed ? new List<string>() : new List<string> { "Optimize memory usage or reduce asset quality" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "MemoryUsage",
                    Passed = false,
                    Message = $"Memory usage test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review memory usage calculation" }
                };
            }
        }

        private async Task<TestResult> TestRenderingPerformance(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var renderingScore = await CalculateRenderingScore(contentBundle, cancellationToken);
                var minScore = GetMinRenderingScore(gamePlan.Genre);
                var passed = renderingScore >= minScore;

                return new TestResult
                {
                    TestName = "RenderingPerformance",
                    Passed = passed,
                    Message = passed ?
                        $"Rendering performance acceptable ({renderingScore:F1}% >= {minScore}%)" :
                        $"Rendering performance poor ({renderingScore:F1}% < {minScore}%)",
                    Score = renderingScore,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = passed ? new List<string>() : new List<string> { $"Rendering performance below threshold: {renderingScore:F1}% < {minScore}%" },
                    Suggestions = passed ? new List<string>() : new List<string> { "Optimize rendering performance" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "RenderingPerformance",
                    Passed = false,
                    Message = $"Rendering performance test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review rendering performance calculation" }
                };
            }
        }

        private async Task<TestResult> TestTriangleCount(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var triangleCount = await CalculateTriangleCount(contentBundle, cancellationToken);
                var maxTriangles = GetMaxTriangleCount(gamePlan.Genre);
                var score = maxTriangles > 0 ? Math.Max(0, 100f - (triangleCount - maxTriangles) / maxTriangles * 100f) : 100f;
                var passed = triangleCount <= maxTriangles;

                return new TestResult
                {
                    TestName = "TriangleCount",
                    Passed = passed,
                    Message = passed ?
                        $"Triangle count within limits ({triangleCount}/{maxTriangles})" :
                        $"Triangle count exceeds limits ({triangleCount}/{maxTriangles})",
                    Score = Math.Max(0, score),
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = passed ? new List<string>() : new List<string> { $"Too many triangles: {triangleCount} > {maxTriangles}" },
                    Suggestions = passed ? new List<string>() : new List<string> { "Reduce triangle count or use LODs" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "TriangleCount",
                    Passed = false,
                    Message = $"Triangle count test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review triangle count calculation" }
                };
            }
        }

        private async Task<TestResult> TestDrawCalls(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var drawCalls = await CalculateDrawCalls(contentBundle, cancellationToken);
                var maxDrawCalls = GetMaxDrawCalls(gamePlan.Genre);
                var score = maxDrawCalls > 0 ? Math.Max(0, 100f - (drawCalls - maxDrawCalls) / maxDrawCalls * 100f) : 100f;
                var passed = drawCalls <= maxDrawCalls;

                return new TestResult
                {
                    TestName = "DrawCalls",
                    Passed = passed,
                    Message = passed ?
                        $"Draw calls within limits ({drawCalls}/{maxDrawCalls})" :
                        $"Draw calls exceed limits ({drawCalls}/{maxDrawCalls})",
                    Score = Math.Max(0, score),
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = passed ? new List<string>() : new List<string> { $"Too many draw calls: {drawCalls} > {maxDrawCalls}" },
                    Suggestions = passed ? new List<string>() : new List<string> { "Reduce draw calls or use batching" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "DrawCalls",
                    Passed = false,
                    Message = $"Draw calls test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review draw call calculation" }
                };
            }
        }

        private int GetMaxComponentCount(string genre)
        {
            return genre.ToLower() switch
            {
                "fps" => 50,
                "platformer" => 30,
                "rpg" => 100,
                _ => 25
            };
        }

        private int GetMaxMemoryUsage(string genre)
        {
            return genre.ToLower() switch
            {
                "fps" => 512, // 512MB
                "platformer" => 256, // 256MB
                "rpg" => 1024, // 1GB
                _ => 128 // 128MB
            };
        }

        private float GetMinRenderingScore(string genre)
        {
            return genre.ToLower() switch
            {
                "fps" => 60f,
                "platformer" => 30f,
                "rpg" => 45f,
                _ => 30f
            };
        }

        private int GetMaxTriangleCount(string genre)
        {
            return genre.ToLower() switch
            {
                "fps" => 100000,
                "platformer" => 50000,
                "rpg" => 200000,
                _ => 25000
            };
        }

        private int GetMaxDrawCalls(string genre)
        {
            return genre.ToLower() switch
            {
                "fps" => 200,
                "platformer" => 100,
                "rpg" => 300,
                _ => 50
            };
        }

        private int EstimateMemoryUsage(ContentBundle contentBundle)
        {
            // Simple estimation based on asset types
            var memoryUsage = 0;
            foreach (var asset in contentBundle.Assets)
            {
                memoryUsage += asset.AssetType.ToLower() switch
                {
                    "texture" => 16, // 16MB per texture
                    "audio" => 8, // 8MB per audio clip
                    "mesh" => 4, // 4MB per mesh
                    "prefab" => 2, // 2MB per prefab
                    _ => 1 // 1MB per other asset
                };
            }
            return memoryUsage;
        }

        private async Task<float> CalculateRenderingScore(ContentBundle contentBundle, CancellationToken cancellationToken)
        {
            // Simulate rendering performance calculation
            await Task.Delay(50, cancellationToken);
            
            var baseScore = 100f;
            var textureCount = contentBundle.Assets.Count(a => a.AssetType.ToLower() == "texture");
            var meshCount = contentBundle.Assets.Count(a => a.AssetType.ToLower() == "mesh");
            
            // Reduce score based on complexity
            baseScore -= textureCount * 2f; // 2% per texture
            baseScore -= meshCount * 1f; // 1% per mesh
            
            return Math.Max(0, baseScore);
        }

        private async Task<int> CalculateTriangleCount(ContentBundle contentBundle, CancellationToken cancellationToken)
        {
            // Simulate triangle count calculation
            await Task.Delay(30, cancellationToken);
            
            var triangleCount = 0;
            foreach (var asset in contentBundle.Assets)
            {
                if (asset.AssetType.ToLower() == "mesh")
                {
                    triangleCount += UnityEngine.Random.Range(1000, 10000); // Simulate triangle count
                }
            }
            
            return triangleCount;
        }

        private async Task<int> CalculateDrawCalls(ContentBundle contentBundle, CancellationToken cancellationToken)
        {
            // Simulate draw call calculation
            await Task.Delay(20, cancellationToken);
            
            var drawCalls = 0;
            foreach (var asset in contentBundle.Assets)
            {
                if (asset.AssetType.ToLower() == "prefab")
                {
                    drawCalls += UnityEngine.Random.Range(1, 5); // Simulate draw calls per prefab
                }
            }
            
            return drawCalls;
        }
    }
}
