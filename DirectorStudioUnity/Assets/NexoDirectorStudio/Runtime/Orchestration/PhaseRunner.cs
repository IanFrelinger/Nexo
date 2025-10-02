using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NexoDirectorStudio.Orchestration
{
    public sealed class PhaseRunner
    {
        private readonly List<object> _phases = new();

        public PhaseRunner Add(object phase)
        {
            _phases.Add(phase);
            return this;
        }

        public async Task<object> RunAsync(object input, RunContext ctx, CancellationToken ct)
        {
            object current = input;
            foreach (var phase in _phases)
            {
                // Get token using reflection to avoid dynamic binding
                var tokenProperty = phase.GetType().GetProperty("Token");
                var token = tokenProperty?.GetValue(phase) as PhaseToken;
                
                // checkpoint resume
                if (ctx.Checkpoints != null && token != null && await ctx.Checkpoints.TryLoadAsync(token, ctx) is { found: true, data: var cachedObj } && cachedObj != null)
                {
                    ctx.Log.Info("resume", new { phase = token.Value, resumed = true });
                    current = cachedObj;
                    continue;
                }

                // phase execution (with optional caching)
                var output = await PhaseInvoker.InvokeAsync(phase, current, ctx, ct);
                current = output;

                if (ctx.Checkpoints != null && token != null)
                    await ctx.Checkpoints.SaveAsync(token, output, ctx);
            }
            return current;
        }
    }
}
