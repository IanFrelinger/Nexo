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
    /// Simple demo to test the Unity director pipeline functionality.
    /// This provides a basic demonstration of the Director Studio capabilities.
    /// </summary>
    [TestFixture]
    public class SimpleDirectorDemo
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
        public void DirectorStudio_ShouldInitialize()
        {
            System.Console.WriteLine("🎮 Director Studio - Simple Demo");
            System.Console.WriteLine("===============================");
            System.Console.WriteLine();

            // Test service initialization
            Assert.IsNotNull(_service, "Service should be initialized");
            System.Console.WriteLine("✅ Service initialized successfully");
            System.Console.WriteLine();

            // Test command availability
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
            var contentCommand = _service.GetService<ICreateContentBundleCommand>();
            
            Assert.IsNotNull(planCommand, "Plan command should be available");
            Assert.IsNotNull(buildCommand, "Build command should be available");
            Assert.IsNotNull(interactionsCommand, "Interactions command should be available");
            Assert.IsNotNull(contentCommand, "Content command should be available");
            System.Console.WriteLine("✅ All commands available");
            System.Console.WriteLine();

            // Test adapter availability
            var ollamaAdapter = _service.GetService<IOllamaAdapter>();
            var comfyuiAdapter = _service.GetService<ITextureGenAdapter>();
            var piperAdapter = _service.GetService<ITtsAdapter>();
            
            Assert.IsNotNull(ollamaAdapter, "Ollama adapter should be available");
            Assert.IsNotNull(comfyuiAdapter, "ComfyUI adapter should be available");
            Assert.IsNotNull(piperAdapter, "Piper adapter should be available");
            System.Console.WriteLine("✅ All adapters available");
            System.Console.WriteLine();

            // Test validator availability
            var validators = _service.GetService<IEnumerable<IValidator<GamePlan>>>();
            Assert.IsNotNull(validators, "Validators should be available");
            System.Console.WriteLine($"✅ {validators.Count()} validators available");
            System.Console.WriteLine();

            System.Console.WriteLine("🎉 Director Studio is ready for use! 🚀");
            System.Console.WriteLine();
        }

        [Test]
        public async Task DirectorStudio_ShouldCreateGamePlan()
        {
            System.Console.WriteLine("🎮 Director Studio - Game Plan Creation Demo");
            System.Console.WriteLine("=============================================");
            System.Console.WriteLine();

            // Create a design brief
            var designBrief = new DesignBrief(
                Description: "A simple platformer game with jumping mechanics",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 1,
                Seed: 12345
            );

            System.Console.WriteLine($"Creating game plan for: {designBrief.Description}");
            System.Console.WriteLine($"Genre: {designBrief.GenreHint}");
            System.Console.WriteLine($"Duration: {designBrief.TargetDurationMinutes} minutes");
            System.Console.WriteLine($"Difficulty: {designBrief.DifficultyLevel}");
            System.Console.WriteLine();

            // Plan the game slice
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);

            // Verify the game plan
            Assert.IsNotNull(gamePlan, "Game plan should be created");
            Assert.IsNotNull(gamePlan.Id, "Game plan should have an ID");
            Assert.IsNotNull(gamePlan.Description, "Game plan should have a description");

            System.Console.WriteLine("✅ Game plan created successfully!");
            System.Console.WriteLine($"   ID: {gamePlan.Id}");
            System.Console.WriteLine($"   Description: {gamePlan.Description}");
            System.Console.WriteLine($"   Genre: {gamePlan.Genre}");
            System.Console.WriteLine($"   Core Mechanics: {string.Join(", ", gamePlan.CoreMechanics)}");
            System.Console.WriteLine($"   Required Assets: {gamePlan.RequiredAssets.Count}");
            System.Console.WriteLine();

            System.Console.WriteLine("🎉 Game plan creation demo complete! 🚀");
            System.Console.WriteLine();
        }

        [Test]
        public async Task DirectorStudio_ShouldRunFullPipeline()
        {
            System.Console.WriteLine("🎮 Director Studio - Full Pipeline Demo");
            System.Console.WriteLine("=======================================");
            System.Console.WriteLine();

            try
            {
                // Step 1: Create Design Brief
                System.Console.WriteLine("1️⃣ Creating Design Brief...");
                var designBrief = new DesignBrief(
                    Description: "A fast-paced action game with combat and exploration",
                    GenreHint: "Action",
                    TargetDurationMinutes: 8,
                    DifficultyLevel: 2,
                    Seed: 54321
                );
                System.Console.WriteLine($"   Genre: {designBrief.GenreHint}");
                System.Console.WriteLine($"   Duration: {designBrief.TargetDurationMinutes} minutes");
                System.Console.WriteLine("   ✅ Design brief created");
                System.Console.WriteLine();

                // Step 2: Plan Game Slice
                System.Console.WriteLine("2️⃣ Planning Game Slice...");
                var planCommand = _service.GetService<IPlanGameSliceCommand>();
                var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
                
                Assert.IsNotNull(gamePlan, "Game plan should be created");
                System.Console.WriteLine($"   Game Plan ID: {gamePlan.Id}");
                System.Console.WriteLine($"   Description: {gamePlan.Description}");
                System.Console.WriteLine("   ✅ Game slice planned");
                System.Console.WriteLine();

                // Step 3: Build World Layout
                System.Console.WriteLine("3️⃣ Building World Layout...");
                var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
                var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
                
                Assert.IsNotNull(worldLayout, "World layout should be created");
                System.Console.WriteLine($"   World Layout ID: {worldLayout.Id}");
                System.Console.WriteLine($"   Dimensions: {worldLayout.Dimensions.x}x{worldLayout.Dimensions.y}");
                System.Console.WriteLine($"   Tiles: {worldLayout.Tiles.Count}");
                System.Console.WriteLine("   ✅ World layout built");
                System.Console.WriteLine();

                // Step 4: Place Interactions
                System.Console.WriteLine("4️⃣ Placing Interactions...");
                var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
                var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), CancellationToken.None);
                
                Assert.IsNotNull(interactionGraph, "Interaction graph should be created");
                System.Console.WriteLine($"   Interaction Graph ID: {interactionGraph.Id}");
                System.Console.WriteLine($"   Nodes: {interactionGraph.Nodes.Count}");
                System.Console.WriteLine($"   Connections: {interactionGraph.Connections.Count}");
                System.Console.WriteLine("   ✅ Interactions placed");
                System.Console.WriteLine();

                // Step 5: Create Content Bundle
                System.Console.WriteLine("5️⃣ Creating Content Bundle...");
                var contentCommand = _service.GetService<ICreateContentBundleCommand>();
                var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), CancellationToken.None);
                
                Assert.IsNotNull(contentBundle, "Content bundle should be created");
                System.Console.WriteLine($"   Content Bundle ID: {contentBundle.Id}");
                System.Console.WriteLine($"   Assets: {contentBundle.Assets.Count}");
                System.Console.WriteLine($"   Addressables Group: {contentBundle.AddressablesGroup.GroupName}");
                System.Console.WriteLine("   ✅ Content bundle created");
                System.Console.WriteLine();

                // Step 6: Run Validation
                System.Console.WriteLine("6️⃣ Running Validation...");
                var validators = _service.GetService<IEnumerable<IValidator<GamePlan>>>();
                var validationResults = new List<NexoDirectorStudio.Validators.ValidationResult>();
                
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

                // Summary
                System.Console.WriteLine("🎉 Full Pipeline Demo Complete!");
                System.Console.WriteLine("===============================");
                System.Console.WriteLine($"✅ All 6 steps completed successfully");
                System.Console.WriteLine($"✅ Game Plan: {gamePlan.Id}");
                System.Console.WriteLine($"✅ World Layout: {worldLayout.Id}");
                System.Console.WriteLine($"✅ Interaction Graph: {interactionGraph.Id}");
                System.Console.WriteLine($"✅ Content Bundle: {contentBundle.Id}");
                System.Console.WriteLine();
                System.Console.WriteLine("The Director Studio pipeline is working correctly! 🚀");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Pipeline demo failed: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
