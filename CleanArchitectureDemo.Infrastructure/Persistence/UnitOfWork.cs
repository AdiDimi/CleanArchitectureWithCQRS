using CleanArchitectureDemo.Application.Interfaces;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitectureDemo.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private IDbConnection? _connection;
        private IDbTransaction? _transaction;
        private readonly UserRepository _userRepository;

        public UnitOfWork(IDbConnectionFactory connectionFactory, UserRepository userRepository)
        {
            _connectionFactory = connectionFactory;
            _userRepository = userRepository;
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _connection = _connectionFactory.CreateConnection();
            _connection.Open();
            _transaction = _connection.BeginTransaction();

            // Set transaction on repositories
            _userRepository.SetTransaction(_connection, _transaction);

            return Task.CompletedTask;
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _transaction?.Commit();
            }
            catch
            {
                _transaction?.Rollback();
                throw;
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
            }

            return Task.CompletedTask;
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _transaction?.Rollback();
            }
            finally
            {
                _transaction?.Dispose();
                _transaction = null;
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }
    }
}
