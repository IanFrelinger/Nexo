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
    /// Test suite for integration validation of generated components.
    /// </summary>
    public class IntegrationTestSuite : IComponentTest
    {
        public string Name => "IntegrationTestSuite";

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
                    () => TestComponentInteractions(contentBundle, gamePlan, cancellationToken),
                    () => TestSceneHierarchy(contentBundle, gamePlan, cancellationToken),
                    () => TestPhysicsSetup(contentBundle, gamePlan, cancellationToken),
                    () => TestCircularReferences(contentBundle, gamePlan, cancellationToken),
                    () => TestDependencyChain(contentBundle, gamePlan, cancellationToken)
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

        private async Task<TestResult> TestComponentInteractions(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var interactionPairs = await FindInteractionPairs(contentBundle, cancellationToken);
                var validInteractions = 0;
                var totalInteractions = interactionPairs.Count;

                foreach (var pair in interactionPairs)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (await CanComponentsInteract(pair.Item1, pair.Item2, cancellationToken))
                    {
                        validInteractions++;
                    }
                }

                var score = totalInteractions > 0 ? (float)validInteractions / totalInteractions * 100f : 100f;
                var passed = score >= 80f;

                return new TestResult
                {
                    TestName = "ComponentInteractions",
                    Passed = passed,
                    Message = passed ?
                        $"Component interactions working ({validInteractions}/{totalInteractions})" :
                        $"Component interaction issues ({validInteractions}/{totalInteractions})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = passed ? new List<string>() : new List<string> { "Some component interactions are not working" },
                    Suggestions = passed ? new List<string>() : new List<string> { "Fix component interaction implementation" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "ComponentInteractions",
                    Passed = false,
                    Message = $"Component interactions test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review component interaction implementation" }
                };
            }
        }

        private async Task<TestResult> TestSceneHierarchy(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var hierarchyIssues = await FindHierarchyIssues(contentBundle, cancellationToken);
                var score = hierarchyIssues.Count == 0 ? 100f : Math.Max(0, 100f - hierarchyIssues.Count * 10f);
                var passed = hierarchyIssues.Count == 0;

                return new TestResult
                {
                    TestName = "SceneHierarchy",
                    Passed = passed,
                    Message = passed ?
                        "Scene hierarchy is valid" :
                        $"Scene hierarchy issues found ({hierarchyIssues.Count})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = hierarchyIssues,
                    Suggestions = passed ? new List<string>() : new List<string> { "Fix scene hierarchy structure" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "SceneHierarchy",
                    Passed = false,
                    Message = $"Scene hierarchy test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review scene hierarchy implementation" }
                };
            }
        }

        private async Task<TestResult> TestPhysicsSetup(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var physicsIssues = await FindPhysicsIssues(contentBundle, cancellationToken);
                var score = physicsIssues.Count == 0 ? 100f : Math.Max(0, 100f - physicsIssues.Count * 15f);
                var passed = physicsIssues.Count == 0;

                return new TestResult
                {
                    TestName = "PhysicsSetup",
                    Passed = passed,
                    Message = passed ?
                        "Physics setup is valid" :
                        $"Physics setup issues found ({physicsIssues.Count})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = physicsIssues,
                    Suggestions = passed ? new List<string>() : new List<string> { "Fix physics setup configuration" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "PhysicsSetup",
                    Passed = false,
                    Message = $"Physics setup test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review physics setup implementation" }
                };
            }
        }

        private async Task<TestResult> TestCircularReferences(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var circularReferences = await FindCircularReferences(contentBundle, cancellationToken);
                var score = circularReferences.Count == 0 ? 100f : 0f;
                var passed = circularReferences.Count == 0;

                return new TestResult
                {
                    TestName = "CircularReferences",
                    Passed = passed,
                    Message = passed ?
                        "No circular references found" :
                        $"Circular references found ({circularReferences.Count})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = circularReferences,
                    Suggestions = passed ? new List<string>() : new List<string> { "Remove circular references" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "CircularReferences",
                    Passed = false,
                    Message = $"Circular references test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review component reference structure" }
                };
            }
        }

        private async Task<TestResult> TestDependencyChain(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var dependencyIssues = await FindDependencyIssues(contentBundle, cancellationToken);
                var score = dependencyIssues.Count == 0 ? 100f : Math.Max(0, 100f - dependencyIssues.Count * 20f);
                var passed = dependencyIssues.Count == 0;

                return new TestResult
                {
                    TestName = "DependencyChain",
                    Passed = passed,
                    Message = passed ?
                        "Dependency chain is valid" :
                        $"Dependency chain issues found ({dependencyIssues.Count})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = dependencyIssues,
                    Suggestions = passed ? new List<string>() : new List<string> { "Fix dependency chain issues" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "DependencyChain",
                    Passed = false,
                    Message = $"Dependency chain test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review dependency chain implementation" }
                };
            }
        }

        private async Task<List<(AssetData, AssetData)>> FindInteractionPairs(ContentBundle contentBundle, CancellationToken cancellationToken)
        {
            var pairs = new List<(AssetData, AssetData)>();
            
            for (int i = 0; i < contentBundle.Assets.Count && !cancellationToken.IsCancellationRequested; i++)
            {
                for (int j = i + 1; j < contentBundle.Assets.Count; j++)
                {
                    if (ShouldInteract(contentBundle.Assets[i], contentBundle.Assets[j]))
                    {
                        pairs.Add((contentBundle.Assets[i], contentBundle.Assets[j]));
                    }
                }
            }
            
            await Task.Delay(10, cancellationToken); // Simulate async work
            return pairs;
        }

        private bool ShouldInteract(AssetData asset1, AssetData asset2)
        {
            // Determine if two assets should interact based on their types
            var type1 = asset1.AssetType.ToLower();
            var type2 = asset2.AssetType.ToLower();
            
            return (type1 == "player" && type2 == "enemy") ||
                   (type1 == "player" && type2 == "collectible") ||
                   (type1 == "weapon" && type2 == "enemy") ||
                   (type1 == "platform" && type2 == "player") ||
                   (type1 == "npc" && type2 == "player");
        }

        private async Task<bool> CanComponentsInteract(AssetData asset1, AssetData asset2, CancellationToken cancellationToken)
        {
            // Simulate interaction check
            await Task.Delay(5, cancellationToken);
            
            // Simple heuristic: different types can interact
            return asset1.AssetType != asset2.AssetType;
        }

        private async Task<List<string>> FindHierarchyIssues(ContentBundle contentBundle, CancellationToken cancellationToken)
        {
            var issues = new List<string>();
            
            // Check for common hierarchy issues
            var rootObjects = contentBundle.Assets.Where(a => a.Path.Split('/').Length == 2).ToList();
            if (rootObjects.Count == 0)
            {
                issues.Add("No root objects found in scene hierarchy");
            }
            
            var duplicateNames = contentBundle.Assets
                .GroupBy(a => a.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            
            if (duplicateNames.Any())
            {
                issues.Add($"Duplicate asset names found: {string.Join(", ", duplicateNames)}");
            }
            
            await Task.Delay(10, cancellationToken); // Simulate async work
            return issues;
        }

        private async Task<List<string>> FindPhysicsIssues(ContentBundle contentBundle, CancellationToken cancellationToken)
        {
            var issues = new List<string>();
            
            // Check for physics-related issues
            var physicsAssets = contentBundle.Assets.Where(a => 
                a.AssetType.ToLower().Contains("collider") || 
                a.AssetType.ToLower().Contains("rigidbody") ||
                a.AssetType.ToLower().Contains("physics")).ToList();
            
            if (physicsAssets.Count == 0)
            {
                issues.Add("No physics components found");
            }
            
            var colliderAssets = contentBundle.Assets.Where(a => 
                a.AssetType.ToLower().Contains("collider")).ToList();
            
            if (colliderAssets.Count == 0)
            {
                issues.Add("No collider components found");
            }
            
            await Task.Delay(10, cancellationToken); // Simulate async work
            return issues;
        }

        private async Task<List<string>> FindCircularReferences(ContentBundle contentBundle, CancellationToken cancellationToken)
        {
            var issues = new List<string>();
            
            // Simple circular reference detection
            var referenceMap = new Dictionary<string, List<string>>();
            
            foreach (var asset in contentBundle.Assets)
            {
                referenceMap[asset.Id] = new List<string>();
                // Simulate finding references
                var references = contentBundle.Assets
                    .Where(a => a.Id != asset.Id && a.Path.Contains(asset.Id))
                    .Select(a => a.Id)
                    .ToList();
                referenceMap[asset.Id] = references;
            }
            
            // Check for circular references
            foreach (var kvp in referenceMap)
            {
                if (HasCircularReference(kvp.Key, referenceMap, new HashSet<string>()))
                {
                    issues.Add($"Circular reference detected involving {kvp.Key}");
                }
            }
            
            await Task.Delay(10, cancellationToken); // Simulate async work
            return issues;
        }

        private bool HasCircularReference(string assetId, Dictionary<string, List<string>> referenceMap, HashSet<string> visited)
        {
            if (visited.Contains(assetId))
                return true;
                
            visited.Add(assetId);
            
            if (referenceMap.ContainsKey(assetId))
            {
                foreach (var reference in referenceMap[assetId])
                {
                    if (HasCircularReference(reference, referenceMap, new HashSet<string>(visited)))
                        return true;
                }
            }
            
            return false;
        }

        private async Task<List<string>> FindDependencyIssues(ContentBundle contentBundle, CancellationToken cancellationToken)
        {
            var issues = new List<string>();
            
            // Check for missing dependencies
            var allAssetIds = contentBundle.Assets.Select(a => a.Id).ToHashSet();
            
            foreach (var asset in contentBundle.Assets)
            {
                // Simulate finding dependencies
                var dependencies = ExtractDependencies(asset);
                var missingDependencies = dependencies.Where(d => !allAssetIds.Contains(d)).ToList();
                
                if (missingDependencies.Any())
                {
                    issues.Add($"Asset {asset.Id} has missing dependencies: {string.Join(", ", missingDependencies)}");
                }
            }
            
            await Task.Delay(10, cancellationToken); // Simulate async work
            return issues;
        }

        private List<string> ExtractDependencies(AssetData asset)
        {
            // Simulate dependency extraction
            var dependencies = new List<string>();
            
            // Simple heuristic: assets with similar names might depend on each other
            var baseName = asset.Id.Split('_')[0];
            if (baseName.Length > 3)
            {
                dependencies.Add($"{baseName}_dependency");
            }
            
            return dependencies;
        }
    }
}
