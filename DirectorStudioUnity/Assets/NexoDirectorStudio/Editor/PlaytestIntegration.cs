using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Agents;
using NexoDirectorStudio.Interactions;

namespace NexoDirectorStudio.Editor
{
    /// <summary>
    /// Simple playtesting integration for DirectorCliRunner
    /// </summary>
    public static class PlaytestIntegration
    {
        // Usage: -executeMethod NexoDirectorStudio.Editor.PlaytestIntegration.RunPlaytest --prompt "FPS game" --testDuration 15
        public static void RunPlaytest()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                var prompt = ReadArg(args, "--prompt") ?? ReadArg(args, "-prompt") ?? "FPS shooter with enemies and power-ups";
                var testDuration = float.TryParse(ReadArg(args, "--testDuration") ?? ReadArg(args, "-testDuration"), out var td) ? td : 15f;
                var resultsPath = ReadArg(args, "--results") ?? ReadArg(args, "-results") ?? Path.GetFullPath("playtest-integration-results.json");

                System.Console.WriteLine("=== Playtest Integration ===");
                System.Console.WriteLine($"Prompt: {prompt}");
                System.Console.WriteLine($"Test Duration: {testDuration}s");
                System.Console.WriteLine();

                // 1. Generate game content using existing DirectorCliRunner
                System.Console.WriteLine("🎬 Phase 1: Generating game content...");
                using var service = new DirectorStudioService();
                var designBrief = DirectorCliRunner.PromptToBriefAsync(service, prompt, CancellationToken.None).Result;
                
                var plan = new PlanGameSliceCommand().ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None).Result;
                var world = new BuildWorldLayoutCommand().ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None).Result;
                var interactions = new PlaceInteractionsCommand().ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None).Result;
                var bundle = new CreateContentBundleCommand().ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None).Result;
                
                System.Console.WriteLine($"✓ Generated: {plan.CoreMechanics.Count} mechanics, {world.Tiles.Count} tiles, {interactions.Nodes.Count} interactions");

                // 2. Create playtest scene
                System.Console.WriteLine("🎮 Phase 2: Creating playtest scene...");
                var scenePath = CreatePlaytestScene(plan, world);
                System.Console.WriteLine($"✓ Scene created: {scenePath}");

                // 3. Run playtest
                System.Console.WriteLine($"🤖 Phase 3: Running AI playtest for {testDuration}s...");
                var metrics = RunPlaytestSync(scenePath, testDuration);
                System.Console.WriteLine($"✓ Playtest completed: {metrics.interactionTriggerCount} interactions triggered");

                // 4. Save results
                System.Console.WriteLine("💾 Phase 4: Saving results...");
                SaveResults(metrics, prompt, testDuration, resultsPath);
                System.Console.WriteLine($"✓ Results saved: {resultsPath}");

                System.Console.WriteLine("✅ Playtest integration completed successfully!");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Playtest integration failed: {ex.Message}");
                EditorApplication.Exit(1);
            }
        }
        
        private static string CreatePlaytestScene(GamePlan plan, WorldLayout world)
        {
            // Create a new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Create basic FPS game structure
            var gameGO = new GameObject("FPSGame");
            var game = gameGO.AddComponent<NexoDirectorStudio.Game.DoomFPSGame>();
            game.gamePlan = plan;
            game.worldLayout = world;
            
            // Create player with autoplayer
            var player = new GameObject("Player");
            player.tag = "Player";
            player.AddComponent<CharacterController>();
            player.AddComponent<AIAutoplayer>();
            player.transform.position = new Vector3(0, 1, 0);
            
            // Create camera
            var camera = new GameObject("Main Camera");
            camera.tag = "MainCamera";
            camera.AddComponent<Camera>();
            camera.transform.SetParent(player.transform);
            camera.transform.localPosition = new Vector3(0, 1.6f, 0);
            
            // Create InteractionBus
            var busGO = new GameObject("InteractionBus");
            var bus = busGO.AddComponent<InteractionBus>();
            
            // Create test interactions
            CreateTestInteractions(bus);
            
            // Save scene
            var scenePath = $"Assets/GeneratedScenes/Playtest_{plan.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.unity";
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            EditorSceneManager.SaveScene(scene, scenePath);
            
            return scenePath;
        }
        
        private static void CreateTestInteractions(InteractionBus bus)
        {
            // Create power-ups
            for (int i = 0; i < 3; i++)
            {
                var powerUp = new GameObject($"PowerUp_{i}");
                powerUp.tag = "PowerUp";
                powerUp.transform.position = new Vector3(i * 5, 1, 5);
                
                var collider = powerUp.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                
                var interaction = powerUp.AddComponent<SimpleTestInteraction>();
                bus.Register(interaction);
            }
            
            // Create enemies
            for (int i = 0; i < 2; i++)
            {
                var enemy = new GameObject($"Enemy_{i}");
                enemy.tag = "Enemy";
                enemy.transform.position = new Vector3(i * 8, 1, 8);
                
                var collider = enemy.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                
                var interaction = enemy.AddComponent<SimpleTestInteraction>();
                bus.Register(interaction);
            }
        }
        
        private static PlaytestMetrics RunPlaytestSync(string scenePath, float testDuration)
        {
            // Load the scene
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new Exception($"Failed to load scene: {scenePath}");
            }
            
            // Create playtest agent
            var agentGO = new GameObject("SimplePlaytestAgent");
            var agent = agentGO.AddComponent<SimplePlaytestAgent>();
            agent.testDuration = testDuration;
            agent.enableMetrics = true;
            
            // Set up completion handler
            PlaytestMetrics result = null;
            var completed = false;
            
            agent.OnPlaytestComplete += (metrics) =>
            {
                result = metrics;
                completed = true;
            };
            
            // Wait for playtest completion synchronously
            var startTime = Time.time;
            while (!completed && (Time.time - startTime) < testDuration + 10)
            {
                System.Threading.Thread.Sleep(16);
            }
            
            // Cleanup
            if (agentGO != null)
            {
                UnityEngine.Object.DestroyImmediate(agentGO);
            }
            
            return result ?? new PlaytestMetrics { playtestCompleted = false };
        }
        
        private static void SaveResults(PlaytestMetrics metrics, string prompt, float testDuration, string resultsPath)
        {
            var result = new
            {
                prompt = prompt,
                testDuration = testDuration,
                success = metrics.playtestCompleted,
                metrics = new
                {
                    playtestDuration = metrics.totalPlaytestDuration,
                    averageFPS = metrics.averageFPS,
                    maxDistanceTraveled = metrics.maxDistanceTraveled,
                    totalInteractionsFound = metrics.totalInteractionsFound,
                    interactionTriggerCount = metrics.interactionTriggerCount,
                    interactionSuccessRate = metrics.interactionSuccessRate
                },
                timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
            
            var json = JsonUtility.ToJson(result, true);
            Directory.CreateDirectory(Path.GetDirectoryName(resultsPath));
            File.WriteAllText(resultsPath, json);
        }
        
        private static string ReadArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }
    }
}
