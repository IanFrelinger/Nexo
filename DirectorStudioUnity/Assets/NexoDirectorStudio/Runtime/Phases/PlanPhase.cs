using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;

namespace NexoDirectorStudio.Phases
{
    public sealed class PlanPhase : IPhase<DesignBrief, GamePlan>
    {
        public PhaseToken Token { get; } = PhaseToken.Of("Plan");
        public bool IsIdempotent => true;

        private readonly IPlanGameSliceCommand _cmd;
        public PlanPhase(IPlanGameSliceCommand cmd) => _cmd = cmd;

        public async Task<GamePlan> RunAsync(DesignBrief input, RunContext ctx, CancellationToken ct)
        {
            var t0 = ctx.Clock.UtcNow;
            var cmdInput = new IPlanGameSliceCommand.Input(input);
            var plan = await _cmd.ExecuteAsync(cmdInput, ct);
            var dt = (ctx.Clock.UtcNow - t0).TotalMilliseconds;
            ctx.Metrics.Set("plan.ms", dt);
            ArtifactWriter.WriteJson(ctx.PhaseFolder(Token), "output.json", plan);
            return plan;
        }
    }
}
