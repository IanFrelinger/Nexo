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
    /// Test suite for genre-specific validation of generated components.
    /// </summary>
    public class GenreSpecificTestSuite : IComponentTest
    {
        public string Name => "GenreSpecificTestSuite";

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
                    () => TestFPSRequirements(contentBundle, gamePlan, cancellationToken),
                    () => TestPlatformerRequirements(contentBundle, gamePlan, cancellationToken),
                    () => TestRPGRequirements(contentBundle, gamePlan, cancellationToken),
                    () => TestGenreMechanics(contentBundle, gamePlan, cancellationToken),
                    () => TestGenreBalance(contentBundle, gamePlan, cancellationToken)
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

        private async Task<TestResult> TestFPSRequirements(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                if (gamePlan.Genre.ToLower() != "fps")
                {
                    return new TestResult
                    {
                        TestName = "FPSRequirements",
                        Passed = true,
                        Message = "Not an FPS game - skipping FPS requirements",
                        Score = 100f,
                        Duration = DateTimeOffset.UtcNow - startTime
                    };
                }

                var fpsIssues = await FindFPSIssues(contentBundle, gamePlan, cancellationToken);
                var score = fpsIssues.Count == 0 ? 100f : Math.Max(0, 100f - fpsIssues.Count * 20f);
                var passed = fpsIssues.Count == 0;

                return new TestResult
                {
                    TestName = "FPSRequirements",
                    Passed = passed,
                    Message = passed ?
                        "FPS requirements met" :
                        $"FPS requirements issues found ({fpsIssues.Count})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = fpsIssues,
                    Suggestions = passed ? new List<string>() : new List<string> { "Address FPS-specific requirements" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "FPSRequirements",
                    Passed = false,
                    Message = $"FPS requirements test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review FPS requirements validation" }
                };
            }
        }

        private async Task<TestResult> TestPlatformerRequirements(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                if (gamePlan.Genre.ToLower() != "platformer")
                {
                    return new TestResult
                    {
                        TestName = "PlatformerRequirements",
                        Passed = true,
                        Message = "Not a platformer game - skipping platformer requirements",
                        Score = 100f,
                        Duration = DateTimeOffset.UtcNow - startTime
                    };
                }

                var platformerIssues = await FindPlatformerIssues(contentBundle, gamePlan, cancellationToken);
                var score = platformerIssues.Count == 0 ? 100f : Math.Max(0, 100f - platformerIssues.Count * 20f);
                var passed = platformerIssues.Count == 0;

                return new TestResult
                {
                    TestName = "PlatformerRequirements",
                    Passed = passed,
                    Message = passed ?
                        "Platformer requirements met" :
                        $"Platformer requirements issues found ({platformerIssues.Count})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = platformerIssues,
                    Suggestions = passed ? new List<string>() : new List<string> { "Address platformer-specific requirements" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "PlatformerRequirements",
                    Passed = false,
                    Message = $"Platformer requirements test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review platformer requirements validation" }
                };
            }
        }

        private async Task<TestResult> TestRPGRequirements(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                if (gamePlan.Genre.ToLower() != "rpg")
                {
                    return new TestResult
                    {
                        TestName = "RPGRequirements",
                        Passed = true,
                        Message = "Not an RPG game - skipping RPG requirements",
                        Score = 100f,
                        Duration = DateTimeOffset.UtcNow - startTime
                    };
                }

                var rpgIssues = await FindRPGIssues(contentBundle, gamePlan, cancellationToken);
                var score = rpgIssues.Count == 0 ? 100f : Math.Max(0, 100f - rpgIssues.Count * 20f);
                var passed = rpgIssues.Count == 0;

                return new TestResult
                {
                    TestName = "RPGRequirements",
                    Passed = passed,
                    Message = passed ?
                        "RPG requirements met" :
                        $"RPG requirements issues found ({rpgIssues.Count})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = rpgIssues,
                    Suggestions = passed ? new List<string>() : new List<string> { "Address RPG-specific requirements" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "RPGRequirements",
                    Passed = false,
                    Message = $"RPG requirements test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review RPG requirements validation" }
                };
            }
        }

        private async Task<TestResult> TestGenreMechanics(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var mechanicsIssues = await FindGenreMechanicsIssues(contentBundle, gamePlan, cancellationToken);
                var score = mechanicsIssues.Count == 0 ? 100f : Math.Max(0, 100f - mechanicsIssues.Count * 15f);
                var passed = mechanicsIssues.Count == 0;

                return new TestResult
                {
                    TestName = "GenreMechanics",
                    Passed = passed,
                    Message = passed ?
                        "Genre mechanics are properly implemented" :
                        $"Genre mechanics issues found ({mechanicsIssues.Count})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = mechanicsIssues,
                    Suggestions = passed ? new List<string>() : new List<string> { "Improve genre-specific mechanics" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "GenreMechanics",
                    Passed = false,
                    Message = $"Genre mechanics test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review genre mechanics validation" }
                };
            }
        }

        private async Task<TestResult> TestGenreBalance(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var startTime = DateTimeOffset.UtcNow;
            
            try
            {
                var balanceIssues = await FindGenreBalanceIssues(contentBundle, gamePlan, cancellationToken);
                var score = balanceIssues.Count == 0 ? 100f : Math.Max(0, 100f - balanceIssues.Count * 10f);
                var passed = balanceIssues.Count == 0;

                return new TestResult
                {
                    TestName = "GenreBalance",
                    Passed = passed,
                    Message = passed ?
                        "Genre balance is appropriate" :
                        $"Genre balance issues found ({balanceIssues.Count})",
                    Score = score,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = balanceIssues,
                    Suggestions = passed ? new List<string>() : new List<string> { "Adjust genre balance" }
                };
            }
            catch (Exception ex)
            {
                return new TestResult
                {
                    TestName = "GenreBalance",
                    Passed = false,
                    Message = $"Genre balance test failed: {ex.Message}",
                    Score = 0f,
                    Duration = DateTimeOffset.UtcNow - startTime,
                    Issues = new List<string> { ex.Message },
                    Suggestions = new List<string> { "Review genre balance validation" }
                };
            }
        }

        private async Task<List<string>> FindFPSIssues(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var issues = new List<string>();
            
            // Check for FPS-specific requirements
            var weapons = contentBundle.Assets.Where(a => a.AssetType.ToLower().Contains("weapon")).ToList();
            if (weapons.Count == 0)
            {
                issues.Add("No weapons found - FPS games require weapons");
            }
            
            var enemies = contentBundle.Assets.Where(a => a.AssetType.ToLower().Contains("enemy")).ToList();
            if (enemies.Count == 0)
            {
                issues.Add("No enemies found - FPS games require enemies");
            }
            
            var spawnPoints = contentBundle.Assets.Where(a => a.AssetType.ToLower().Contains("spawn")).ToList();
            if (spawnPoints.Count == 0)
            {
                issues.Add("No spawn points found - FPS games require spawn points");
            }
            
            var ammo = contentBundle.Assets.Where(a => a.AssetType.ToLower().Contains("ammo")).ToList();
            if (ammo.Count == 0)
            {
                issues.Add("No ammo found - FPS games require ammunition");
            }
            
            await Task.Delay(10, cancellationToken); // Simulate async work
            return issues;
        }

        private async Task<List<string>> FindPlatformerIssues(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var issues = new List<string>();
            
            // Check for platformer-specific requirements
            var platforms = contentBundle.Assets.Where(a => a.AssetType.ToLower().Contains("platform")).ToList();
            if (platforms.Count == 0)
            {
                issues.Add("No platforms found - platformer games require platforms");
            }
            
            var collectibles = contentBundle.Assets.Where(a => a.AssetType.ToLower().Contains("collectible")).ToList();
            if (collectibles.Count == 0)
            {
                issues.Add("No collectibles found - platformer games require collectibles");
            }
            
            var hazards = contentBundle.Assets.Where(a => a.AssetType.ToLower().Contains("hazard")).ToList();
            if (hazards.Count == 0)
            {
                issues.Add("No hazards found - platformer games require hazards");
            }
            
            var powerUps = contentBundle.Assets.Where(a => a.AssetType.ToLower().Contains("powerup")).ToList();
            if (powerUps.Count == 0)
            {
                issues.Add("No power-ups found - platformer games benefit from power-ups");
            }
            
            await Task.Delay(10, cancellationToken); // Simulate async work
            return issues;
        }

        private async Task<List<string>> FindRPGIssues(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var issues = new List<string>();
            
            // Check for RPG-specific requirements
            var npcs = contentBundle.Assets.Where(a => a.AssetType.ToLower().Contains("npc")).ToList();
            if (npcs.Count == 0)
            {
                issues.Add("No NPCs found - RPG games require NPCs");
            }
            
            var quests = contentBundle.Assets.Where(a => a.AssetType.ToLower().Contains("quest")).ToList();
            if (quests.Count == 0)
            {
                issues.Add("No quests found - RPG games require quests");
            }
            
            var dialogue = contentBundle.Assets.Where(a => a.AssetType.ToLower().Contains("dialogue")).ToList();
            if (dialogue.Count == 0)
            {
                issues.Add("No dialogue found - RPG games require dialogue");
            }
            
            var inventory = contentBundle.Assets.Where(a => a.AssetType.ToLower().Contains("inventory")).ToList();
            if (inventory.Count == 0)
            {
                issues.Add("No inventory found - RPG games require inventory system");
            }
            
            await Task.Delay(10, cancellationToken); // Simulate async work
            return issues;
        }

        private async Task<List<string>> FindGenreMechanicsIssues(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var issues = new List<string>();
            
            // Check for genre-specific mechanics
            var requiredMechanics = GetRequiredMechanics(gamePlan.Genre);
            var presentMechanics = await GetPresentMechanics(contentBundle, cancellationToken);
            
            foreach (var requiredMechanic in requiredMechanics)
            {
                if (!presentMechanics.Contains(requiredMechanic))
                {
                    issues.Add($"Missing required mechanic: {requiredMechanic}");
                }
            }
            
            await Task.Delay(10, cancellationToken); // Simulate async work
            return issues;
        }

        private async Task<List<string>> FindGenreBalanceIssues(ContentBundle contentBundle, GamePlan gamePlan, CancellationToken cancellationToken)
        {
            var issues = new List<string>();
            
            // Check for genre balance
            var assetTypeCounts = contentBundle.Assets
                .GroupBy(a => a.AssetType)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var totalAssets = contentBundle.Assets.Count;
            
            foreach (var kvp in assetTypeCounts)
            {
                var percentage = (float)kvp.Value / totalAssets * 100f;
                var expectedRange = GetExpectedAssetTypeRange(kvp.Key, gamePlan.Genre);
                
                if (percentage < expectedRange.Min || percentage > expectedRange.Max)
                {
                    issues.Add($"Asset type {kvp.Key} is {percentage:F1}% of total (expected {expectedRange.Min}-{expectedRange.Max}%)");
                }
            }
            
            await Task.Delay(10, cancellationToken); // Simulate async work
            return issues;
        }

        private List<string> GetRequiredMechanics(string genre)
        {
            return genre.ToLower() switch
            {
                "fps" => new List<string> { "Shooting", "Movement", "Aiming", "Reloading" },
                "platformer" => new List<string> { "Jumping", "Running", "Collecting", "Avoiding" },
                "rpg" => new List<string> { "Character Progression", "Quest System", "Dialogue", "Inventory" },
                _ => new List<string> { "Basic Movement", "Basic Interaction" }
            };
        }

        private async Task<List<string>> GetPresentMechanics(ContentBundle contentBundle, CancellationToken cancellationToken)
        {
            // Simulate mechanics detection
            await Task.Delay(10, cancellationToken);
            
            var mechanics = new List<string>();
            
            // Simple heuristic based on asset types
            if (contentBundle.Assets.Any(a => a.AssetType.ToLower().Contains("weapon")))
                mechanics.Add("Shooting");
            if (contentBundle.Assets.Any(a => a.AssetType.ToLower().Contains("platform")))
                mechanics.Add("Jumping");
            if (contentBundle.Assets.Any(a => a.AssetType.ToLower().Contains("npc")))
                mechanics.Add("Dialogue");
            if (contentBundle.Assets.Any(a => a.AssetType.ToLower().Contains("collectible")))
                mechanics.Add("Collecting");
            
            return mechanics;
        }

        private (float Min, float Max) GetExpectedAssetTypeRange(string assetType, string genre)
        {
            return assetType.ToLower() switch
            {
                "weapon" when genre.ToLower() == "fps" => (10f, 30f),
                "enemy" when genre.ToLower() == "fps" => (15f, 40f),
                "platform" when genre.ToLower() == "platformer" => (20f, 50f),
                "collectible" when genre.ToLower() == "platformer" => (10f, 30f),
                "npc" when genre.ToLower() == "rpg" => (5f, 25f),
                "quest" when genre.ToLower() == "rpg" => (5f, 20f),
                _ => (0f, 100f)
            };
        }
    }
}
