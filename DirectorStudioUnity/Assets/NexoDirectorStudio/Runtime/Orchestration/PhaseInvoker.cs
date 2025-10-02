using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NexoDirectorStudio.Orchestration
{
    internal static class PhaseInvoker
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> _cache = new();

        public static async Task<object> InvokeAsync(object phase, object input, RunContext ctx, CancellationToken ct)
        {
            var t = phase.GetType();
            var mi = _cache.GetOrAdd(t, ty =>
            {
                // Find RunAsync(TIn, RunContext, CancellationToken)
                foreach (var m in ty.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (m.Name != "RunAsync") continue;
                    var p = m.GetParameters();
                    if (p.Length == 3 && p[1].ParameterType == typeof(RunContext) && p[2].ParameterType == typeof(CancellationToken))
                        return m;
                }
                throw new InvalidOperationException($"No suitable RunAsync found on {ty.FullName}");
            });

            var result = mi.Invoke(phase, new[] { input, ctx, ct });
            if (result is Task task)
            {
                await task.ConfigureAwait(false);
                var prop = task.GetType().GetProperty("Result");
                return prop != null ? prop.GetValue(task) : null;
            }
            return result;
        }
    }
}
