using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;

namespace NexoDirectorStudio.Phases
{
    public sealed class ContentPhase : IPhase<InteractionGraph, ContentBundle>
    {
        public PhaseToken Token { get; } = PhaseToken.Of("Content");
        public bool IsIdempotent => false;

        private readonly ICreateContentBundleCommand _cmd;
        public ContentPhase(ICreateContentBundleCommand cmd) => _cmd = cmd;

        public async Task<ContentBundle> RunAsync(InteractionGraph input, RunContext ctx, CancellationToken ct)
        {
            var t0 = ctx.Clock.UtcNow;
            // Note: ICreateContentBundleCommand.Input requires both InteractionGraph and GamePlan
            // For now, we'll create a minimal GamePlan - this should be passed from previous phase
            var gamePlan = new GamePlan(
                Id: Guid.NewGuid().ToString(),
                SourceBrief: new DesignBrief("Minimal brief"),
                Genre: "Unknown",
                Description: "Minimal game plan",
                CoreMechanics: new List<string>(),
                PlayerExperience: new List<string>(),
                EstimatedDurationMinutes: 5,
                DifficultyProgression: new List<DifficultyBeat>(),
                NarrativeBeats: new List<string>(),
                RequiredAssets: new List<AssetRequirement>(),
                Seed: 0,
                GeneratedAt: DateTimeOffset.UtcNow,
                Hash: "minimal"
            );
            var cmdInput = new ICreateContentBundleCommand.Input(input, gamePlan);
            var bundle = await _cmd.ExecuteAsync(cmdInput, ct);
            var dt = (ctx.Clock.UtcNow - t0).TotalMilliseconds;
            ctx.Metrics.Set("content.ms", dt);
            ArtifactWriter.WriteJson(ctx.PhaseFolder(Token), "output.json", bundle);
            return bundle;
        }
    }
}
