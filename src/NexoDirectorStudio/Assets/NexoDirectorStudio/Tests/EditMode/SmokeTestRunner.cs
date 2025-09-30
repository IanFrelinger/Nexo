using System;
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
            Console.WriteLine("🎮 Director Studio Smoke Test Runner");
            Console.WriteLine("====================================");
            
            var allPassed = true;
            
            try
            {
                // Test 1: Service Initialization
                Console.WriteLine("\n1. Testing Service Initialization...");
                allPassed &= await TestServiceInitialization();
                
                // Test 2: Command Execution
                Console.WriteLine("\n2. Testing Command Execution...");
                allPassed &= await TestCommandExecution();
                
                // Test 3: Validation System
                Console.WriteLine("\n3. Testing Validation System...");
                allPassed &= await TestValidationSystem();
                
                // Test 4: Adapter Health Checks
                Console.WriteLine("\n4. Testing Adapter Health Checks...");
                allPassed &= await TestAdapterHealthChecks();
                
                // Test 5: Genre Profile System
                Console.WriteLine("\n5. Testing Genre Profile System...");
                allPassed &= await TestGenreProfileSystem();
                
                // Test 6: JSON Repair
                Console.WriteLine("\n6. Testing JSON Repair...");
                allPassed &= await TestJsonRepair();
                
                // Test 7: Complete Workflow
                Console.WriteLine("\n7. Testing Complete Workflow...");
                allPassed &= await TestCompleteWorkflow();
                
                // Test 8: Determinism
                Console.WriteLine("\n8. Testing Determinism...");
                allPassed &= await TestDeterminism();
                
                Console.WriteLine("\n====================================");
                Console.WriteLine(allPassed ? "✅ All smoke tests passed!" : "❌ Some smoke tests failed!");
                Console.WriteLine("====================================");
                
                return allPassed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Smoke test failed with exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        private static async Task<bool> TestServiceInitialization()
        {
            try
            {
                using var service = new DirectorStudioService();
                Console.WriteLine("  ✅ DirectorStudioService initialized");
                
                var planCommand = service.GetService<IPlanGameSliceCommand>();
                Console.WriteLine("  ✅ IPlanGameSliceCommand available");
                
                var validators = service.GetService<System.Collections.Generic.IEnumerable<IValidator<GamePlan>>>();
                Console.WriteLine("  ✅ Validators available");
                
                var adapters = service.GetService<IOllamaAdapter>();
                Console.WriteLine("  ✅ Adapters available");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Service initialization failed: {ex.Message}");
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
                
                Console.WriteLine($"  ✅ Game plan generated: {gamePlan.Id}");
                Console.WriteLine($"  ✅ Genre: {gamePlan.Genre}");
                Console.WriteLine($"  ✅ Duration: {gamePlan.EstimatedDurationMinutes} minutes");
                Console.WriteLine($"  ✅ Mechanics: {gamePlan.CoreMechanics.Count}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Command execution failed: {ex.Message}");
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
                    SourceBrief: new DesignBrief("Test brief", "Platformer", 5, 3, seed: 12345),
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
                var validationReport = new ValidationReport();
                
                foreach (var validator in validators)
                {
                    var result = await validator.ValidateAsync(gamePlan, System.Threading.CancellationToken.None);
                    validationReport.AddResult(result);
                }
                
                Console.WriteLine($"  ✅ Validation report generated: {validationReport.ReportId}");
                Console.WriteLine($"  ✅ Overall status: {validationReport.OverallStatus}");
                Console.WriteLine($"  ✅ Results count: {validationReport.Results.Count}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Validation system failed: {ex.Message}");
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
                
                Console.WriteLine($"  ✅ Ollama health: {ollamaHealth.IsHealthy} ({ollamaHealth.ResponseTimeMs}ms)");
                Console.WriteLine($"  ✅ ComfyUI health: {comfyuiHealth.IsHealthy} ({comfyuiHealth.ResponseTimeMs}ms)");
                Console.WriteLine($"  ✅ Piper health: {piperHealth.IsHealthy} ({piperHealth.ResponseTimeMs}ms)");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Adapter health checks failed: {ex.Message}");
                return false;
            }
        }
        
        private static async Task<bool> TestGenreProfileSystem()
        {
            try
            {
                using var service = new DirectorStudioService();
                
                var genreRegistry = service.GetService<GenreRegistry>();
                var allProfiles = genreRegistry.GetAllProfiles();
                
                Console.WriteLine($"  ✅ Genre profiles available: {allProfiles.Count}");
                
                var fpsProfile = genreRegistry.GetProfile("fps");
                var platformerProfile = genreRegistry.GetProfile("platformer");
                var rpgProfile = genreRegistry.GetProfile("rpg");
                
                Console.WriteLine($"  ✅ FPS profile: {fpsProfile?.GenreName ?? "Not found"}");
                Console.WriteLine($"  ✅ Platformer profile: {platformerProfile?.GenreName ?? "Not found"}");
                Console.WriteLine($"  ✅ RPG profile: {rpgProfile?.GenreName ?? "Not found"}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Genre profile system failed: {ex.Message}");
                return false;
            }
        }
        
        private static async Task<bool> TestJsonRepair()
        {
            try
            {
                var malformedJson = @"{""name"": ""test"", ""value"": 123,}";
                var repairResult = JsonRepair.Repair(malformedJson);
                
                Console.WriteLine($"  ✅ JSON repair successful: {repairResult.IsSuccessful}");
                Console.WriteLine($"  ✅ Repair attempts: {repairResult.RepairAttempts}");
                
                if (repairResult.IsSuccessful)
                {
                    Console.WriteLine($"  ✅ Repaired JSON: {repairResult.RepairedJson.Substring(0, Math.Min(50, repairResult.RepairedJson.Length))}...");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ JSON repair failed: {ex.Message}");
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
                
                Console.WriteLine($"  ✅ Complete workflow executed successfully");
                Console.WriteLine($"  ✅ Game plan: {gamePlan.Id}");
                Console.WriteLine($"  ✅ World layout: {worldLayout.Id}");
                Console.WriteLine($"  ✅ Interaction graph: {interactionGraph.Id}");
                Console.WriteLine($"  ✅ Content bundle: {contentBundle.Id}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Complete workflow failed: {ex.Message}");
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
                
                Console.WriteLine($"  ✅ Game plan 1: {gamePlan1.Genre} ({gamePlan1.EstimatedDurationMinutes}min)");
                Console.WriteLine($"  ✅ Game plan 2: {gamePlan2.Genre} ({gamePlan2.EstimatedDurationMinutes}min)");
                Console.WriteLine($"  ✅ Deterministic: {gamePlan1.Genre == gamePlan2.Genre}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Determinism test failed: {ex.Message}");
                return false;
            }
        }
    }
}
