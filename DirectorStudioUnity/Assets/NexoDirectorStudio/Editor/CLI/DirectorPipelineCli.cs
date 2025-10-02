#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Phases;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Validators;
using UnityEditor;
using UnityEngine;

namespace NexoDirectorStudio.EditorCLI
{
    public static class DirectorPipelineCli
    {
        // Usage (example):
        //  -executeMethod NexoDirectorStudio.EditorCLI.DirectorPipelineCli.Run --prompt "..." --seed 42 --artifacts /abs/path --resume
        public static void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                string Arg(string key, string def=null)
                {
                    var idx = Array.IndexOf(args, key);
                    return (idx >= 0 && idx < args.Length - 1) ? args[idx+1] : def;
                }

                var prompt = Arg("--prompt", "micro room with one switch and one door");
                var seed = int.TryParse(Arg("--seed","1337"), out var s) ? s : 1337;
                var artifacts = Arg("--artifacts", "Artifacts");
                var resume = args.Contains("--resume");

                var svc = new DirectorStudioService();
                var ctx = new RunContext { Seed = seed, ArtifactRoot = artifacts, Checkpoints = resume ? new FileCheckpointStore() : null };
                ctx.Rng.Init(seed);
                Directory.CreateDirectory(ctx.RunFolder);

                var runner = new PhaseRunner()
                    .Add(new PlanPhase(svc.GetService<IPlanGameSliceCommand>()))
                    .Add(new LayoutPhase(svc.GetService<IBuildWorldLayoutCommand>()))
                    .Add(new PlacementPhase(svc.GetService<IPlaceInteractionsCommand>()))
                    .Add(new ContentPhase(svc.GetService<ICreateContentBundleCommand>()))
                    .Add(new ValidatePhase(svc.GetService<IValidator<ContentBundle>>()));

                var brief = new DesignBrief(Description: prompt);
                _ = RunAsync(runner, brief, ctx);
            }
            catch (Exception ex)
            {
                Debug.LogError("[DirectorPipelineCli] " + ex);
                EditorApplication.Exit(2);
            }
        }

        private static async Task RunAsync(PhaseRunner runner, object input, RunContext ctx)
        {
            await runner.RunAsync(input, ctx, CancellationToken.None);
            Debug.Log("[DirectorPipelineCli] Done. Artifacts: " + ctx.RunFolder);
            EditorApplication.Exit(0);
        }
    }
}
#endif
