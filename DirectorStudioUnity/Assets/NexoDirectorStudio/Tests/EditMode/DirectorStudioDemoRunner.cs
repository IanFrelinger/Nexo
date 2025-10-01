using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using NUnit.Framework;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Demo runner to test the Unity director pipeline functionality.
    /// This provides a comprehensive demonstration of the Director Studio capabilities.
    /// </summary>
    [TestFixture]
    public class DirectorStudioDemoRunner
    {
        private DirectorStudioService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioService();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        [Test]
        public async Task RunDirectorPipelineDemo()
        {
            System.Console.WriteLine("🎮 Director Studio Pipeline Demo");
            System.Console.WriteLine("================================");
            System.Console.WriteLine();

            try
            {
                // Step 1: Service Initialization
                System.Console.WriteLine("1️⃣ Initializing Director Studio Service...");
                Assert.IsNotNull(_service, "Service should be initialized");
                System.Console.WriteLine("   ✅ Service initialized successfully");
                System.Console.WriteLine();

                // Step 2: Command Availability
                System.Console.WriteLine("2️⃣ Checking Command Availability...");
                var planCommand = _service.GetService<IPlanGameSliceCommand>();
                var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
                var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
                var contentCommand = _service.GetService<ICreateContentBundleCommand>();
                
                Assert.IsNotNull(planCommand, "Plan command should be available");
                Assert.IsNotNull(buildCommand, "Build command should be available");
                Assert.IsNotNull(interactionsCommand, "Interactions command should be available");
                Assert.IsNotNull(contentCommand, "Content command should be available");
                System.Console.WriteLine("   ✅ All commands available");
                System.Console.WriteLine();

                // Step 3: Adapter Health Checks
                System.Console.WriteLine("3️⃣ Checking Adapter Health...");
                var ollamaAdapter = _service.GetService<IOllamaAdapter>();
                var comfyuiAdapter = _service.GetService<ITextureGenAdapter>();
                var piperAdapter = _service.GetService<ITtsAdapter>();
                
                var ollamaHealth = await ollamaAdapter.HealthCheckAsync(CancellationToken.None);
                var comfyuiHealth = await comfyuiAdapter.HealthCheckAsync(CancellationToken.None);
                var piperHealth = await piperAdapter.HealthCheckAsync(CancellationToken.None);
                
                System.Console.WriteLine($"   Ollama: {(ollamaHealth.IsHealthy ? "✅" : "❌")} {ollamaHealth.Message}");
                System.Console.WriteLine($"   ComfyUI: {(comfyuiHealth.IsHealthy ? "✅" : "❌")} {comfyuiHealth.Message}");
                System.Console.WriteLine($"   Piper: {(piperHealth.IsHealthy ? "✅" : "❌")} {piperHealth.Message}");
                System.Console.WriteLine();

                // Step 4: Create Design Brief
                System.Console.WriteLine("4️⃣ Creating Design Brief...");
                var designBrief = new DesignBrief(
                    Description: "A fast-paced platformer with jumping mechanics and collectible items",
                    GenreHint: "Platformer",
                    TargetDurationMinutes: 10,
                    DifficultyLevel: 2,
                    Seed: 12345
                );
                System.Console.WriteLine($"   Genre: {designBrief.GenreHint}");
                System.Console.WriteLine($"   Duration: {designBrief.TargetDurationMinutes} minutes");
                System.Console.WriteLine($"   Difficulty: {designBrief.DifficultyLevel}");
                System.Console.WriteLine("   ✅ Design brief created");
                System.Console.WriteLine();

                // Step 5: Plan Game Slice
                System.Console.WriteLine("5️⃣ Planning Game Slice...");
                var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
                
                Assert.IsNotNull(gamePlan, "Game plan should be created");
                Assert.IsNotNull(gamePlan.Id, "Game plan should have an ID");
                Assert.IsNotNull(gamePlan.Description, "Game plan should have a description");
                
                System.Console.WriteLine($"   Game Plan ID: {gamePlan.Id}");
                System.Console.WriteLine($"   Description: {gamePlan.Description}");
                System.Console.WriteLine($"   Core Mechanics: {string.Join(", ", gamePlan.CoreMechanics)}");
                System.Console.WriteLine($"   Required Assets: {gamePlan.RequiredAssets.Count}");
                System.Console.WriteLine("   ✅ Game slice planned");
                System.Console.WriteLine();

                // Step 6: Build World Layout
                System.Console.WriteLine("6️⃣ Building World Layout...");
                var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
                
                Assert.IsNotNull(worldLayout, "World layout should be created");
                Assert.IsNotNull(worldLayout.Id, "World layout should have an ID");
                Assert.IsTrue(worldLayout.Dimensions.x > 0 && worldLayout.Dimensions.y > 0, "World should have valid dimensions");
                
                System.Console.WriteLine($"   World Layout ID: {worldLayout.Id}");
                System.Console.WriteLine($"   Dimensions: {worldLayout.Dimensions.x}x{worldLayout.Dimensions.y}");
                System.Console.WriteLine($"   Tiles: {worldLayout.Tiles.Count}");
                System.Console.WriteLine("   ✅ World layout built");
                System.Console.WriteLine();

                // Step 7: Place Interactions
                System.Console.WriteLine("7️⃣ Placing Interactions...");
                var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), CancellationToken.None);
                
                Assert.IsNotNull(interactionGraph, "Interaction graph should be created");
                Assert.IsNotNull(interactionGraph.Id, "Interaction graph should have an ID");
                
                System.Console.WriteLine($"   Interaction Graph ID: {interactionGraph.Id}");
                System.Console.WriteLine($"   Nodes: {interactionGraph.Nodes.Count}");
                System.Console.WriteLine($"   Connections: {interactionGraph.Connections.Count}");
                System.Console.WriteLine("   ✅ Interactions placed");
                System.Console.WriteLine();

                // Step 8: Create Content Bundle
                System.Console.WriteLine("8️⃣ Creating Content Bundle...");
                var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), CancellationToken.None);
                
                Assert.IsNotNull(contentBundle, "Content bundle should be created");
                Assert.IsNotNull(contentBundle.Id, "Content bundle should have an ID");
                
                System.Console.WriteLine($"   Content Bundle ID: {contentBundle.Id}");
                System.Console.WriteLine($"   Assets: {contentBundle.Assets.Count}");
                System.Console.WriteLine($"   Addressables Group: {contentBundle.AddressablesGroup.GroupName}");
                System.Console.WriteLine($"   Bundle Version: {contentBundle.Metadata.Version}");
                System.Console.WriteLine("   ✅ Content bundle created");
                System.Console.WriteLine();

                // Step 9: Validation
                System.Console.WriteLine("9️⃣ Running Validation...");
                var validators = _service.GetService<IEnumerable<IValidator<GamePlan>>>();
                Assert.IsNotNull(validators, "Validators should be available");
                
                var validationResults = new List<ValidationResult>();
                foreach (var validator in validators)
                {
                    var result = await validator.ValidateAsync(gamePlan, CancellationToken.None);
                    validationResults.Add(result);
                }
                
                var passedValidations = validationResults.Count(r => r.Score > 0);
                System.Console.WriteLine($"   Validators: {validationResults.Count}");
                System.Console.WriteLine($"   Passed: {passedValidations}");
                System.Console.WriteLine("   ✅ Validation completed");
                System.Console.WriteLine();

                // Step 10: Summary
                System.Console.WriteLine("🎉 Director Studio Pipeline Demo Complete!");
                System.Console.WriteLine("==========================================");
                System.Console.WriteLine($"✅ All {10} steps completed successfully");
                System.Console.WriteLine($"✅ Game Plan: {gamePlan.Id}");
                System.Console.WriteLine($"✅ World Layout: {worldLayout.Id}");
                System.Console.WriteLine($"✅ Interaction Graph: {interactionGraph.Id}");
                System.Console.WriteLine($"✅ Content Bundle: {contentBundle.Id}");
                System.Console.WriteLine();
                System.Console.WriteLine("The Director Studio pipeline is working correctly! 🚀");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Demo failed: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
