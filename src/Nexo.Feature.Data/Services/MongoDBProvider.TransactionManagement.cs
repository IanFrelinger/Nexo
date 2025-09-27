using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// Transaction management functionality for MongoDB provider.
    /// </summary>
    public partial class MongoDBProvider
    {
        public async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _totalTransactions);
            try
            {
                var session = await _client.StartSessionAsync(null, cancellationToken);
                session.StartTransaction();
                return new MongoDBTransaction(session, this);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedTransactions);
                _logger.LogError(ex, "Failed to begin MongoDB transaction");
                throw;
            }
        }

        internal void OnTransactionCompleted(bool isSuccess)
        {
            if (!isSuccess)
            {
                Interlocked.Increment(ref _failedTransactions);
            }
        }
    }
}