using CleanArchitectureDemo.Application.Interfaces;
using CleanArchitectureDemo.Domain.Entities;
using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitectureDemo.Infrastructure.Persistence
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<UserRepository> _logger;
        private IDbConnection? _connection;
        private IDbTransaction? _transaction;

        public UserRepository(IDbConnectionFactory connectionFactory, ILogger<UserRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        // Method to set transaction (called by UnitOfWork)
        public void SetTransaction(IDbConnection connection, IDbTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        private IDbConnection GetConnection()
        {
            return _connection ?? _connectionFactory.CreateConnection();
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = GetConnection();
            var result = await connection.QueryAsync<User>(
                "FN_GET_USER_BY_ID",
                new { p_id = id.ToString() },
                commandType: CommandType.StoredProcedure);
            return result.FirstOrDefault();
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            using var connection = GetConnection();
            var result = await connection.QueryAsync<User>(
                "FN_GET_USER_BY_EMAIL",
                new { p_email = email },
                commandType: CommandType.StoredProcedure);
            return result.FirstOrDefault();
        }

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            using var connection = GetConnection();
            var users = await connection.QueryAsync<User>(
                "FN_GET_ALL_USERS",
                commandType: CommandType.StoredProcedure);
            return users.ToList();
        }

        public async Task<IReadOnlyList<User>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            using var connection = GetConnection();
            var offset = (pageNumber - 1) * pageSize;

            var users = await connection.QueryAsync<User>(
                "FN_GET_PAGED_USERS",
                new { p_offset = offset, p_page_size = pageSize },
                commandType: CommandType.StoredProcedure);
            return users.ToList();
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            using var connection = GetConnection();
            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT FN_USER_EXISTS(:p_id) FROM DUAL",
                new { p_id = id.ToString() });
            return count > 0;
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            using var connection = GetConnection();
            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT FN_EMAIL_EXISTS(:p_email) FROM DUAL",
                new { p_email = email });
            return count > 0;
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            using var connection = GetConnection();
            return await connection.ExecuteScalarAsync<int>("SELECT FN_GET_USER_COUNT() FROM DUAL");
        }

        public void Add(User user)
        {
            _logger.LogInformation("Adding user with ID: {UserId}, Email: {Email}", user.Id, user.Email);
            var connection = _connection ?? throw new InvalidOperationException("Transaction not started");

            connection.Execute(
                "SP_INSERT_USER",
                new { p_id = user.Id.ToString(), p_email = user.Email },
                _transaction,
                commandType: CommandType.StoredProcedure);

            _logger.LogInformation("Successfully added user with ID: {UserId}", user.Id);
        }

        public void Update(User user)
        {
            _logger.LogInformation("Updating user with ID: {UserId}, Email: {Email}", user.Id, user.Email);
            var connection = _connection ?? throw new InvalidOperationException("Transaction not started");

            connection.Execute(
                "SP_UPDATE_USER",
                new { p_id = user.Id.ToString(), p_email = user.Email },
                _transaction,
                commandType: CommandType.StoredProcedure);

            _logger.LogInformation("Successfully updated user with ID: {UserId}", user.Id);
        }

        public void Delete(User user)
        {
            _logger.LogInformation("Deleting user with ID: {UserId}", user.Id);
            var connection = _connection ?? throw new InvalidOperationException("Transaction not started");

            connection.Execute(
                "SP_DELETE_USER",
                new { p_id = user.Id.ToString() },
                _transaction,
                commandType: CommandType.StoredProcedure);

            _logger.LogInformation("Successfully deleted user with ID: {UserId}", user.Id);
        }
    }
}
