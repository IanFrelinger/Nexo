using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;

namespace NexoDirectorStudio.Phases
{
    public sealed class LayoutPhase : IPhase<GamePlan, WorldLayout>
    {
        public PhaseToken Token { get; } = PhaseToken.Of("Layout");
        public bool IsIdempotent => true;

        private readonly IBuildWorldLayoutCommand _cmd;
        public LayoutPhase(IBuildWorldLayoutCommand cmd) => _cmd = cmd;

        public async Task<WorldLayout> RunAsync(GamePlan input, RunContext ctx, CancellationToken ct)
        {
            var t0 = ctx.Clock.UtcNow;
            var cmdInput = new IBuildWorldLayoutCommand.Input(input);
            var layout = await _cmd.ExecuteAsync(cmdInput, ct);
            var dt = (ctx.Clock.UtcNow - t0).TotalMilliseconds;
            ctx.Metrics.Set("layout.ms", dt);
            ArtifactWriter.WriteJson(ctx.PhaseFolder(Token), "output.json", layout);
            return layout;
        }
    }
}
