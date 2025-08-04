using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.Data.Interfaces;
using Nexo.Feature.Data.Models;
using System.Data;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// Unit of Work implementation for managing transactions and repositories
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, object> _repositories;
        private IDbTransaction? _currentTransaction;
        private bool _disposed;

        public UnitOfWork(DbContext context, ILogger<UnitOfWork> logger, IServiceProvider serviceProvider)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _repositories = new Dictionary<Type, object>();
        }

        public IRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : class
        {
            var entityType = typeof(TEntity);
            var keyType = typeof(TKey);

            var repositoryKey = $"{entityType.Name}_{keyType.Name}";

            if (!_repositories.ContainsKey(entityType))
            {
                _logger.LogDebug("Creating repository for entity type {EntityType} with key type {KeyType}", 
                    entityType.Name, keyType.Name);

                var repositoryType = typeof(Repository<,>).MakeGenericType(entityType, keyType);
                
                // Create a logger of the correct type for the repository
                var loggerType = typeof(ILogger<>).MakeGenericType(repositoryType);
                var repositoryLogger = _serviceProvider.GetService(loggerType);
                
                // If we can't get the specific logger, use a null logger
                if (repositoryLogger == null)
                {
                    // Create a null logger of the correct type
                    var nullLoggerType = typeof(NullLogger<>).MakeGenericType(repositoryType);
                    repositoryLogger = Activator.CreateInstance(nullLoggerType);
                }
                
                var repository = Activator.CreateInstance(repositoryType, _context, repositoryLogger);
                
                if (repository == null)
                {
                    throw new InvalidOperationException($"Failed to create repository for {entityType.Name}");
                }

                _repositories[entityType] = repository;
            }

            return (IRepository<TEntity, TKey>)_repositories[entityType];
        }

        public IDbTransaction BeginTransaction()
        {
            if (_currentTransaction != null)
            {
                _logger.LogWarning("Transaction already exists, returning current transaction");
                return _currentTransaction;
            }

            _logger.LogDebug("Beginning new database transaction");
            
            try
            {
                // Check if we're using a relational database
                if (_context.Database.IsRelational())
                {
                    var connection = _context.Database.GetDbConnection();
                    if (connection.State != ConnectionState.Open)
                    {
                        connection.Open();
                    }

                    _currentTransaction = connection.BeginTransaction();
                    return _currentTransaction;
                }
                else
                {
                    // For in-memory databases, create a mock transaction
                    _logger.LogWarning("Using in-memory database - transactions are not supported");
                    _currentTransaction = new MockDbTransaction();
                    return _currentTransaction;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error beginning database transaction");
                throw;
            }
        }

        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            if (_currentTransaction != null)
            {
                _logger.LogWarning("Transaction already exists, returning current transaction");
                return _currentTransaction;
            }

            _logger.LogDebug("Beginning new database transaction with isolation level {IsolationLevel}", isolationLevel);
            
            try
            {
                // Check if we're using a relational database
                if (_context.Database.IsRelational())
                {
                    var connection = _context.Database.GetDbConnection();
                    if (connection.State != ConnectionState.Open)
                    {
                        connection.Open();
                    }

                    _currentTransaction = connection.BeginTransaction(isolationLevel);
                    return _currentTransaction;
                }
                else
                {
                    // For in-memory databases, create a mock transaction
                    _logger.LogWarning("Using in-memory database - transactions are not supported");
                    _currentTransaction = new MockDbTransaction();
                    return _currentTransaction;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error beginning database transaction with isolation level {IsolationLevel}", isolationLevel);
                throw;
            }
        }

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Committing changes to database");
            
            try
            {
                var affectedRecords = await _context.SaveChangesAsync(cancellationToken);
                
                if (_currentTransaction != null)
                {
                    _currentTransaction.Commit();
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }

                _logger.LogInformation("Successfully committed {AffectedRecords} changes to database", affectedRecords);
                return affectedRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error committing changes to database");
                
                if (_currentTransaction != null)
                {
                    _currentTransaction.Rollback();
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }
                
                throw;
            }
        }

        public void Rollback()
        {
            if (_currentTransaction == null)
            {
                _logger.LogWarning("No active transaction to rollback");
                return;
            }

            _logger.LogDebug("Rolling back database transaction");
            
            try
            {
                _currentTransaction.Rollback();
                _currentTransaction.Dispose();
                _currentTransaction = null;
                
                // Reset entity states
                var changedEntries = _context.ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                    .ToList();

                foreach (var entry in changedEntries)
                {
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entry.State = EntityState.Detached;
                            break;
                        case EntityState.Modified:
                            entry.State = EntityState.Unchanged;
                            break;
                        case EntityState.Deleted:
                            entry.Reload();
                            break;
                    }
                }

                _logger.LogInformation("Successfully rolled back database transaction");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back database transaction");
                throw;
            }
        }

        public IDatabaseContext DatabaseContext => throw new NotImplementedException("DatabaseContext property not implemented in this version");

        public bool HasChanges()
        {
            return _context.ChangeTracker.HasChanges();
        }

        public IChangeTracker ChangeTracker => new EntityFrameworkChangeTracker(_context.ChangeTracker, _context, _logger);

        public void DetachAll()
        {
            _logger.LogDebug("Detaching all entities from context");
            
            var entries = _context.ChangeTracker.Entries().ToList();
            foreach (var entry in entries)
            {
                entry.State = EntityState.Detached;
            }
        }

        public async Task RefreshAsync(object entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Refreshing entity of type {EntityType}", entity.GetType().Name);
            
            try
            {
                _context.Entry(entity).Reload();
                await Task.CompletedTask; // For async signature consistency
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing entity of type {EntityType}", entity.GetType().Name);
                throw;
            }
        }

        public async Task<Interfaces.DatabaseStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Getting database statistics");
            
            try
            {
                // This is a simplified implementation
                // In a real implementation, you would query the database for actual statistics
                var statistics = new Interfaces.DatabaseStatistics
                {
                    TotalConnections = 1,
                    ActiveConnections = 1,
                    TotalQueries = 0,
                    FailedQueries = 0,
                    AverageQueryTime = TimeSpan.Zero,
                    TotalTransactions = 0,
                    FailedTransactions = 0,
                    LastReset = DateTime.UtcNow
                };

                await Task.CompletedTask; // For async signature consistency
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database statistics");
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _currentTransaction?.Dispose();
                _context?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Entity Framework change tracker wrapper
    /// </summary>
    public class EntityFrameworkChangeTracker : IChangeTracker
    {
        private readonly Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker _changeTracker;
        private readonly DbContext _context;
        private readonly ILogger _logger;

        public EntityFrameworkChangeTracker(Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker changeTracker, DbContext context, ILogger logger)
        {
            _changeTracker = changeTracker;
            _context = context;
            _logger = logger;
        }

        public IEnumerable<object> GetTrackedEntities()
        {
            return _changeTracker.Entries().Select(e => e.Entity);
        }

        public IEnumerable<object> GetEntitiesWithState(Nexo.Feature.Data.Enums.EntityState state)
        {
            var efState = ConvertToEFState(state);
            return _changeTracker.Entries()
                .Where(e => e.State == efState)
                .Select(e => e.Entity);
        }

        public Nexo.Feature.Data.Enums.EntityState GetEntityState(object entity)
        {
            var entry = _context.Entry(entity);
            return ConvertFromEFState(entry.State);
        }

        public bool IsTracked(object entity)
        {
            return _changeTracker.Entries().Any(e => e.Entity == entity);
        }

        public void Detach(object entity)
        {
            var entry = _context.Entry(entity);
            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }

        public int GetTrackedEntitiesCount()
        {
            return _changeTracker.Entries().Count();
        }

        public ChangeSummary GetChangeSummary()
        {
            var entries = _changeTracker.Entries();
            
            return new ChangeSummary
            {
                AddedCount = entries.Count(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added),
                ModifiedCount = entries.Count(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Modified),
                DeletedCount = entries.Count(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Deleted),
                UnchangedCount = entries.Count(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Unchanged),
                DetachedCount = entries.Count(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            };
        }

        private static Microsoft.EntityFrameworkCore.EntityState ConvertToEFState(Nexo.Feature.Data.Enums.EntityState state)
        {
            return state switch
            {
                Nexo.Feature.Data.Enums.EntityState.Detached => Microsoft.EntityFrameworkCore.EntityState.Detached,
                Nexo.Feature.Data.Enums.EntityState.Unchanged => Microsoft.EntityFrameworkCore.EntityState.Unchanged,
                Nexo.Feature.Data.Enums.EntityState.Added => Microsoft.EntityFrameworkCore.EntityState.Added,
                Nexo.Feature.Data.Enums.EntityState.Deleted => Microsoft.EntityFrameworkCore.EntityState.Deleted,
                Nexo.Feature.Data.Enums.EntityState.Modified => Microsoft.EntityFrameworkCore.EntityState.Modified,
                _ => Microsoft.EntityFrameworkCore.EntityState.Detached
            };
        }

        private static Nexo.Feature.Data.Enums.EntityState ConvertFromEFState(Microsoft.EntityFrameworkCore.EntityState state)
        {
            return state switch
            {
                Microsoft.EntityFrameworkCore.EntityState.Detached => Nexo.Feature.Data.Enums.EntityState.Detached,
                Microsoft.EntityFrameworkCore.EntityState.Unchanged => Nexo.Feature.Data.Enums.EntityState.Unchanged,
                Microsoft.EntityFrameworkCore.EntityState.Added => Nexo.Feature.Data.Enums.EntityState.Added,
                Microsoft.EntityFrameworkCore.EntityState.Deleted => Nexo.Feature.Data.Enums.EntityState.Deleted,
                Microsoft.EntityFrameworkCore.EntityState.Modified => Nexo.Feature.Data.Enums.EntityState.Modified,
                _ => Nexo.Feature.Data.Enums.EntityState.Detached
            };
        }
    }

    /// <summary>
    /// Mock database transaction for in-memory databases
    /// </summary>
    public class MockDbTransaction : IDbTransaction
    {
        public IDbConnection Connection => null!;
        public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
        public bool IsDisposed { get; private set; }

        public void Commit()
        {
            // No-op for in-memory database
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public void Rollback()
        {
            // No-op for in-memory database
        }
    }
} 