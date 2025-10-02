using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Agents;
using NexoDirectorStudio.Interactions;

namespace NexoDirectorStudio.Editor
{
    public static class DirectorCliRunner
    {
        // Usage: -executeMethod NexoDirectorStudio.Editor.DirectorCliRunner.Run --prompt "Make a cozy farming sim"
        public static async void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                var prompt = ReadArg(args, "--prompt") ?? ReadArg(args, "-prompt") ?? "Create a simple Adventure game with exploration";

                System.Console.WriteLine("=== Director CLI Runner ===");
                System.Console.WriteLine($"Prompt: {prompt}");
                System.Console.WriteLine();

                using var service = new DirectorStudioService();

                // Phase 1: Service Initialization
                System.Console.WriteLine("🔧 Phase 1: Service Initialization");
                System.Console.WriteLine("  ✓ Initializing Director Studio Service...");
                System.Console.WriteLine("  ✓ Service initialized successfully");
                System.Console.WriteLine();

                // Phase 2: Prompt Processing
                System.Console.WriteLine("📝 Phase 2: Prompt Processing");
                System.Console.WriteLine("  ✓ Parsing natural language prompt...");
                var designBrief = await PromptToBriefAsync(service, prompt, CancellationToken.None);
                System.Console.WriteLine($"  ✓ Brief created: {designBrief.Description}");
                System.Console.WriteLine($"  ✓ Genre: {designBrief.GenreHint}, Duration: {designBrief.TargetDurationMinutes}min, Difficulty: {designBrief.DifficultyLevel}");
                System.Console.WriteLine();

                // Phase 3: Game Planning
                System.Console.WriteLine("🎯 Phase 3: Game Planning");
                System.Console.WriteLine("  ✓ Generating game mechanics and structure...");
                var planCommand = service.GetService<IPlanGameSliceCommand>();
                var plan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
                System.Console.WriteLine($"  ✓ Plan created: {plan.CoreMechanics.Count} mechanics, {plan.EstimatedDurationMinutes}min duration");
                System.Console.WriteLine($"  ✓ Mechanics: {string.Join(", ", plan.CoreMechanics.Take(3))}...");
                System.Console.WriteLine();

                // Phase 4: World Layout
                System.Console.WriteLine("🏗️ Phase 4: World Layout");
                System.Console.WriteLine("  ✓ Generating spatial layout and navigation...");
                var layoutCommand = service.GetService<IBuildWorldLayoutCommand>();
                var world = await layoutCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);
                System.Console.WriteLine($"  ✓ World created: {world.Tiles.Count} tiles, {world.Objects.Count} objects");
                System.Console.WriteLine($"  ✓ Dimensions: {world.Dimensions.x:F1}x{world.Dimensions.y:F1}x{world.Dimensions.z:F1}");
                System.Console.WriteLine();

                // Phase 5: Interaction Placement
                System.Console.WriteLine("🎮 Phase 5: Interaction Placement");
                System.Console.WriteLine("  ✓ Placing interactive elements and triggers...");
                var placementCommand = service.GetService<IPlaceInteractionsCommand>();
                var interactions = await placementCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);
                System.Console.WriteLine($"  ✓ Interactions placed: {interactions.Nodes.Count} nodes, {interactions.Connections.Count} connections");
                System.Console.WriteLine($"  ✓ Entry points: {interactions.EntryPointIds.Count}");
                System.Console.WriteLine();

                // Phase 6: Content Bundle Creation
                System.Console.WriteLine("📦 Phase 6: Content Bundle Creation");
                System.Console.WriteLine("  ✓ Generating assets and content...");
                var contentCommand = service.GetService<ICreateContentBundleCommand>();
                var bundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);
                System.Console.WriteLine($"  ✓ Bundle created: {bundle.Assets.Count} assets");
                System.Console.WriteLine($"  ✓ Addressables group: {bundle.AddressablesGroup.GroupName}");
                System.Console.WriteLine();

                // Phase 7: Validation
                System.Console.WriteLine("✅ Phase 7: Validation");
                System.Console.WriteLine("  ✓ Running validation checks...");
                var validator = service.GetService<IValidator<ContentBundle>>();
                var validation = await validator.ValidateAsync(bundle, CancellationToken.None);
                System.Console.WriteLine($"  ✓ Validation complete: {(validation.IsValid ? "PASSED" : "FAILED")} (Score: {validation.Score}/100)");
                if (!validation.IsValid)
                {
                    System.Console.WriteLine($"  ⚠️ Issues found: {validation.Issues.Count}");
                    foreach (var issue in validation.Issues.Take(3))
                    {
                        System.Console.WriteLine($"    - {issue.Title}: {issue.Description}");
                    }
                }
                System.Console.WriteLine();

                // Phase 8: Scene Generation
                System.Console.WriteLine("🎬 Phase 8: Scene Generation");
                System.Console.WriteLine("  ✓ Creating Unity scene...");
                var scenePath = await CreateSceneAsync(plan, world, interactions, bundle);
                System.Console.WriteLine($"  ✓ Scene created: {scenePath}");
                System.Console.WriteLine();

                // Phase 9: Playtesting Setup
                System.Console.WriteLine("🤖 Phase 9: Playtesting Setup");
                System.Console.WriteLine("  ✓ Setting up AI playtesting...");
                await SetupPlaytestingAsync(scenePath, plan);
                System.Console.WriteLine("  ✓ Playtesting configured");
                System.Console.WriteLine();

                System.Console.WriteLine("🎉 Director CLI Runner completed successfully!");
                System.Console.WriteLine($"📁 Generated scene: {scenePath}");
                System.Console.WriteLine($"📊 Validation score: {validation.Score}/100");
                System.Console.WriteLine($"🎮 Ready for playtesting!");

                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Director CLI Runner failed: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                EditorApplication.Exit(1);
            }
        }

        public static async Task<DesignBrief> PromptToBriefAsync(DirectorStudioService service, string prompt, CancellationToken cancellationToken)
        {
            // Simple prompt-to-brief conversion for CLI
            var brief = new DesignBrief(
                Description: prompt,
                GenreHint: ExtractGenreHint(prompt),
                TargetDurationMinutes: ExtractDuration(prompt),
                DifficultyLevel: ExtractDifficulty(prompt),
                Seed: Environment.TickCount
            );
            
            return brief;
        }

        private static string ExtractGenreHint(string prompt)
        {
            var lower = prompt.ToLower();
            if (lower.Contains("fps") || lower.Contains("shooter") || lower.Contains("gun")) return "fps";
            if (lower.Contains("rpg") || lower.Contains("role") || lower.Contains("character")) return "rpg";
            if (lower.Contains("platform") || lower.Contains("jump") || lower.Contains("mario")) return "platformer";
            if (lower.Contains("puzzle") || lower.Contains("solve") || lower.Contains("brain")) return "puzzle";
            if (lower.Contains("racing") || lower.Contains("car") || lower.Contains("speed")) return "racing";
            if (lower.Contains("strategy") || lower.Contains("tactical") || lower.Contains("plan")) return "strategy";
            return "adventure";
        }

        private static int ExtractDuration(string prompt)
        {
            var lower = prompt.ToLower();
            if (lower.Contains("short") || lower.Contains("quick")) return 5;
            if (lower.Contains("long") || lower.Contains("extended")) return 30;
            if (lower.Contains("medium")) return 15;
            return 10;
        }

        private static int ExtractDifficulty(string prompt)
        {
            var lower = prompt.ToLower();
            if (lower.Contains("easy") || lower.Contains("simple")) return 1;
            if (lower.Contains("hard") || lower.Contains("difficult") || lower.Contains("challenging")) return 5;
            if (lower.Contains("medium")) return 3;
            return 3;
        }

        private static async Task<string> CreateSceneAsync(GamePlan plan, WorldLayout world, InteractionGraph interactions, ContentBundle bundle)
        {
            // Create a new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Create main game object
            var gameGO = new GameObject("GeneratedGame");
            var game = gameGO.AddComponent<NexoDirectorStudio.Game.DoomFPSGame>();
            game.gamePlan = plan;
            game.worldLayout = world;
            game.interactionGraph = interactions;
            game.contentBundle = bundle;
            
            // Create player
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
            
            // Create world tiles
            CreateWorldTiles(world, bus);
            
            // Create interactive objects
            CreateInteractiveObjects(interactions, bus);
            
            // Save scene
            var scenePath = $"Assets/GeneratedScenes/Generated_{plan.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.unity";
            Directory.CreateDirectory(Path.GetDirectoryName(scenePath));
            EditorSceneManager.SaveScene(scene, scenePath);
            
            return scenePath;
        }

        private static void CreateWorldTiles(WorldLayout world, InteractionBus bus)
        {
            foreach (var tile in world.Tiles)
            {
                var tileGO = new GameObject($"Tile_{tile.GridPosition.x}_{tile.GridPosition.y}");
                tileGO.transform.position = tile.WorldPosition;
                
                // Add collider
                var collider = tileGO.AddComponent<BoxCollider>();
                collider.size = Vector3.one;
                
                // Add material based on tile type
                var renderer = tileGO.AddComponent<MeshRenderer>();
                var mesh = tileGO.AddComponent<MeshFilter>();
                mesh.mesh = CreateCubeMesh();
                
                // Set material color based on tile type
                var material = new Material(Shader.Find("Standard"));
                material.color = GetTileColor(tile.TileType);
                renderer.material = material;
            }
        }

        private static void CreateInteractiveObjects(InteractionGraph interactions, InteractionBus bus)
        {
            foreach (var node in interactions.Nodes)
            {
                var objGO = new GameObject($"Interaction_{node.Name}");
                objGO.transform.position = node.WorldPosition;
                
                // Add collider
                var collider = objGO.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = Vector3.one * 2;
                
                // Add interaction component
                var interaction = objGO.AddComponent<SimpleTestInteraction>();
                bus.Register(interaction);
                
                // Add visual representation
                var renderer = objGO.AddComponent<MeshRenderer>();
                var mesh = objGO.AddComponent<MeshFilter>();
                mesh.mesh = CreateSphereMesh();
                
                var material = new Material(Shader.Find("Standard"));
                material.color = GetInteractionColor(node.NodeType);
                renderer.material = material;
            }
        }

        private static async Task SetupPlaytestingAsync(string scenePath, GamePlan plan)
        {
            // Add feedback collector to the scene
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var feedbackGO = new GameObject("FeedbackCollector");
            var feedback = feedbackGO.AddComponent<FeedbackCollectorComponent>();
            feedback.GamePlanId = plan.Id;
            feedback.ScenePath = scenePath;
            
            EditorSceneManager.SaveScene(scene);
        }

        private static Mesh CreateCubeMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f)
            };
            mesh.triangles = new int[]
            {
                0, 2, 1, 0, 3, 2, 2, 3, 4, 2, 4, 5, 1, 2, 5, 5, 2, 6, 4, 7, 6, 4, 6, 5, 0, 7, 4, 0, 4, 3, 0, 1, 5, 0, 5, 4
            };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreateSphereMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0, 0), new Vector3(0, 0, 0.5f), new Vector3(-0.5f, 0, 0), new Vector3(0, 0, -0.5f), new Vector3(0, -0.5f, 0)
            };
            mesh.triangles = new int[]
            {
                0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1, 5, 2, 1, 5, 3, 2, 5, 4, 3, 5, 1, 4
            };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Color GetTileColor(string tileType)
        {
            return tileType.ToLower() switch
            {
                "ground" => Color.green,
                "wall" => Color.gray,
                "platform" => Color.yellow,
                "hazard" => Color.red,
                _ => Color.white
            };
        }

        private static Color GetInteractionColor(string nodeType)
        {
            return nodeType.ToLower() switch
            {
                "trigger" => Color.blue,
                "event" => Color.cyan,
                "quest" => Color.magenta,
                "dialog" => Color.yellow,
                "spawn" => Color.green,
                _ => Color.white
            };
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