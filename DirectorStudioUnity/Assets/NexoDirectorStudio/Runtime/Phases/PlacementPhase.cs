using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;

namespace NexoDirectorStudio.Phases
{
    public sealed class PlacementPhase : IPhase<WorldLayout, InteractionGraph>
    {
        public PhaseToken Token { get; } = PhaseToken.Of("Placement");
        public bool IsIdempotent => true;

        private readonly IPlaceInteractionsCommand _cmd;
        public PlacementPhase(IPlaceInteractionsCommand cmd) => _cmd = cmd;

        public async Task<InteractionGraph> RunAsync(WorldLayout input, RunContext ctx, CancellationToken ct)
        {
            var t0 = ctx.Clock.UtcNow;
            // Note: IPlaceInteractionsCommand.Input requires both WorldLayout and GamePlan
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
            var cmdInput = new IPlaceInteractionsCommand.Input(input, gamePlan);
            var graph = await _cmd.ExecuteAsync(cmdInput, ct);
            var dt = (ctx.Clock.UtcNow - t0).TotalMilliseconds;
            ctx.Metrics.Set("placement.ms", dt);
            ArtifactWriter.WriteJson(ctx.PhaseFolder(Token), "output.json", graph);
            return graph;
        }
    }
}
