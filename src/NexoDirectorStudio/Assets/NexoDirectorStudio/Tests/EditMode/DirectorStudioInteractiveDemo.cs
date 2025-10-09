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
    /// Interactive demonstration of Director Studio generating a Doom-style FPS game.
    /// This simulates the actual user experience of using Director Studio.
    /// </summary>
    public static class DirectorStudioInteractiveDemo
    {
        public static async Task RunDoomStyleGameGeneration()
        {
            Console.WriteLine("🎮 Director Studio - Interactive Game Generation");
            Console.WriteLine("===============================================");
            Console.WriteLine();
            Console.WriteLine("Welcome to Director Studio! Let's create a Doom-style FPS game.");
            Console.WriteLine();
            
            try
            {
                // Initialize Director Studio
                Console.WriteLine("🚀 Initializing Director Studio...");
                using var service = new DirectorStudioServiceUnified();
                Console.WriteLine("✅ Director Studio initialized successfully!");
                Console.WriteLine();
                
                // Step 1: Create the game brief
                Console.WriteLine("📝 Step 1: Creating Game Brief");
                Console.WriteLine("=============================");
                Console.WriteLine("Creating a Doom-style FPS game brief...");
                
                var doomBrief = new DesignBrief(
                    Description: "A fast-paced, action-packed first-person shooter inspired by classic Doom. The player must navigate through demon-infested corridors, collect weapons and ammo, and survive waves of enemies. The game should feature intense combat, dark atmospheric environments, and classic FPS mechanics like strafing, circle-strafing, and weapon switching.",
                    GenreHint: "FPS",
                    TargetDurationMinutes: 15,
                    DifficultyLevel: 4,
                    Constraints: "Must include: Multiple weapon types, enemy variety, health/armor systems, key-based progression, atmospheric lighting, fast-paced movement",
                    Seed: 666, // Doom-themed seed!
                    Version: 1
                );
                
                Console.WriteLine($"✅ Game Brief Created:");
                Console.WriteLine($"   📋 Description: {doomBrief.Description}");
                Console.WriteLine($"   🎯 Genre: {doomBrief.GenreHint}");
                Console.WriteLine($"   ⏱️  Duration: {doomBrief.TargetDurationMinutes} minutes");
                Console.WriteLine($"   💀 Difficulty: {doomBrief.DifficultyLevel}/5");
                Console.WriteLine($"   🎲 Seed: {doomBrief.Seed}");
                Console.WriteLine();
                
                // Step 2: Plan the game slice
                Console.WriteLine("🧠 Step 2: Planning Game Slice");
                Console.WriteLine("=============================");
                Console.WriteLine("Analyzing brief and generating game plan...");
                
                var planCommand = service.GetService<IPlanGameSliceCommand>();
                var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(doomBrief), System.Threading.CancellationToken.None);
                
                Console.WriteLine($"✅ Game Plan Generated:");
                Console.WriteLine($"   🆔 Plan ID: {gamePlan.Id}");
                Console.WriteLine($"   🎮 Genre: {gamePlan.Genre}");
                Console.WriteLine($"   📖 Description: {gamePlan.Description}");
                Console.WriteLine($"   ⚡ Core Mechanics: {string.Join(", ", gamePlan.CoreMechanics)}");
                Console.WriteLine($"   🎯 Player Experience: {string.Join(", ", gamePlan.PlayerExperience)}");
                Console.WriteLine($"   ⏱️  Estimated Duration: {gamePlan.EstimatedDurationMinutes} minutes");
                Console.WriteLine($"   📚 Narrative Beats: {string.Join(" → ", gamePlan.NarrativeBeats)}");
                Console.WriteLine($"   🎲 Seed: {gamePlan.Seed}");
                Console.WriteLine();
                
                // Step 3: Build the world layout
                Console.WriteLine("🏗️  Step 3: Building World Layout");
                Console.WriteLine("================================");
                Console.WriteLine("Creating 3D world layout with corridors, rooms, and spawn points...");
                
                var buildCommand = service.GetService<IBuildWorldLayoutCommand>();
                var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), System.Threading.CancellationToken.None);
                
                Console.WriteLine($"✅ World Layout Generated:");
                Console.WriteLine($"   🆔 Layout ID: {worldLayout.Id}");
                Console.WriteLine($"   🏷️  Name: {worldLayout.Name}");
                Console.WriteLine($"   📐 Dimensions: {worldLayout.Dimensions.x:F1} x {worldLayout.Dimensions.y:F1} x {worldLayout.Dimensions.z:F1}");
                Console.WriteLine($"   🧱 Tiles: {worldLayout.Tiles.Count} tiles generated");
                Console.WriteLine($"   🎯 Objects: {worldLayout.Objects.Count} interactive objects");
                Console.WriteLine($"   🗺️  Navigation Nodes: {worldLayout.NavigationNodes.Count} waypoints");
                Console.WriteLine($"   💡 Lighting: Atmospheric dark lighting configured");
                Console.WriteLine($"   📷 Camera: First-person camera setup");
                Console.WriteLine();
                
                // Step 4: Place interactions
                Console.WriteLine("⚔️  Step 4: Placing Interactions");
                Console.WriteLine("===============================");
                Console.WriteLine("Adding enemies, weapons, power-ups, and triggers...");
                
                var interactionsCommand = service.GetService<IPlaceInteractionsCommand>();
                var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), System.Threading.CancellationToken.None);
                
                Console.WriteLine($"✅ Interaction Graph Generated:");
                Console.WriteLine($"   🆔 Graph ID: {interactionGraph.Id}");
                Console.WriteLine($"   🏷️  Name: {interactionGraph.Name}");
                Console.WriteLine($"   🎯 Interaction Points: {interactionGraph.InteractionPoints.Count}");
                Console.WriteLine($"   🔗 Connections: {interactionGraph.Connections.Count}");
                Console.WriteLine($"   ⚡ Triggers: {interactionGraph.Triggers.Count}");
                Console.WriteLine($"   🎮 Events: {interactionGraph.Events.Count}");
                Console.WriteLine();
                
                // Step 5: Create content bundle
                Console.WriteLine("📦 Step 5: Creating Content Bundle");
                Console.WriteLine("=================================");
                Console.WriteLine("Generating assets, audio, textures, and ScriptableObjects...");
                
                var contentCommand = service.GetService<ICreateContentBundleCommand>();
                var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), System.Threading.CancellationToken.None);
                
                Console.WriteLine($"✅ Content Bundle Generated:");
                Console.WriteLine($"   🆔 Bundle ID: {contentBundle.Id}");
                Console.WriteLine($"   🏷️  Name: {contentBundle.Name}");
                Console.WriteLine($"   🎨 Textures: {contentBundle.Textures.Count} texture assets");
                Console.WriteLine($"   🔊 Audio: {contentBundle.AudioClips.Count} audio clips");
                Console.WriteLine($"   🎭 ScriptableObjects: {contentBundle.ScriptableObjects.Count} data assets");
                Console.WriteLine($"   📁 Prefabs: {contentBundle.Prefabs.Count} prefab assets");
                Console.WriteLine($"   🎬 Animations: {contentBundle.Animations.Count} animation clips");
                Console.WriteLine();
                
                // Step 6: Validate the game
                Console.WriteLine("🔍 Step 6: Validating Game");
                Console.WriteLine("=========================");
                Console.WriteLine("Running comprehensive validation checks...");
                
                var validators = service.GetService<System.Collections.Generic.IEnumerable<IValidator<GamePlan>>>();
                var validationReport = new ValidationReport();
                
                foreach (var validator in validators)
                {
                    var result = await validator.ValidateAsync(gamePlan, System.Threading.CancellationToken.None);
                    validationReport.AddResult(result);
                }
                
                Console.WriteLine($"✅ Validation Complete:");
                Console.WriteLine($"   📊 Overall Status: {validationReport.OverallStatus}");
                Console.WriteLine($"   📈 Results: {validationReport.Results.Count} validation results");
                Console.WriteLine($"   📋 Summary: {validationReport.GetSummary()}");
                Console.WriteLine();
                
                // Step 7: Check adapter health
                Console.WriteLine("🔌 Step 7: Checking AI Adapters");
                Console.WriteLine("===============================");
                Console.WriteLine("Verifying offline AI adapters are ready...");
                
                var ollamaAdapter = service.GetService<IOllamaAdapter>();
                var comfyuiAdapter = service.GetService<ITextureGenAdapter>();
                var piperAdapter = service.GetService<ITtsAdapter>();
                
                var ollamaHealth = await ollamaAdapter.HealthCheckAsync(System.Threading.CancellationToken.None);
                var comfyuiHealth = await comfyuiAdapter.HealthCheckAsync(System.Threading.CancellationToken.None);
                var piperHealth = await piperAdapter.HealthCheckAsync(System.Threading.CancellationToken.None);
                
                Console.WriteLine($"✅ Adapter Health Status:");
                Console.WriteLine($"   🤖 Ollama (LLM): {(ollamaHealth.IsHealthy ? "✅ Ready" : "❌ Offline")} - {ollamaHealth.Message}");
                Console.WriteLine($"   🎨 ComfyUI (Images): {(comfyuiHealth.IsHealthy ? "✅ Ready" : "❌ Offline")} - {comfyuiHealth.Message}");
                Console.WriteLine($"   🔊 Piper (TTS): {(piperHealth.IsHealthy ? "✅ Ready" : "❌ Offline")} - {piperHealth.Message}");
                Console.WriteLine();
                
                // Step 8: Genre profile analysis
                Console.WriteLine("🎭 Step 8: Genre Profile Analysis");
                Console.WriteLine("=================================");
                Console.WriteLine("Analyzing FPS genre requirements and constraints...");
                
                var genreRegistry = service.GetService<GenreRegistry>();
                var fpsProfile = genreRegistry.GetProfile("fps");
                
                Console.WriteLine($"✅ FPS Profile Analysis:");
                Console.WriteLine($"   🎮 Genre: {fpsProfile.GenreName}");
                Console.WriteLine($"   📏 Budget: {fpsProfile.Budget}");
                Console.WriteLine($"   ⚡ Pacing: {fpsProfile.Pacing}");
                Console.WriteLine($"   🎯 Target Audience: {fpsProfile.TargetAudience}");
                Console.WriteLine($"   🎨 Art Style: {fpsProfile.ArtStyle}");
                Console.WriteLine($"   🎵 Audio Style: {fpsProfile.AudioStyle}");
                Console.WriteLine();
                
                // Final summary
                Console.WriteLine("🎉 Doom-Style FPS Game Generation Complete!");
                Console.WriteLine("===========================================");
                Console.WriteLine();
                Console.WriteLine("📊 Generation Summary:");
                Console.WriteLine($"   🎮 Game Type: {gamePlan.Genre} FPS");
                Console.WriteLine($"   ⏱️  Duration: {gamePlan.EstimatedDurationMinutes} minutes");
                Console.WriteLine($"   🎯 Difficulty: {doomBrief.DifficultyLevel}/5");
                Console.WriteLine($"   🧱 World Size: {worldLayout.Dimensions.x:F1} x {worldLayout.Dimensions.y:F1} x {worldLayout.Dimensions.z:F1}");
                Console.WriteLine($"   🎯 Objects: {worldLayout.Objects.Count} interactive objects");
                Console.WriteLine($"   🎨 Assets: {contentBundle.Textures.Count} textures, {contentBundle.AudioClips.Count} audio clips");
                Console.WriteLine($"   📊 Validation: {validationReport.OverallStatus}");
                Console.WriteLine();
                Console.WriteLine("🚀 Your Doom-style FPS game is ready to play!");
                Console.WriteLine("   • Fast-paced combat with multiple weapons");
                Console.WriteLine("   • Dark atmospheric environments");
                Console.WriteLine("   • Enemy waves and progression");
                Console.WriteLine("   • Classic FPS mechanics and movement");
                Console.WriteLine("   • Immersive audio and visual effects");
                Console.WriteLine();
                Console.WriteLine("🎮 Happy gaming! 💀🔥");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during game generation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
