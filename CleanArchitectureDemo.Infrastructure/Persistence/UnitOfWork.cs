using CleanArchitectureDemo.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitectureDemo.Infrastructure.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IDbSessionAccessor _dbSessionAccessor;
        private readonly ILogger<UnitOfWork> _logger;
    
        private DbConnection? _connection;
        private DbTransaction? _transaction;
        private bool _disposed;

        public UnitOfWork(
            IDbConnectionFactory connectionFactory,
            IDbSessionAccessor dbSessionAccessor,
            //IUserRepository userRepository,
            ILogger<UnitOfWork> logger)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _dbSessionAccessor = dbSessionAccessor ?? throw new ArgumentNullException(nameof(dbSessionAccessor));
            //_userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_transaction is not null)
            {
                throw new InvalidOperationException("A transaction is already active.");
            }

            var connection = _connectionFactory.CreateConnection();

            if (connection is not DbConnection dbConnection)
            {
                connection.Dispose();
                throw new InvalidOperationException(
                    $"Connection must inherit from {nameof(DbConnection)} to support async operations.");
            }

            try
            {
                await dbConnection.OpenAsync(cancellationToken);

                var transaction = await dbConnection.BeginTransactionAsync(cancellationToken);

                if (transaction is not DbTransaction dbTransaction)
                {
                    await dbConnection.DisposeAsync();
                    throw new InvalidOperationException("The created transaction is not a DbTransaction.");
                }

                _connection = dbConnection;
                _transaction = dbTransaction;

                _dbSessionAccessor.SetSession(_connection, _transaction);

                _logger.LogInformation("Database transaction started.");
            }
            catch
            {
                await dbConnection.DisposeAsync();
                throw;
            }
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_transaction is null)
            {
                throw new InvalidOperationException("No active transaction to commit.");
            }

            try
            {
                await _transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Database transaction committed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error committing database transaction.");
                throw;
            }
            finally
            {
                await CleanupAsync();
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_transaction is null)
            {
                return;
            }

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning("Database transaction rolled back.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back database transaction.");
                throw;
            }
            finally
            {
                await CleanupAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            await CleanupAsync();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private async Task CleanupAsync()
        {
            _dbSessionAccessor.ClearSession();

            if (_transaction is not null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(UnitOfWork));
            }
        }
    }
}
