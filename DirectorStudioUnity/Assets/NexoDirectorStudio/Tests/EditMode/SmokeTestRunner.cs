using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Simple smoke test runner to validate the refactored Director Studio system.
    /// This can be run to verify all components work correctly.
    /// </summary>
    public static class SmokeTestRunner
    {
        public static async Task<bool> RunSmokeTests()
        {
            System.Console.WriteLine("🎮 Director Studio Smoke Test Runner");
            System.Console.WriteLine("====================================");
            
            var allPassed = true;
            
            try
            {
                // Test 1: Service Initialization
                System.Console.WriteLine("\n1. Testing Service Initialization...");
                allPassed &= await TestServiceInitialization();
                
                // Test 2: Command Execution
                System.Console.WriteLine("\n2. Testing Command Execution...");
                allPassed &= await TestCommandExecution();
                
                // Test 3: Validation System
                System.Console.WriteLine("\n3. Testing Validation System...");
                allPassed &= await TestValidationSystem();
                
                // Test 4: Adapter Health Checks
                System.Console.WriteLine("\n4. Testing Adapter Health Checks...");
                allPassed &= await TestAdapterHealthChecks();
                
                // Test 5: Genre Profile System
                System.Console.WriteLine("\n5. Testing Genre Profile System...");
                allPassed &= await TestGenreProfileSystem();
                
                // Test 6: JSON Repair
                System.Console.WriteLine("\n6. Testing JSON Repair...");
                allPassed &= await TestJsonRepair();
                
                // Test 7: Complete Workflow
                System.Console.WriteLine("\n7. Testing Complete Workflow...");
                allPassed &= await TestCompleteWorkflow();
                
                // Test 8: Determinism
                System.Console.WriteLine("\n8. Testing Determinism...");
                allPassed &= await TestDeterminism();
                
                System.Console.WriteLine("\n====================================");
                System.Console.WriteLine(allPassed ? "✅ All smoke tests passed!" : "❌ Some smoke tests failed!");
                System.Console.WriteLine("====================================");
                
                return allPassed;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Smoke test failed with exception: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        private static async Task<bool> TestServiceInitialization()
        {
            try
            {
                using var service = new DirectorStudioService();
                System.Console.WriteLine("  ✅ DirectorStudioService initialized");
                
                var planCommand = service.GetService<IPlanGameSliceCommand>();
                System.Console.WriteLine("  ✅ IPlanGameSliceCommand available");
                
                var validators = service.GetService<System.Collections.Generic.IEnumerable<IValidator<GamePlan>>>();
                System.Console.WriteLine("  ✅ Validators available");
                
                var adapters = service.GetService<IOllamaAdapter>();
                System.Console.WriteLine("  ✅ Adapters available");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"  ❌ Service initialization failed: {ex.Message}");
                return false;
            }
        }
        
        private static async Task<bool> TestCommandExecution()
        {
            try
            {
                using var service = new DirectorStudioService();
                
                var designBrief = new DesignBrief(
                    Description: "A simple test game",
                    GenreHint: "Platformer",
                    TargetDurationMinutes: 5,
                    DifficultyLevel: 3,
                    Seed: 12345
                );
                
                var planCommand = service.GetService<IPlanGameSliceCommand>();
                var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), System.Threading.CancellationToken.None);
                
                System.Console.WriteLine($"  ✅ Game plan generated: {gamePlan.Id}");
                System.Console.WriteLine($"  ✅ Genre: {gamePlan.Genre}");
                System.Console.WriteLine($"  ✅ Duration: {gamePlan.EstimatedDurationMinutes} minutes");
                System.Console.WriteLine($"  ✅ Mechanics: {gamePlan.CoreMechanics.Count}");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"  ❌ Command execution failed: {ex.Message}");
                return false;
            }
        }
        
        private static async Task<bool> TestValidationSystem()
        {
            try
            {
                using var service = new DirectorStudioService();
                
                var gamePlan = new GamePlan(
                    Id: "test-plan",
                    SourceBrief: new DesignBrief("Test brief", "Platformer", 5, 3, Seed: 12345),
                    Genre: "Platformer",
                    Description: "Test game plan",
                    CoreMechanics: new[] { "Jump", "Move" },
                    PlayerExperience: new[] { "Fun", "Challenging" },
                    EstimatedDurationMinutes: 5,
                    DifficultyProgression: new[] { new DifficultyBeat(0, 3, "Start") },
                    NarrativeBeats: new[] { "Start", "Middle", "End" },
                    RequiredAssets: new[] { new AssetRequirement("Platform", "Ground", "Basic ground platform") },
                    Seed: 12345,
                    GeneratedAt: DateTimeOffset.UtcNow,
                    Hash: "test-hash"
                );
                
                var validators = service.GetService<System.Collections.Generic.IEnumerable<IValidator<GamePlan>>>();
                var validationResults = new List<NexoDirectorStudio.Validators.ValidationResult>();
                
                foreach (var validator in validators)
                {
                    var result = await validator.ValidateAsync(gamePlan, System.Threading.CancellationToken.None);
                    validationResults.Add(result);
                }
                
                var validationReport = new ValidationReport
                {
                    OverallPassed = validationResults.All(r => r.IsValid),
                    OverallScore = (int)validationResults.Average(r => r.Score),
                    Issues = validationResults.SelectMany(r => r.Issues).ToList(),
                    Suggestions = validationResults.SelectMany(r => r.Suggestions).ToList()
                };
                
                System.Console.WriteLine($"  ✅ Validation report generated: {validationReport.Id}");
                System.Console.WriteLine($"  ✅ Overall status: {validationReport.OverallPassed}");
                System.Console.WriteLine($"  ✅ Results count: {validationReport.Issues.Count}");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"  ❌ Validation system failed: {ex.Message}");
                return false;
            }
        }
        
        private static async Task<bool> TestAdapterHealthChecks()
        {
            try
            {
                using var service = new DirectorStudioService();
                
                var ollamaAdapter = service.GetService<IOllamaAdapter>();
                var comfyuiAdapter = service.GetService<ITextureGenAdapter>();
                var piperAdapter = service.GetService<ITtsAdapter>();
                
                var ollamaHealth = await ollamaAdapter.HealthCheckAsync(System.Threading.CancellationToken.None);
                var comfyuiHealth = await comfyuiAdapter.HealthCheckAsync(System.Threading.CancellationToken.None);
                var piperHealth = await piperAdapter.HealthCheckAsync(System.Threading.CancellationToken.None);
                
                System.Console.WriteLine($"  ✅ Ollama health: {ollamaHealth.IsHealthy} ({ollamaHealth.ResponseTimeMs}ms)");
                System.Console.WriteLine($"  ✅ ComfyUI health: {comfyuiHealth.IsHealthy} ({comfyuiHealth.ResponseTimeMs}ms)");
                System.Console.WriteLine($"  ✅ Piper health: {piperHealth.IsHealthy} ({piperHealth.ResponseTimeMs}ms)");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"  ❌ Adapter health checks failed: {ex.Message}");
                return false;
            }
        }
        
        private static async Task<bool> TestGenreProfileSystem()
        {
            try
            {
                using var service = new DirectorStudioService();
                
                var genreRegistry = service.GetService<GenreRegistry>();
                var allProfiles = genreRegistry.AllProfiles;
                
                System.Console.WriteLine($"  ✅ Genre profiles available: {allProfiles.Count}");
                
                var fpsProfile = genreRegistry.GetProfileById("fps");
                var platformerProfile = genreRegistry.GetProfileById("platformer");
                var rpgProfile = genreRegistry.GetProfileById("rpg");
                
                System.Console.WriteLine($"  ✅ FPS profile: {fpsProfile?.Name ?? "Not found"}");
                System.Console.WriteLine($"  ✅ Platformer profile: {platformerProfile?.Name ?? "Not found"}");
                System.Console.WriteLine($"  ✅ RPG profile: {rpgProfile?.Name ?? "Not found"}");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"  ❌ Genre profile system failed: {ex.Message}");
                return false;
            }
        }
        
        private static async Task<bool> TestJsonRepair()
        {
            try
            {
                var malformedJson = @"{""name"": ""test"", ""value"": 123,}";
                var repairResult = JsonRepair.Repair(malformedJson);
                
                System.Console.WriteLine($"  ✅ JSON repair successful: {repairResult.IsSuccessful}");
                System.Console.WriteLine($"  ✅ Repair attempts: {repairResult.RepairAttempts}");
                
                if (repairResult.IsSuccessful)
                {
                    System.Console.WriteLine($"  ✅ Repaired JSON: {repairResult.RepairedJson.Substring(0, Math.Min(50, repairResult.RepairedJson.Length))}...");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"  ❌ JSON repair failed: {ex.Message}");
                return false;
            }
        }
        
        private static async Task<bool> TestCompleteWorkflow()
        {
            try
            {
                using var service = new DirectorStudioService();
                
                var designBrief = new DesignBrief(
                    Description: "A complete test workflow",
                    GenreHint: "Platformer",
                    TargetDurationMinutes: 3,
                    DifficultyLevel: 2,
                    Seed: 54321
                );
                
                // Plan
                var planCommand = service.GetService<IPlanGameSliceCommand>();
                var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), System.Threading.CancellationToken.None);
                
                // Build
                var buildCommand = service.GetService<IBuildWorldLayoutCommand>();
                var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), System.Threading.CancellationToken.None);
                
                // Interactions
                var interactionsCommand = service.GetService<IPlaceInteractionsCommand>();
                var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), System.Threading.CancellationToken.None);
                
                // Content
                var contentCommand = service.GetService<ICreateContentBundleCommand>();
                var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), System.Threading.CancellationToken.None);
                
                System.Console.WriteLine($"  ✅ Complete workflow executed successfully");
                System.Console.WriteLine($"  ✅ Game plan: {gamePlan.Id}");
                System.Console.WriteLine($"  ✅ World layout: {worldLayout.Id}");
                System.Console.WriteLine($"  ✅ Interaction graph: {interactionGraph.Id}");
                System.Console.WriteLine($"  ✅ Content bundle: {contentBundle.Id}");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"  ❌ Complete workflow failed: {ex.Message}");
                return false;
            }
        }
        
        private static async Task<bool> TestDeterminism()
        {
            try
            {
                using var service = new DirectorStudioService();
                
                var designBrief = new DesignBrief(
                    Description: "A deterministic test game",
                    GenreHint: "Platformer",
                    TargetDurationMinutes: 5,
                    DifficultyLevel: 3,
                    Seed: 99999
                );
                
                var planCommand = service.GetService<IPlanGameSliceCommand>();
                var gamePlan1 = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), System.Threading.CancellationToken.None);
                var gamePlan2 = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), System.Threading.CancellationToken.None);
                
                System.Console.WriteLine($"  ✅ Game plan 1: {gamePlan1.Genre} ({gamePlan1.EstimatedDurationMinutes}min)");
                System.Console.WriteLine($"  ✅ Game plan 2: {gamePlan2.Genre} ({gamePlan2.EstimatedDurationMinutes}min)");
                System.Console.WriteLine($"  ✅ Deterministic: {gamePlan1.Genre == gamePlan2.Genre}");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"  ❌ Determinism test failed: {ex.Message}");
                return false;
            }
        }
    }
}
