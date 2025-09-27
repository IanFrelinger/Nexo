using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Interfaces;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// MongoDB database provider implementation
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class MongoDBProvider : IDatabaseProvider
    {
        private readonly ILogger<MongoDBProvider> _logger;
        private readonly string _connectionString;
        private readonly string _databaseName;
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly object _lockObject = new object();
        private long _totalConnections;
        private long _activeConnections;
        private long _totalQueries;
        private long _failedQueries;
        private long _totalTransactions;
        private long _failedTransactions;
        private readonly List<TimeSpan> _queryTimes = new List<TimeSpan>();

        public MongoDBProvider(ILogger<MongoDBProvider> logger, string connectionString, string databaseName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(databaseName);
        }

        public DatabaseType DatabaseType => DatabaseType.MongoDB;
        public string ConnectionString => _connectionString;
    }

    /// <summary>
    /// MongoDB transaction implementation
    /// </summary>
    public class MongoDBTransaction : IDatabaseTransaction
    {
        private readonly IClientSessionHandle _session;
        private readonly MongoDBProvider _provider;
        private bool _disposed;

        public MongoDBTransaction(IClientSessionHandle session, MongoDBProvider provider)
        {
            _session = session;
            _provider = provider;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MongoDBTransaction));
            
            try
            {
                await _session.CommitTransactionAsync(cancellationToken);
                _provider.OnTransactionCompleted(true);
            }
            catch
            {
                _provider.OnTransactionCompleted(false);
                throw;
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MongoDBTransaction));
            
            try
            {
                await _session.AbortTransactionAsync(cancellationToken);
                _provider.OnTransactionCompleted(true);
            }
            catch
            {
                _provider.OnTransactionCompleted(false);
                throw;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _session?.Dispose();
                _disposed = true;
            }
        }
    }
}