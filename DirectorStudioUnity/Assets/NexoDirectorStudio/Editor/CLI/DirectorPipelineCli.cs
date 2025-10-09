#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Phases;
using NexoDirectorStudio.Config;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Validators;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.EditorCLI
{
    public static class DirectorPipelineCli
    {
        // -executeMethod NexoDirectorStudio.EditorCLI.DirectorPipelineCli.Run
        //   [--config nexo.pipeline.json] [--prompt "..."] [--seed 42] [--artifacts /abs/path] [--resume]
        public static async void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                string Arg(string key, string def = null)
                {
                    var i = Array.IndexOf(args, key);
                    return (i >= 0 && i < args.Length - 1) ? args[i + 1] : def;
                }

                var configPath = Arg("--config", "nexo.pipeline.json");
                var cfg = PipelineConfigLoader.LoadFromPath(configPath, new PipelineConfig());
                // CLI flags override config
                cfg.prompt    = Arg("--prompt", cfg.prompt);
                cfg.artifacts = Arg("--artifacts", cfg.artifacts);
                if (int.TryParse(Arg("--seed", cfg.seed.ToString()), out var s)) cfg.seed = s;
                var resume = args.Contains("--resume");

                var svc = new DirectorStudioServiceUnified();
                var ctx = new RunContext { Seed = cfg.seed, ArtifactRoot = cfg.artifacts, Checkpoints = resume ? new FileCheckpointStore() : null };
                ctx.Rng.Init(cfg.seed);
                Directory.CreateDirectory(ctx.RunFolder);

                var runner = new PhaseRunner();
                var reg = PhaseRegistry.BuildDefault(svc);
                if (cfg.phases != null && cfg.phases.Length > 0)
                    foreach (var p in cfg.phases) runner.Add(reg.Create(p.type, svc));
                else
                    runner.Add(new PlanPhase(svc.GetService<IPlanGameSliceCommand>()))
                          .Add(new LayoutPhase(svc.GetService<IBuildWorldLayoutCommand>()))
                          .Add(new PlacementPhase(svc.GetService<IPlaceInteractionsCommand>()))
                          .Add(new ContentPhase(svc.GetService<ICreateContentBundleCommand>()))
                          .Add(new ValidatePhase(svc.GetService<IValidator<ContentBundle>>()));

                var brief = new DesignBrief(Description: cfg.prompt, GenreHint: "fps", Seed: cfg.seed); // adapt to your DTO
                Debug.Log($"[DirectorPipelineCli] Starting pipeline with prompt: {cfg.prompt}, seed: {cfg.seed}");
                var t0 = DateTime.UtcNow;
                await RunAsync(runner, brief, ctx);
                var t1 = DateTime.UtcNow;
                Debug.Log($"[DirectorPipelineCli] Pipeline completed in {(t1 - t0).TotalMilliseconds:F0}ms");

                // Run index summary
                var summary = new RunSummary
                {
                    runId = ctx.RunId.ToString("N"),
                    seed = cfg.seed,
                    prompt = cfg.prompt,
                    config = configPath,
                    artifacts = ctx.RunFolder,
                    elapsedMs = (t1 - t0).TotalMilliseconds
                };
                File.WriteAllText(Path.Combine(ctx.RunFolder, "run.summary.json"), JsonUtility.ToJson(summary, true));
                Debug.Log("[DirectorPipelineCli] Done. " + ctx.RunFolder);
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError("[DirectorPipelineCli] " + ex);
                EditorApplication.Exit(2);
            }
        }

        private static async Task RunAsync(PhaseRunner runner, object input, RunContext ctx)
        {
            Debug.Log($"[DirectorPipelineCli] Running phases with input type: {input.GetType().Name}");
            await runner.RunAsync(input, ctx, CancellationToken.None);
            Debug.Log($"[DirectorPipelineCli] All phases completed successfully");
        }

        [Serializable] private class RunSummary
        {
            public string runId;
            public int seed;
            public string prompt;
            public string config;
            public string artifacts;
            public double elapsedMs;
        }
    }
}
#endif