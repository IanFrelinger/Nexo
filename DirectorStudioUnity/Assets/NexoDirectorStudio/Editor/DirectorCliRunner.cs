using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Validators;

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
                var prompt = ReadArg(args, "--prompt") ?? "Create a simple Adventure game with exploration";

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

                // Exit cleanly
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"CLI Error: {ex.Message}\n{ex.StackTrace}");
                EditorApplication.Exit(1);
            }
        }

        private static string? ReadArg(string[] args, string key)
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

        private static string? TryExtract(string text, string key)
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
    }
}


