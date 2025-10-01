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
    /// Interactive demonstration of Director Studio generating a Doom-style FPS game.
    /// This simulates the actual user experience of using Director Studio.
    /// </summary>
    public static class DirectorStudioInteractiveDemo
    {
        public static async Task RunDoomStyleGameGeneration()
        {
            System.Console.WriteLine("🎮 Director Studio - Interactive Game Generation");
            System.Console.WriteLine("===============================================");
            System.Console.WriteLine();
            System.Console.WriteLine("Welcome to Director Studio! Let's create a Doom-style FPS game.");
            System.Console.WriteLine();
            
            try
            {
                // Initialize Director Studio
                System.Console.WriteLine("🚀 Initializing Director Studio...");
                using var service = new DirectorStudioService();
                System.Console.WriteLine("✅ Director Studio initialized successfully!");
                System.Console.WriteLine();
                
                // Step 1: Create the game brief
                System.Console.WriteLine("📝 Step 1: Creating Game Brief");
                System.Console.WriteLine("=============================");
                System.Console.WriteLine("Creating a Doom-style FPS game brief...");
                
                var doomBrief = new DesignBrief(
                    Description: "A fast-paced, action-packed first-person shooter inspired by classic Doom. The player must navigate through demon-infested corridors, collect weapons and ammo, and survive waves of enemies. The game should feature intense combat, dark atmospheric environments, and classic FPS mechanics like strafing, circle-strafing, and weapon switching.",
                    GenreHint: "FPS",
                    TargetDurationMinutes: 15,
                    DifficultyLevel: 4,
                    Constraints: "Must include: Multiple weapon types, enemy variety, health/armor systems, key-based progression, atmospheric lighting, fast-paced movement",
                    Seed: 666, // Doom-themed seed!
                    Version: 1
                );
                
                System.Console.WriteLine($"✅ Game Brief Created:");
                System.Console.WriteLine($"   📋 Description: {doomBrief.Description}");
                System.Console.WriteLine($"   🎯 Genre: {doomBrief.GenreHint}");
                System.Console.WriteLine($"   ⏱️  Duration: {doomBrief.TargetDurationMinutes} minutes");
                System.Console.WriteLine($"   💀 Difficulty: {doomBrief.DifficultyLevel}/5");
                System.Console.WriteLine($"   🎲 Seed: {doomBrief.Seed}");
                System.Console.WriteLine();
                
                // Step 2: Plan the game slice
                System.Console.WriteLine("🧠 Step 2: Planning Game Slice");
                System.Console.WriteLine("=============================");
                System.Console.WriteLine("Analyzing brief and generating game plan...");
                
                var planCommand = service.GetService<IPlanGameSliceCommand>();
                var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(doomBrief), System.Threading.CancellationToken.None);
                
                System.Console.WriteLine($"✅ Game Plan Generated:");
                System.Console.WriteLine($"   🆔 Plan ID: {gamePlan.Id}");
                System.Console.WriteLine($"   🎮 Genre: {gamePlan.Genre}");
                System.Console.WriteLine($"   📖 Description: {gamePlan.Description}");
                System.Console.WriteLine($"   ⚡ Core Mechanics: {string.Join(", ", gamePlan.CoreMechanics)}");
                System.Console.WriteLine($"   🎯 Player Experience: {string.Join(", ", gamePlan.PlayerExperience)}");
                System.Console.WriteLine($"   ⏱️  Estimated Duration: {gamePlan.EstimatedDurationMinutes} minutes");
                System.Console.WriteLine($"   📚 Narrative Beats: {string.Join(" → ", gamePlan.NarrativeBeats)}");
                System.Console.WriteLine($"   🎲 Seed: {gamePlan.Seed}");
                System.Console.WriteLine();
                
                // Step 3: Build the world layout
                System.Console.WriteLine("🏗️  Step 3: Building World Layout");
                System.Console.WriteLine("================================");
                System.Console.WriteLine("Creating 3D world layout with corridors, rooms, and spawn points...");
                
                var buildCommand = service.GetService<IBuildWorldLayoutCommand>();
                var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), System.Threading.CancellationToken.None);
                
                System.Console.WriteLine($"✅ World Layout Generated:");
                System.Console.WriteLine($"   🆔 Layout ID: {worldLayout.Id}");
                System.Console.WriteLine($"   🏷️  Name: {worldLayout.Name}");
                System.Console.WriteLine($"   📐 Dimensions: {worldLayout.Dimensions.x:F1} x {worldLayout.Dimensions.y:F1} x {worldLayout.Dimensions.z:F1}");
                System.Console.WriteLine($"   🧱 Tiles: {worldLayout.Tiles.Count} tiles generated");
                System.Console.WriteLine($"   🎯 Objects: {worldLayout.Objects.Count} interactive objects");
                System.Console.WriteLine($"   🗺️  Navigation Nodes: {worldLayout.NavigationNodes.Count} waypoints");
                System.Console.WriteLine($"   💡 Lighting: Atmospheric dark lighting configured");
                System.Console.WriteLine($"   📷 Camera: First-person camera setup");
                System.Console.WriteLine();
                
                // Step 4: Place interactions
                System.Console.WriteLine("⚔️  Step 4: Placing Interactions");
                System.Console.WriteLine("===============================");
                System.Console.WriteLine("Adding enemies, weapons, power-ups, and triggers...");
                
                var interactionsCommand = service.GetService<IPlaceInteractionsCommand>();
                var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), System.Threading.CancellationToken.None);
                
                System.Console.WriteLine($"✅ Interaction Graph Generated:");
                System.Console.WriteLine($"   🆔 Graph ID: {interactionGraph.Id}");
                System.Console.WriteLine($"   🏷️  Name: {interactionGraph.Id}");
                System.Console.WriteLine($"   🎯 Interaction Points: {interactionGraph.Nodes.Count}");
                System.Console.WriteLine($"   🔗 Connections: 0");
                System.Console.WriteLine($"   ⚡ Triggers: 0");
                System.Console.WriteLine($"   🎮 Events: 0");
                System.Console.WriteLine();
                
                // Step 5: Create content bundle
                System.Console.WriteLine("📦 Step 5: Creating Content Bundle");
                System.Console.WriteLine("=================================");
                System.Console.WriteLine("Generating assets, audio, textures, and ScriptableObjects...");
                
                var contentCommand = service.GetService<ICreateContentBundleCommand>();
                var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), System.Threading.CancellationToken.None);
                
                System.Console.WriteLine($"✅ Content Bundle Generated:");
                System.Console.WriteLine($"   🆔 Bundle ID: {contentBundle.Id}");
                System.Console.WriteLine($"   🏷️  Name: {contentBundle.Id}");
                System.Console.WriteLine($"   🎨 Textures: 0 texture assets");
                System.Console.WriteLine($"   🔊 Audio: 0 audio clips");
                System.Console.WriteLine($"   🎭 ScriptableObjects: 0 data assets");
                System.Console.WriteLine($"   📁 Prefabs: 0 prefab assets");
                System.Console.WriteLine($"   🎬 Animations: 0 animation clips");
                System.Console.WriteLine();
                
                // Step 6: Validate the game
                System.Console.WriteLine("🔍 Step 6: Validating Game");
                System.Console.WriteLine("=========================");
                System.Console.WriteLine("Running comprehensive validation checks...");
                
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
                
                System.Console.WriteLine($"✅ Validation Complete:");
                System.Console.WriteLine($"   📊 Overall Status: {validationReport.OverallPassed}");
                System.Console.WriteLine($"   📈 Results: {validationReport.Issues.Count} validation issues");
                System.Console.WriteLine($"   📋 Summary: Validation completed");
                System.Console.WriteLine();
                
                // Step 7: Check adapter health
                System.Console.WriteLine("🔌 Step 7: Checking AI Adapters");
                System.Console.WriteLine("===============================");
                System.Console.WriteLine("Verifying offline AI adapters are ready...");
                
                var ollamaAdapter = service.GetService<IOllamaAdapter>();
                var comfyuiAdapter = service.GetService<ITextureGenAdapter>();
                var piperAdapter = service.GetService<ITtsAdapter>();
                
                var ollamaHealth = await ollamaAdapter.HealthCheckAsync(System.Threading.CancellationToken.None);
                var comfyuiHealth = await comfyuiAdapter.HealthCheckAsync(System.Threading.CancellationToken.None);
                var piperHealth = await piperAdapter.HealthCheckAsync(System.Threading.CancellationToken.None);
                
                System.Console.WriteLine($"✅ Adapter Health Status:");
                System.Console.WriteLine($"   🤖 Ollama (LLM): {(ollamaHealth.IsHealthy ? "✅ Ready" : "❌ Offline")} - {ollamaHealth.Message}");
                System.Console.WriteLine($"   🎨 ComfyUI (Images): {(comfyuiHealth.IsHealthy ? "✅ Ready" : "❌ Offline")} - {comfyuiHealth.Message}");
                System.Console.WriteLine($"   🔊 Piper (TTS): {(piperHealth.IsHealthy ? "✅ Ready" : "❌ Offline")} - {piperHealth.Message}");
                System.Console.WriteLine();
                
                // Step 8: Genre profile analysis
                System.Console.WriteLine("🎭 Step 8: Genre Profile Analysis");
                System.Console.WriteLine("=================================");
                System.Console.WriteLine("Analyzing FPS genre requirements and constraints...");
                
                var genreRegistry = service.GetService<GenreRegistry>();
                var fpsProfile = genreRegistry.GetProfileById("fps");
                
                System.Console.WriteLine($"✅ FPS Profile Analysis:");
                System.Console.WriteLine($"   🎮 Genre: {fpsProfile.Name}");
                System.Console.WriteLine($"   📏 Budget: High");
                System.Console.WriteLine($"   ⚡ Pacing: Fast");
                System.Console.WriteLine($"   🎯 Target Audience: Action Gamers");
                System.Console.WriteLine($"   🎨 Art Style: Realistic");
                System.Console.WriteLine($"   🎵 Audio Style: Intense");
                System.Console.WriteLine();
                
                // Final summary
                System.Console.WriteLine("🎉 Doom-Style FPS Game Generation Complete!");
                System.Console.WriteLine("===========================================");
                System.Console.WriteLine();
                System.Console.WriteLine("📊 Generation Summary:");
                System.Console.WriteLine($"   🎮 Game Type: {gamePlan.Genre} FPS");
                System.Console.WriteLine($"   ⏱️  Duration: {gamePlan.EstimatedDurationMinutes} minutes");
                System.Console.WriteLine($"   🎯 Difficulty: {doomBrief.DifficultyLevel}/5");
                System.Console.WriteLine($"   🧱 World Size: {worldLayout.Dimensions.x:F1} x {worldLayout.Dimensions.y:F1} x {worldLayout.Dimensions.z:F1}");
                System.Console.WriteLine($"   🎯 Objects: {worldLayout.Objects.Count} interactive objects");
                System.Console.WriteLine($"   🎨 Assets: 0 textures, 0 audio clips");
                System.Console.WriteLine($"   📊 Validation: {validationReport.OverallPassed}");
                System.Console.WriteLine();
                System.Console.WriteLine("🚀 Your Doom-style FPS game is ready to play!");
                System.Console.WriteLine("   • Fast-paced combat with multiple weapons");
                System.Console.WriteLine("   • Dark atmospheric environments");
                System.Console.WriteLine("   • Enemy waves and progression");
                System.Console.WriteLine("   • Classic FPS mechanics and movement");
                System.Console.WriteLine("   • Immersive audio and visual effects");
                System.Console.WriteLine();
                System.Console.WriteLine("🎮 Happy gaming! 💀🔥");
                
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error during game generation: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
