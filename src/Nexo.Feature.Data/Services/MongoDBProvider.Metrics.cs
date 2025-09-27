using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// Metrics functionality for MongoDB provider.
    /// </summary>
    public partial class MongoDBProvider
    {
        private void UpdateQueryMetrics(TimeSpan queryTime, bool isSuccess)
        {
            lock (_lockObject)
            {
                Interlocked.Increment(ref _totalQueries);
                if (!isSuccess)
                {
                    Interlocked.Increment(ref _failedQueries);
                }
                _queryTimes.Add(queryTime);
                
                // Keep only last 1000 query times to prevent memory growth
                if (_queryTimes.Count > 1000)
                {
                    _queryTimes.RemoveAt(0);
                }
            }
        }
    }
}
