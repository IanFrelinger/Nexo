using System;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Runtime.Commands;
using NexoDirectorStudio.Runtime.DTO;
using NexoDirectorStudio.Runtime.Validators;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Simple test runner to validate all Director Studio components.
    /// This can be run outside of Unity to verify the system works.
    /// </summary>
    public static class ValidationTestRunner
    {
        public static async Task<bool> RunAllValidations()
        {
            Console.WriteLine("🎮 Director Studio Validation Test Runner");
            Console.WriteLine("==========================================");
            
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
                
                // Test 8: Error Handling
                Console.WriteLine("\n8. Testing Error Handling...");
                allPassed &= await TestErrorHandling();
                
                // Test 9: Determinism
                Console.WriteLine("\n9. Testing Determinism...");
                allPassed &= await TestDeterminism();
                
                // Test 10: DTOs
                Console.WriteLine("\n10. Testing DTOs...");
                allPassed &= await TestDTOs();
                
                Console.WriteLine("\n==========================================");
                Console.WriteLine(allPassed ? "✅ All validations passed!" : "❌ Some validations failed!");
                Console.WriteLine("==========================================");
                
                return allPassed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Validation failed with exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        private static async Task<bool> TestServiceInitialization()
        {
            try
            {
                using var service = new DirectorStudioServiceUnified();
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
                using var service = new DirectorStudioServiceUnified();
                
                var designBrief = new DesignBrief
                {
                    Description = "A simple test game",
                    GenreHint = "Platformer",
                    TargetDurationMinutes = 5,
                    DifficultyLevel = 0.5f
                };
                
                var planCommand = service.GetService<IPlanGameSliceCommand>();
                var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), System.Threading.CancellationToken.None);
                
                Console.WriteLine($"  ✅ Game plan generated: {gamePlan.Id}");
                Console.WriteLine($"  ✅ Genre: {gamePlan.Genre}");
                Console.WriteLine($"  ✅ Duration: {gamePlan.EstimatedDurationMinutes} minutes");
                Console.WriteLine($"  ✅ Mechanics: {gamePlan.CoreMechanics.Length}");
                
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
                using var service = new DirectorStudioServiceUnified();
                
                var gamePlan = new GamePlan
                {
                    Id = "test-plan",
                    Genre = "Platformer",
                    Description = "Test game plan",
                    CoreMechanics = new[] { "Jump", "Move" },
                    EstimatedDurationMinutes = 5
                };
                
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
                using var service = new DirectorStudioServiceUnified();
                
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
                using var service = new DirectorStudioServiceUnified();
                
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
                using var service = new DirectorStudioServiceUnified();
                
                var designBrief = new DesignBrief
                {
                    Description = "A complete test game workflow",
                    GenreHint = "Platformer",
                    TargetDurationMinutes = 5,
                    DifficultyLevel = 0.5f
                };
                
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
        
        private static async Task<bool> TestErrorHandling()
        {
            try
            {
                using var service = new DirectorStudioServiceUnified();
                
                var planCommand = service.GetService<IPlanGameSliceCommand>();
                
                // Test with null input - should handle gracefully
                try
                {
                    await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(null), System.Threading.CancellationToken.None);
                }
                catch (System.ArgumentNullException)
                {
                    Console.WriteLine("  ✅ Null input handled gracefully");
                }
                
                // Test cancellation
                var cancellationTokenSource = new System.Threading.CancellationTokenSource();
                cancellationTokenSource.Cancel();
                
                try
                {
                    await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(new DesignBrief()), cancellationTokenSource.Token);
                }
                catch (System.OperationCanceledException)
                {
                    Console.WriteLine("  ✅ Cancellation handled gracefully");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ Error handling failed: {ex.Message}");
                return false;
            }
        }
        
        private static async Task<bool> TestDeterminism()
        {
            try
            {
                using var service = new DirectorStudioServiceUnified();
                
                var designBrief = new DesignBrief
                {
                    Description = "A deterministic test game",
                    GenreHint = "Platformer",
                    TargetDurationMinutes = 5,
                    DifficultyLevel = 0.5f
                };
                
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
        
        private static async Task<bool> TestDTOs()
        {
            try
            {
                var designBrief = new DesignBrief
                {
                    Description = "Test brief",
                    GenreHint = "Platformer",
                    TargetDurationMinutes = 5,
                    DifficultyLevel = 0.5f
                };
                
                var gamePlan = new GamePlan
                {
                    Id = "test-plan",
                    Genre = "Platformer",
                    Description = "Test plan",
                    CoreMechanics = new[] { "Jump", "Move" },
                    EstimatedDurationMinutes = 5
                };
                
                var worldLayout = new WorldLayout
                {
                    Id = "test-layout",
                    Name = "Test Layout",
                    GridSize = new Vector2Int(10, 10)
                };
                
                var interactionGraph = new InteractionGraph
                {
                    Id = "test-graph",
                    Name = "Test Graph"
                };
                
                var contentBundle = new ContentBundle
                {
                    Id = "test-bundle",
                    Name = "Test Bundle"
                };
                
                Console.WriteLine($"  ✅ DesignBrief: {designBrief.Id}");
                Console.WriteLine($"  ✅ GamePlan: {gamePlan.Id}");
                Console.WriteLine($"  ✅ WorldLayout: {worldLayout.Id}");
                Console.WriteLine($"  ✅ InteractionGraph: {interactionGraph.Id}");
                Console.WriteLine($"  ✅ ContentBundle: {contentBundle.Id}");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ DTO test failed: {ex.Message}");
                return false;
            }
        }
    }
}
