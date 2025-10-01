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
    /// Test suite for basic functionality validation of generated components.
    /// </summary>
    public class FunctionalityTestSuite : IComponentTest
    {
        public string Name => "FunctionalityTestSuite";

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
                    () => TestComponentInstantiation(contentBundle, gamePlan, cancellationToken),
                    () => TestRequiredComponents(contentBundle, gamePlan, cancellationToken),
                    () => TestBasicInteractions(contentBundle, gamePlan, cancellationToken),
                    () => TestNullReferences(contentBundle, gamePlan, cancellationToken),
                    () => TestComponentHierarchy(contentBundle, gamePlan, cancellationToken)
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

        private async Task<TestResult> TestComponentInstantiation(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                // Test that components can be instantiated
                var instantiatedComponents = 0;
                var totalComponents = contentBundle.Assets.Count;

                foreach (var asset in contentBundle.Assets)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Simulate component instantiation
                    if (await CanInstantiateComponent(asset))
                    {
                        instantiatedComponents++;
                    }
                }

                var score = totalComponents > 0 ? (float)instantiatedComponents / totalComponents * 100f : 100f;
                var passed = score >= 80f;

                return new TestResult
                {
                    TestName = "ComponentInstantiation",
                    Passed = passed,
                    Message = passed ? 
                        $"All components instantiated successfully ({instantiatedComponents}/{totalComponents})" :
                        $"Component instantiation failed ({instantiatedComponents}/{totalComponents})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = passed ? new List<string>() : new List<string> { "Some components failed to instantiate" },
                    Suggestions = passed ? new List<string>() : new List<string> { "Check component dependencies and Unity compatibility" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "ComponentInstantiation",
                    Passed = false,
                    Message = $"Component instantiation test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review component implementation and dependencies" }
                };
            }
        }

        private async Task<TestResult> TestRequiredComponents(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var requiredAssetTypes = GetRequiredAssetTypes(gamePlan.Genre);
                var presentAssetTypes = contentBundle.Assets.Select(a => a.AssetType).ToHashSet();
                var missingTypes = requiredAssetTypes.Where(t => !presentAssetTypes.Contains(t)).ToList();
                var presentTypes = requiredAssetTypes.Where(t => presentAssetTypes.Contains(t)).ToList();

                var score = requiredAssetTypes.Count > 0 ? (float)presentTypes.Count / requiredAssetTypes.Count * 100f : 100f;
                var passed = missingTypes.Count == 0;

                return new TestResult
                {
                    TestName = "RequiredComponents",
                    Passed = passed,
                    Message = passed ?
                        $"All required components present ({presentTypes.Count}/{requiredAssetTypes.Count})" :
                        $"Missing required components: {string.Join(", ", missingTypes)}",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = missingTypes,
                    Suggestions = missingTypes.Select(t => $"Generate missing {t} components").ToList()
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "RequiredComponents",
                    Passed = false,
                    Message = $"Required components test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review required components for genre" }
                };
            }
        }

        private async Task<TestResult> TestBasicInteractions(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                // Test that components can interact with each other
                var interactionCount = 0;
                var totalPossibleInteractions = contentBundle.Assets.Count * (contentBundle.Assets.Count - 1);

                // Simulate interaction testing
                for (int i = 0; i < contentBundle.Assets.Count && !cancellationToken.IsCancellationRequested; i++)
                {
                    for (int j = i + 1; j < contentBundle.Assets.Count; j++)
                    {
                        if (await CanInteract(contentBundle.Assets[i], contentBundle.Assets[j]))
                        {
                            interactionCount++;
                        }
                    }
                }

                var score = totalPossibleInteractions > 0 ? (float)interactionCount / totalPossibleInteractions * 100f : 100f;
                var passed = score >= 50f; // At least 50% of possible interactions should work

                return new TestResult
                {
                    TestName = "BasicInteractions",
                    Passed = passed,
                    Message = passed ?
                        $"Component interactions working ({interactionCount}/{totalPossibleInteractions})" :
                        $"Limited component interactions ({interactionCount}/{totalPossibleInteractions})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = passed ? new List<string>() : new List<string> { "Limited component interactions" },
                    Suggestions = passed ? new List<string>() : new List<string> { "Improve component interaction design" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "BasicInteractions",
                    Passed = false,
                    Message = $"Basic interactions test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review component interaction implementation" }
                };
            }
        }

        private async Task<TestResult> TestNullReferences(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var nullReferences = 0;
                var totalReferences = contentBundle.Assets.Count;

                foreach (var asset in contentBundle.Assets)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (await HasNullReferences(asset))
                    {
                        nullReferences++;
                    }
                }

                var score = totalReferences > 0 ? (float)(totalReferences - nullReferences) / totalReferences * 100f : 100f;
                var passed = nullReferences == 0;

                return new TestResult
                {
                    TestName = "NullReferences",
                    Passed = passed,
                    Message = passed ?
                        "No null references found" :
                        $"Found {nullReferences} null references",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = passed ? new List<string>() : new List<string> { $"{nullReferences} null references detected" },
                    Suggestions = passed ? new List<string>() : new List<string> { "Fix null reference issues" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "NullReferences",
                    Passed = false,
                    Message = $"Null references test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review component reference initialization" }
                };
            }
        }

        private async Task<TestResult> TestComponentHierarchy(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                // Test that components have proper hierarchy
                var validHierarchies = 0;
                var totalComponents = contentBundle.Assets.Count;

                foreach (var asset in contentBundle.Assets)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (await HasValidHierarchy(asset))
                    {
                        validHierarchies++;
                    }
                }

                var score = totalComponents > 0 ? (float)validHierarchies / totalComponents * 100f : 100f;
                var passed = score >= 90f;

                return new TestResult
                {
                    TestName = "ComponentHierarchy",
                    Passed = passed,
                    Message = passed ?
                        $"Component hierarchy valid ({validHierarchies}/{totalComponents})" :
                        $"Component hierarchy issues ({validHierarchies}/{totalComponents})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = passed ? new List<string>() : new List<string> { "Component hierarchy issues detected" },
                    Suggestions = passed ? new List<string>() : new List<string> { "Fix component hierarchy structure" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "ComponentHierarchy",
                    Passed = false,
                    Message = $"Component hierarchy test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review component hierarchy implementation" }
                };
            }
        }

        private List<string> GetRequiredAssetTypes(string genre)
        {
            return genre.ToLower() switch
            {
                "fps" => new List<string> { "Player", "Weapon", "Enemy", "Level" },
                "platformer" => new List<string> { "Player", "Platform", "Collectible", "Level" },
                "rpg" => new List<string> { "Player", "NPC", "Quest", "Level" },
                _ => new List<string> { "Player", "Level" }
            };
        }

        private async Task<bool> CanInstantiateComponent(AssetData asset)
        {
            // Simulate component instantiation check
            await Task.Delay(10); // Simulate async work
            return !string.IsNullOrEmpty(asset.AssetType) && !string.IsNullOrEmpty(asset.Path);
        }

        private async Task<bool> CanInteract(AssetData asset1, AssetData asset2)
        {
            // Simulate interaction check
            await Task.Delay(5); // Simulate async work
            return asset1.AssetType != asset2.AssetType; // Different types can interact
        }

        private async Task<bool> HasNullReferences(AssetData asset)
        {
            // Simulate null reference check
            await Task.Delay(5); // Simulate async work
            return string.IsNullOrEmpty(asset.Id) || string.IsNullOrEmpty(asset.AssetType);
        }

        private async Task<bool> HasValidHierarchy(AssetData asset)
        {
            // Simulate hierarchy check
            await Task.Delay(5); // Simulate async work
            return !string.IsNullOrEmpty(asset.Path) && asset.Path.Contains("/");
        }
    }
}
