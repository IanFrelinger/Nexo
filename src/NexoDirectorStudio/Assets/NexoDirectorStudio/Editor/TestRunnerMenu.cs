using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NexoDirectorStudio.Validation;
using NexoDirectorStudio.Tests.EditMode;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Unity Editor menu items for running component validation tests.
    /// Provides easy access to validation testing from the Unity menu.
    /// </summary>
    public static class TestRunnerMenu
    {
        [MenuItem("Nexo/Director Studio/Run Component Validation Tests")]
        public static async void RunComponentValidationTests()
        {
            Debug.Log("🧪 Starting component validation tests...");
            
            try
            {
                var results = await TestRunnerConfiguration.RunMultiGenreValidationAsync(
                    new[] { "FPS", "Platformer", "RPG" },
                    "Unity Test Runner Validation"
                );

                var report = TestRunnerConfiguration.GenerateMultiGenreReport(results);
                Debug.Log(report);

                var allPassed = TestRunnerConfiguration.ValidateQualityStandards(results);
                if (allPassed)
                {
                    Debug.Log("✅ All component validation tests PASSED!");
                }
                else
                {
                    Debug.LogWarning("⚠️ Some component validation tests FAILED. Check the report above for details.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Component validation tests failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Run FPS Validation Tests")]
        public static async void RunFPSValidationTests()
        {
            Debug.Log("🎯 Running FPS component validation tests...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunGenreValidationAsync(
                    "FPS", 
                    "Doom-style FPS with weapons and enemies", 
                    5, 4, 123
                );

                if (result.OverallPassed)
                {
                    Debug.Log($"✅ FPS validation PASSED - Score: {result.OverallScore:F1}%");
                }
                else
                {
                    Debug.LogWarning($"❌ FPS validation FAILED - Score: {result.OverallScore:F1}%");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ FPS validation tests failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Run Platformer Validation Tests")]
        public static async void RunPlatformerValidationTests()
        {
            Debug.Log("🦘 Running Platformer component validation tests...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunGenreValidationAsync(
                    "Platformer", 
                    "Mario-style platformer with jumps and collectibles", 
                    3, 2, 456
                );

                if (result.OverallPassed)
                {
                    Debug.Log($"✅ Platformer validation PASSED - Score: {result.OverallScore:F1}%");
                }
                else
                {
                    Debug.LogWarning($"❌ Platformer validation FAILED - Score: {result.OverallScore:F1}%");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Platformer validation tests failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Run RPG Validation Tests")]
        public static async void RunRPGValidationTests()
        {
            Debug.Log("⚔️ Running RPG component validation tests...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunGenreValidationAsync(
                    "RPG", 
                    "Fantasy RPG with NPCs and quests", 
                    10, 4, 789
                );

                if (result.OverallPassed)
                {
                    Debug.Log($"✅ RPG validation PASSED - Score: {result.OverallScore:F1}%");
                }
                else
                {
                    Debug.LogWarning($"❌ RPG validation FAILED - Score: {result.OverallScore:F1}%");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ RPG validation tests failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Run Performance Validation Tests")]
        public static async void RunPerformanceValidationTests()
        {
            Debug.Log("⚡ Running Performance validation tests...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunPerformanceValidationAsync();

                if (result.OverallPassed)
                {
                    Debug.Log($"✅ Performance validation PASSED - Score: {result.OverallScore:F1}%");
                }
                else
                {
                    Debug.LogWarning($"❌ Performance validation FAILED - Score: {result.OverallScore:F1}%");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Performance validation tests failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Run Accessibility Validation Tests")]
        public static async void RunAccessibilityValidationTests()
        {
            Debug.Log("♿ Running Accessibility validation tests...");
            
            try
            {
                var result = await TestRunnerConfiguration.RunAccessibilityValidationAsync();

                if (result.OverallPassed)
                {
                    Debug.Log($"✅ Accessibility validation PASSED - Score: {result.OverallScore:F1}%");
                }
                else
                {
                    Debug.LogWarning($"❌ Accessibility validation FAILED - Score: {result.OverallScore:F1}%");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Accessibility validation tests failed: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Open Test Runner Window")]
        public static void OpenTestRunnerWindow()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
        }

        [MenuItem("Nexo/Director Studio/Export Validation Report")]
        public static async void ExportValidationReport()
        {
            Debug.Log("📄 Generating comprehensive validation report...");
            
            try
            {
                var results = await TestRunnerConfiguration.RunMultiGenreValidationAsync(
                    new[] { "FPS", "Platformer", "RPG" },
                    "Comprehensive Validation Report"
                );

                var report = TestRunnerConfiguration.GenerateMultiGenreReport(results);
                
                // Save to file
                var reportPath = $"Assets/Generated/ValidationReports/comprehensive_report_{System.DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.txt";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(reportPath));
                await System.IO.File.WriteAllTextAsync(reportPath, report);
                
                Debug.Log($"📄 Validation report exported to: {reportPath}");
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Failed to export validation report: {ex.Message}");
            }
        }

        [MenuItem("Nexo/Director Studio/Validate Current Scene Components")]
        public static async void ValidateCurrentSceneComponents()
        {
            Debug.Log("🎮 Validating components in current scene...");
            
            try
            {
                // Find all GameObjects in the current scene that might be generated components
                var sceneObjects = Object.FindObjectsOfType<GameObject>();
                var generatedObjects = sceneObjects.Where(obj => 
                    obj.name.Contains("Generated") || 
                    obj.name.Contains("Weapon") || 
                    obj.name.Contains("Enemy") || 
                    obj.name.Contains("Platform") ||
                    obj.name.Contains("NPC") ||
                    obj.name.Contains("Collectible")
                ).ToList();

                if (generatedObjects.Count == 0)
                {
                    Debug.LogWarning("⚠️ No generated components found in current scene");
                    return;
                }

                Debug.Log($"Found {generatedObjects.Count} potential generated components in scene");

                // Create a mock content bundle for validation
                var mockContentBundle = new ContentBundle
                {
                    Id = "scene_validation",
                    Assets = generatedObjects.Select(obj => new AssetData
                    {
                        Id = obj.GetInstanceID().ToString(),
                        Name = obj.name,
                        AssetType = "Prefab",
                        FilePath = obj.name
                    }).ToList()
                };

                var mockGamePlan = new GamePlan(
                    Id: "scene_validation_plan",
                    SourceBrief: new DesignBrief("Scene validation test"),
                    Genre: "FPS",
                    Description: "Scene validation test",
                    CoreMechanics: new[] { "Move", "Shoot", "Jump" },
                    PlayerExperience: new[] { "Fast-paced action" },
                    EstimatedDurationMinutes: 5,
                    DifficultyProgression: new[] { new DifficultyBeat(0, 3, "Start") },
                    NarrativeBeats: new[] { "Begin adventure" },
                    RequiredAssets: new[] { new AssetRequirement("Weapon", "Test Weapon", "Test weapon for validation", true, 5) },
                    Seed: 123,
                    GeneratedAt: System.DateTimeOffset.UtcNow,
                    Hash: "scene_validation_hash"
                );

                // Run validation
                var testRunner = new TestRunnerIntegration();
                var result = await testRunner.RunValidationTestsAsync(mockContentBundle, mockGamePlan);

                if (result.OverallPassed)
                {
                    Debug.Log($"✅ Scene component validation PASSED - Score: {result.OverallScore:F1}%");
                }
                else
                {
                    Debug.LogWarning($"❌ Scene component validation FAILED - Score: {result.OverallScore:F1}%");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Scene component validation failed: {ex.Message}");
            }
        }
    }
}
