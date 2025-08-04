using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Data.Interfaces;

namespace Nexo.Feature.Data.Services
{
    /// <summary>
    /// Transaction manager for coordinating database transactions across repositories
    /// </summary>
    public class TransactionManager : ITransactionManager
    {
        private readonly IDatabaseProvider _databaseProvider;
        private readonly ILogger<TransactionManager> _logger;
        private readonly List<Action> _rollbackActions;
        private IDatabaseTransaction? _currentTransaction;
        private bool _disposed;

        public TransactionManager(IDatabaseProvider databaseProvider, ILogger<TransactionManager> logger)
        {
            _databaseProvider = databaseProvider ?? throw new ArgumentNullException(nameof(databaseProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rollbackActions = new List<Action>();
        }

        public bool HasActiveTransaction => _currentTransaction != null;

        public async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
            {
                throw new InvalidOperationException("A transaction is already active");
            }

            try
            {
                _currentTransaction = await _databaseProvider.BeginTransactionAsync(cancellationToken);
                _logger.LogDebug("Transaction started");
                return _currentTransaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to begin transaction");
                throw;
            }
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("No active transaction to commit");
            }

            try
            {
                await _currentTransaction.CommitAsync(cancellationToken);
                _logger.LogDebug("Transaction committed successfully");
                ClearTransaction();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to commit transaction");
                await RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("No active transaction to rollback");
            }

            try
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
                _logger.LogDebug("Transaction rolled back successfully");
                
                // Execute rollback actions
                ExecuteRollbackActions();
                ClearTransaction();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rollback transaction");
                throw;
            }
        }

        public void AddRollbackAction(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _rollbackActions.Add(action);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var transactionStarted = false;
            try
            {
                // Start transaction if none exists
                if (!HasActiveTransaction)
                {
                    await BeginTransactionAsync(cancellationToken);
                    transactionStarted = true;
                }

                // Execute the operation
                var result = await operation();

                // Commit if we started the transaction
                if (transactionStarted)
                {
                    await CommitAsync(cancellationToken);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing operation in transaction");
                
                // Rollback if we started the transaction
                if (transactionStarted)
                {
                    await RollbackAsync(cancellationToken);
                }
                
                throw;
            }
        }

        public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            await ExecuteInTransactionAsync(async () =>
            {
                await operation();
                return true; // Dummy return value
            }, cancellationToken);
        }

        private void ExecuteRollbackActions()
        {
            try
            {
                foreach (var action in _rollbackActions)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing rollback action");
                    }
                }
            }
            finally
            {
                _rollbackActions.Clear();
            }
        }

        private void ClearTransaction()
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
            _rollbackActions.Clear();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                ClearTransaction();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Transaction manager interface
    /// </summary>
    public interface ITransactionManager : IDisposable
    {
        /// <summary>
        /// Gets whether there is an active transaction
        /// </summary>
        bool HasActiveTransaction { get; }

        /// <summary>
        /// Begins a new transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Database transaction</returns>
        Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a rollback action to be executed if the transaction is rolled back
        /// </summary>
        /// <param name="action">Action to execute on rollback</param>
        void AddRollbackAction(Action action);

        /// <summary>
        /// Executes an operation within a transaction
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Operation result</returns>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes an operation within a transaction
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
    }
} 