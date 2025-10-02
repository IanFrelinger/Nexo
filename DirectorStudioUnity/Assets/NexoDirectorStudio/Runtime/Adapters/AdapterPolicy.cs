using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexoDirectorStudio.Adapters
{
    public static class AdapterPolicy
    {
        public static async Task<T> WithRetries<T>(Func<CancellationToken, Task<T>> op, int maxRetries, TimeSpan timeout, Action<int,Exception> onRetry=null, CancellationToken ct=default)
        {
            for (int i=0;; i++)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeout);
                try { return await op(cts.Token).ConfigureAwait(false); }
                catch (Exception ex) when (i < maxRetries)
                { onRetry?.Invoke(i+1, ex); await Task.Delay(50 * (i+1), ct); }
            }
        }
    }
}
