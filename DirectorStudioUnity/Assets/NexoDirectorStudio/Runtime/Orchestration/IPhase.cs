using System.Threading;
using System.Threading.Tasks;

namespace NexoDirectorStudio.Orchestration
{
    public interface IPhase<in TIn, TOut>
    {
        PhaseToken Token { get; }
        bool IsIdempotent { get; } // safe to cache
        Task<TOut> RunAsync(TIn input, RunContext ctx, CancellationToken ct);
    }
}
