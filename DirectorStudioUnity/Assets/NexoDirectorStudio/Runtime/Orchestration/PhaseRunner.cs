using System;
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
            var phaseIndex = 0;
            
            foreach (var phase in _phases)
            {
                try
                {
                    // Get token using reflection to avoid dynamic binding
                    var tokenProperty = phase.GetType().GetProperty("Token");
                    var token = tokenProperty?.GetValue(phase) as PhaseToken;
                    
                    UnityEngine.Debug.Log($"[PhaseRunner] Starting phase {phaseIndex + 1}/{_phases.Count}: {token?.Value ?? "Unknown"}");
                    
                    // checkpoint resume
                    if (ctx.Checkpoints != null && token != null && await ctx.Checkpoints.TryLoadAsync(token, ctx) is { found: true, data: var cachedObj } && cachedObj != null)
                    {
                        ctx.Log.Info("resume", new { phase = token.Value, resumed = true });
                        UnityEngine.Debug.Log($"[PhaseRunner] Resumed phase {token.Value} from checkpoint");
                        current = cachedObj;
                        phaseIndex++;
                        continue;
                    }

                    // phase execution with fallback support
                    UnityEngine.Debug.Log($"[PhaseRunner] Executing phase {token?.Value ?? "Unknown"} with input type: {current?.GetType().Name ?? "null"}");
                    var output = await PhaseGuard.ExecuteWithFallback(phase, current, ctx, ct);
                    UnityEngine.Debug.Log($"[PhaseRunner] Phase {token?.Value ?? "Unknown"} completed, output type: {output?.GetType().Name ?? "null"}");
                    current = output;

                    if (ctx.Checkpoints != null && token != null)
                    {
                        await ctx.Checkpoints.SaveAsync(token, output, ctx);
                        UnityEngine.Debug.Log($"[PhaseRunner] Saved checkpoint for phase {token?.Value ?? "Unknown"}");
                    }
                    
                    phaseIndex++;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[PhaseRunner] Phase {phaseIndex + 1} failed: {ex}");
                    throw;
                }
            }
            
            UnityEngine.Debug.Log($"[PhaseRunner] All phases completed successfully");
            return current;
        }
    }
}
