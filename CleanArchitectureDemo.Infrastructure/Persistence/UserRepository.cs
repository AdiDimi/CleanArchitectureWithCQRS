using CleanArchitectureDemo.Application.Interfaces;
using CleanArchitectureDemo.Domain.Entities;
using CleanArchitectureDemo.Infrastructure.Persistence.Constants;
using CleanArchitectureDemo.Infrastructure.Persistence.Parameters;
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
    public class UserRepository : BaseRepository, IUserRepository
    {
        public UserRepository(IDbConnectionFactory connectionFactory, ILogger<UserRepository> logger)
            : base(connectionFactory, logger)
        {
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await QueryStoredProcedureFirstOrDefaultAsync<User>(
                OracleProcedures.GetUserById,
                UserParameters.GetUserById(id),
                cancellationToken);
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await QueryStoredProcedureFirstOrDefaultAsync<User>(
                OracleProcedures.GetUserByEmail,
                UserParameters.GetUserByEmail(email),
                cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await QueryStoredProcedureAsync<User>(
                OracleProcedures.GetAllUsers,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyList<User>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var offset = (pageNumber - 1) * pageSize;

            return await QueryStoredProcedureAsync<User>(
                OracleProcedures.GetPagedUsers,
                UserParameters.GetPagedUsers(offset, pageSize),
                cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var count = await ExecuteOracleFunctionAsync<int>(
                OracleProcedures.UserExists,
                UserParameters.UserExists(id),
                cancellationToken);

            return count > 0;
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            var count = await ExecuteOracleFunctionAsync<int>(
                OracleProcedures.EmailExists,
                UserParameters.EmailExists(email),
                cancellationToken);

            return count > 0;
        }

        public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteOracleFunctionAsync<int>(
                OracleProcedures.GetUserCount,
                cancellationToken: cancellationToken);
        }

        public void Add(User user)
        {
            Logger.LogInformation("Adding user with ID: {UserId}, Email: {Email}", user.Id, user.Email);

            ExecuteStoredProcedure(
                OracleProcedures.InsertUser,
                UserParameters.InsertUser(user.Id, user.Email));

            Logger.LogInformation("Successfully added user with ID: {UserId}", user.Id);
        }

        public void Update(User user)
        {
            Logger.LogInformation("Updating user with ID: {UserId}, Email: {Email}", user.Id, user.Email);

            ExecuteStoredProcedure(
                OracleProcedures.UpdateUser,
                UserParameters.UpdateUser(user.Id, user.Email));

            Logger.LogInformation("Successfully updated user with ID: {UserId}", user.Id);
        }

        public void Delete(User user)
        {
            Logger.LogInformation("Deleting user with ID: {UserId}", user.Id);

            ExecuteStoredProcedure(
                OracleProcedures.DeleteUser,
                UserParameters.DeleteUser(user.Id));

            Logger.LogInformation("Successfully deleted user with ID: {UserId}", user.Id);
        }
    }
}
