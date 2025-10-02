using System;
using System.Collections.Generic;
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

                // Phase 3: Command Setup
                System.Console.WriteLine("⚙️ Phase 3: Command Setup");
                System.Console.WriteLine("  ✓ Getting command services...");
                var planCmd = service.GetService<IPlanGameSliceCommand>();
                var buildCmd = service.GetService<IBuildWorldLayoutCommand>();
                var placeCmd = service.GetService<IPlaceInteractionsCommand>();
                var bundleCmd = service.GetService<ICreateContentBundleCommand>();
                System.Console.WriteLine("  ✓ All commands ready");
                System.Console.WriteLine();

                // Phase 4: Game Planning
                System.Console.WriteLine("🎮 Phase 4: Game Planning");
                System.Console.WriteLine("  ✓ Executing game slice planning...");
                var gamePlan = await planCmd.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
                System.Console.WriteLine($"  ✓ Game plan created: {gamePlan.Id}");
                System.Console.WriteLine($"  ✓ Mechanics: {gamePlan.CoreMechanics.Count}, Assets: {gamePlan.RequiredAssets.Count}");
                System.Console.WriteLine();

                // Phase 5: World Building
                System.Console.WriteLine("🏗️ Phase 5: World Building");
                System.Console.WriteLine("  ✓ Building world layout...");
                var world = await buildCmd.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
                System.Console.WriteLine($"  ✓ World layout created: {world.Id}");
                System.Console.WriteLine($"  ✓ Dimensions: {world.Dimensions.x}x{world.Dimensions.y}, Tiles: {world.Tiles.Count}");
                System.Console.WriteLine();

                // Phase 6: Interaction Placement
                System.Console.WriteLine("🔗 Phase 6: Interaction Placement");
                System.Console.WriteLine("  ✓ Placing interactions and connections...");
                var graph = await placeCmd.ExecuteAsync(new IPlaceInteractionsCommand.Input(world, gamePlan), CancellationToken.None);
                System.Console.WriteLine($"  ✓ Interaction graph created: {graph.Id}");
                System.Console.WriteLine($"  ✓ Nodes: {graph.Nodes.Count}, Connections: {graph.Connections.Count}");
                System.Console.WriteLine();

                // Phase 7: Content Generation
                System.Console.WriteLine("📦 Phase 7: Content Generation");
                System.Console.WriteLine("  ✓ Creating content bundle...");
                var bundle = await bundleCmd.ExecuteAsync(new ICreateContentBundleCommand.Input(graph, gamePlan), CancellationToken.None);
                System.Console.WriteLine($"  ✓ Content bundle created: {bundle.Id}");
                System.Console.WriteLine($"  ✓ Assets: {bundle.Assets.Count}");
                System.Console.WriteLine();

                // Phase 8: Validation
                System.Console.WriteLine("✅ Phase 8: Validation");
                System.Console.WriteLine("  ✓ Running validators...");
                var validators = service.GetService<IEnumerable<IValidator<GamePlan>>>() ?? Enumerable.Empty<IValidator<GamePlan>>();
                var results = new List<ValidationResult>();
                var validatorCount = 0;
                foreach (var v in validators)
                {
                    validatorCount++;
                    System.Console.WriteLine($"  ✓ Running validator {validatorCount}...");
                    var r = await v.ValidateAsync(gamePlan, CancellationToken.None);
                    results.Add(r);
                }
                var passedValidations = results.Count(r => r.Score > 0);
                System.Console.WriteLine($"  ✓ Validation complete: {passedValidations}/{results.Count} passed");
                System.Console.WriteLine();

                // Print concise summary
                System.Console.WriteLine("=== Pipeline Result ===");
                System.Console.WriteLine($"Brief: {designBrief.Description} (GenreHint={designBrief.GenreHint})");
                System.Console.WriteLine($"GamePlan: {gamePlan.Id} | Mechanics={gamePlan.CoreMechanics.Count} Assets={gamePlan.RequiredAssets.Count}");
                System.Console.WriteLine($"World: {world.Id} | Size={world.Dimensions.x}x{world.Dimensions.y} Tiles={world.Tiles.Count}");
                System.Console.WriteLine($"Graph: {graph.Id} | Nodes={graph.Nodes.Count} Connections={graph.Connections.Count}");
                System.Console.WriteLine($"Bundle: {bundle.Id} | Assets={bundle.Assets.Count}");
                System.Console.WriteLine($"Validators: {results.Count} | Passed≈{results.Count(r=>r.Score>0)}");
                System.Console.WriteLine();

                // Phase 9: Scene Generation & Editor Integration
                System.Console.WriteLine("🎬 Phase 9: Scene Generation & Editor Integration");
                System.Console.WriteLine("  ✓ Generating Unity scene from content bundle...");
                var scenePath = await GenerateSceneFromBundleAsync(bundle, gamePlan, world, graph);
                System.Console.WriteLine($"  ✓ Scene created: {scenePath}");
                System.Console.WriteLine();

                // Phase 10: Auto-Playtest Setup
                System.Console.WriteLine("🤖 Phase 10: Auto-Playtest Setup");
                System.Console.WriteLine("  ✓ Setting up AI autoplayer for immediate testing...");
                await SetupAutoPlaytestAsync(scenePath, gamePlan);
                System.Console.WriteLine("  ✓ Autoplayer configured and ready");
                System.Console.WriteLine();

                // Phase 11: Editor Focus
                System.Console.WriteLine("🎯 Phase 11: Editor Focus");
                System.Console.WriteLine("  ✓ Opening scene in Unity Editor...");
                await OpenSceneInEditorAsync(scenePath);
                System.Console.WriteLine("  ✓ Scene loaded and focused for playtesting");
                System.Console.WriteLine();

                System.Console.WriteLine("🎉 Ready for Playtesting!");
                System.Console.WriteLine("=========================");
                System.Console.WriteLine("• Scene is now open in Unity Editor");
                System.Console.WriteLine("• AI Autoplayer is attached and ready");
                System.Console.WriteLine("• Press Play to start automated testing");
                System.Console.WriteLine("• Provide feedback to iterate on design");
                System.Console.WriteLine();

                // Phase 12: Feedback Collection Setup
                System.Console.WriteLine("💬 Phase 12: Feedback Collection Setup");
                System.Console.WriteLine("  ✓ Setting up feedback collection system...");
                await SetupFeedbackCollectionAsync(gamePlan.Id, scenePath);
                System.Console.WriteLine("  ✓ Feedback system ready - use 'NexoDirectorStudio/Record Feedback' menu");
                System.Console.WriteLine();

                // Don't exit - keep editor open for playtesting
                System.Console.WriteLine("Editor will remain open for playtesting and feedback collection.");
                System.Console.WriteLine("Use the Unity menu 'NexoDirectorStudio/Record Feedback' to provide design feedback.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"CLI Error: {ex.Message}\n{ex.StackTrace}");
                EditorApplication.Exit(1);
            }
        }

        private static string ReadArg(string[] args, string key)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return null;
        }

        private static async Task<DesignBrief> PromptToBriefAsync(DirectorStudioService service, string prompt, CancellationToken ct)
        {
            // Heuristic parse of the prompt without external adapters
            string description = prompt;
            string genre = InferGenre(prompt);
            int minutes = InferMinutes(prompt);
            int difficulty = InferDifficulty(prompt);

            return await Task.FromResult(new DesignBrief(
                Description: description,
                GenreHint: genre,
                TargetDurationMinutes: minutes,
                DifficultyLevel: difficulty,
                Seed: Environment.TickCount
            ));
        }

        private static string TryExtract(string text, string key)
        {
            try
            {
                var idx = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return null;
                var colon = text.IndexOf(':', idx);
                if (colon < 0) return null;

                int end = text.Length;
                var comma = text.IndexOf(',', colon + 1);
                var newline = text.IndexOf('\n', colon + 1);
                var brace = text.IndexOf('}', colon + 1);
                var bracket = text.IndexOf(']', colon + 1);
                if (comma >= 0) end = Math.Min(end, comma);
                if (newline >= 0) end = Math.Min(end, newline);
                if (brace >= 0) end = Math.Min(end, brace);
                if (bracket >= 0) end = Math.Min(end, bracket);

                var raw = text.Substring(colon + 1, end - colon - 1).Trim().Trim('"');
                return string.IsNullOrWhiteSpace(raw) ? null : raw;
            }
            catch { return null; }
        }

        private static string InferGenre(string prompt)
        {
            var p = prompt.ToLowerInvariant();
            if (p.Contains("fps") || p.Contains("shooter")) return "FPS";
            if (p.Contains("platform")) return "Platformer";
            if (p.Contains("rpg")) return "RPG";
            if (p.Contains("adventure")) return "Adventure";
            if (p.Contains("survival")) return "Survival";
            return "Adventure";
        }

        private static int InferMinutes(string prompt)
        {
            // pick the first number in range 3..60, else default 10
            int num = 0;
            var digits = new string(prompt.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out num))
            {
                if (num >= 3 && num <= 60) return num;
            }
            return 10;
        }

        private static int InferDifficulty(string prompt)
        {
            var p = prompt.ToLowerInvariant();
            if (p.Contains("easy")) return 1;
            if (p.Contains("hard")) return 3;
            if (p.Contains("medium") || p.Contains("normal")) return 2;
            return 2;
        }

        private static async Task<string> GenerateSceneFromBundleAsync(ContentBundle bundle, GamePlan gamePlan, WorldLayout world, InteractionGraph graph)
        {
            // Create a new scene for the generated game slice
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // Generate scene name from game plan
            var sceneName = $"Generated_{gamePlan.Id}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var scenePath = $"Assets/GeneratedScenes/{sceneName}.unity";
            
            // Ensure directory exists
            var directory = System.IO.Path.GetDirectoryName(scenePath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Create basic scene structure
            await CreateBasicSceneStructureAsync(scene, world, graph, gamePlan);
            
            // Save the scene
            EditorSceneManager.SaveScene(scene, scenePath);
            
            return scenePath;
        }

        private static async Task CreateBasicSceneStructureAsync(UnityEngine.SceneManagement.Scene scene, WorldLayout world, InteractionGraph graph, GamePlan gamePlan)
        {
            // Create a main camera
            var cameraGO = new GameObject("Main Camera");
            var camera = cameraGO.AddComponent<Camera>();
            cameraGO.transform.position = new Vector3(0, 5, -10);
            cameraGO.transform.LookAt(Vector3.zero);

            // Create a player object
            var playerGO = new GameObject("Player");
            playerGO.transform.position = Vector3.zero;
            
            // Add basic player components
            var playerController = playerGO.AddComponent<CharacterController>();
            playerController.height = 2f;
            playerController.radius = 0.5f;
            
            // Add a simple capsule for visualization
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(playerGO.transform);
            capsule.transform.localPosition = Vector3.zero;
            capsule.name = "PlayerVisual";

            // Create world geometry based on world layout
            await CreateWorldGeometryAsync(world, gamePlan);
            
            // Create interaction points based on graph
            await CreateInteractionPointsAsync(graph, gamePlan);
            
            // Add lighting
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        private static async Task CreateWorldGeometryAsync(WorldLayout world, GamePlan gamePlan)
        {
            var worldGO = new GameObject("World");
            
            // Create basic floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(worldGO.transform);
            floor.transform.position = new Vector3(0, -1, 0);
            floor.transform.localScale = new Vector3(world.Dimensions.x * 2, 1, world.Dimensions.y * 2);
            floor.name = "Floor";
            
            // Create walls based on world dimensions
            for (int x = 0; x < world.Dimensions.x; x++)
            {
                for (int y = 0; y < world.Dimensions.y; y++)
                {
                    var tile = world.Tiles.FirstOrDefault(t => t.GridPosition.x == x && t.GridPosition.y == y);
                    if (tile != null)
                    {
                        await CreateTileGeometryAsync(tile, worldGO.transform, x, y);
                    }
                }
            }
        }

        private static async Task CreateTileGeometryAsync(object tile, Transform parent, int x, int y)
        {
            // Create a simple cube for each tile
            var tileGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tileGO.transform.SetParent(parent);
            tileGO.transform.position = new Vector3(x * 2, 0, y * 2);
            tileGO.transform.localScale = new Vector3(1.8f, 2f, 1.8f);
            tileGO.name = $"Tile_{x}_{y}";
            
            // Add a random color for variety
            var renderer = tileGO.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(
                    UnityEngine.Random.Range(0.3f, 0.8f),
                    UnityEngine.Random.Range(0.3f, 0.8f),
                    UnityEngine.Random.Range(0.3f, 0.8f)
                );
            }
        }

            private static async Task CreateInteractionPointsAsync(InteractionGraph graph, GamePlan gamePlan)
            {
                var interactionsGO = new GameObject("Interactions");
                
                foreach (var node in graph.Nodes)
                {
                    var nodeGO = new GameObject($"Interaction_{node.Id}");
                    nodeGO.transform.SetParent(interactionsGO.transform);
                    nodeGO.transform.position = node.WorldPosition;
                    
                    // Create visual representation based on node type
                    GameObject visual = null;
                    Color nodeColor = Color.white;
                    
                    switch (node.NodeType)
                    {
                        case "Enemy":
                            visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                            nodeColor = Color.red;
                            nodeGO.tag = "Enemy";
                            break;
                        case "Collectible":
                            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            nodeColor = Color.blue;
                            nodeGO.name = $"Keycard_{node.Id}";
                            break;
                        case "Door":
                            visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            nodeColor = Color.gray;
                            nodeGO.name = $"Door_{node.Id}";
                            break;
                        case "Boss":
                            visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                            nodeColor = Color.magenta;
                            nodeGO.name = $"Boss_{node.Id}";
                            break;
                        case "PowerUp":
                            visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            nodeColor = Color.green;
                            nodeGO.name = $"PowerUp_{node.Id}";
                            break;
                        default:
                            visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            nodeColor = Color.yellow;
                            break;
                    }
                    
                    if (visual != null)
                    {
                        visual.transform.SetParent(nodeGO.transform);
                        visual.transform.localPosition = Vector3.zero;
                        visual.transform.localScale = Vector3.one * 0.8f;
                        visual.name = "Visual";
                        
                        // Add color to make it visible
                        var renderer = visual.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            renderer.material.color = nodeColor;
                        }
                    }
                }
            }

        private static async Task SetupAutoPlaytestAsync(string scenePath, GamePlan gamePlan)
        {
            // Find the player object in the scene
            var scene = EditorSceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid()) return;
            
            var playerGO = GameObject.Find("Player");
            if (playerGO == null) return;
            
            // Add AgentDirector component
            var agentDirector = playerGO.AddComponent<AgentDirector>();
            
            // Configure the agent director
            agentDirector.prompt = gamePlan.Description;
            agentDirector.attachAutoplayer = true;
            
            // Add AIAutoplayer component for automated testing
            var autoplayer = playerGO.AddComponent<AIAutoplayer>();
            
            // Configure autoplayer behavior based on game plan
            // AIAutoplayer uses built-in behavior, no Policy property needed
            // It automatically prioritizes: enemy → power-up → goal → wander
        }

        private static async Task OpenSceneInEditorAsync(string scenePath)
        {
            // Load the scene in the editor
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            
            // Focus the scene view on the player
            var playerGO = GameObject.Find("Player");
            if (playerGO != null)
            {
                Selection.activeGameObject = playerGO;
                SceneView.FrameLastActiveSceneView();
            }
            
            // Set the scene as the active scene
            var scene = EditorSceneManager.GetSceneByPath(scenePath);
            EditorSceneManager.SetActiveScene(scene);
        }

        private static async Task SetupFeedbackCollectionAsync(string gamePlanId, string scenePath)
        {
            // Store the current game plan ID and scene path for feedback collection
            EditorPrefs.SetString("CurrentGamePlanId", gamePlanId);
            EditorPrefs.SetString("CurrentScenePath", scenePath);
            
            // Create a feedback collection component in the scene
            var feedbackGO = new GameObject("FeedbackCollector");
            var feedbackComponent = feedbackGO.AddComponent<FeedbackCollectorComponent>();
            feedbackComponent.GamePlanId = gamePlanId;
            feedbackComponent.ScenePath = scenePath;
        }
    }

    /// <summary>
    /// MonoBehaviour component for feedback collection in generated scenes.
    /// </summary>
    public class FeedbackCollectorComponent : MonoBehaviour
    {
        public string GamePlanId;
        public string ScenePath;

        [ContextMenu("Record Positive Feedback")]
        public void RecordPositiveFeedback()
        {
            FeedbackCollector.RecordFeedback(GamePlanId, ScenePath, "Positive playtest experience", 5);
        }

        [ContextMenu("Record Negative Feedback")]
        public void RecordNegativeFeedback()
        {
            FeedbackCollector.RecordFeedback(GamePlanId, ScenePath, "Issues identified during playtest", 2);
        }

        [ContextMenu("Show Feedback Summary")]
        public void ShowFeedbackSummary()
        {
            var summary = FeedbackCollector.GenerateFeedbackSummary(GamePlanId);
            Debug.Log($"Feedback Summary for {GamePlanId}:\n{summary}");
        }
    }
}


