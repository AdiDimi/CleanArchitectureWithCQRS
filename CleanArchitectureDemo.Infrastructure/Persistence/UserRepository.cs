using CleanArchitectureDemo.Application.DTOs;
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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CleanArchitectureDemo.Infrastructure.Persistence
{
    public sealed class UserRepository : OracleRepositoryBase, IUserRepository
    {
        public UserRepository(
            IDbConnectionFactory connectionFactory,
            IDbSessionAccessor dbSessionAccessor,
            ILogger<UserRepository> logger)
            : base(connectionFactory, dbSessionAccessor, logger)
        {
        }

        public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var (data, outValues) = await ExecuteOracleStoredProcedureWithCursorAsync<User>(
                OracleProcedures.GetUserById,
                UserParameters.GetUserById(id),
                cancellationToken: cancellationToken);
            return data.SingleOrDefault();
        }

        public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var(data, outValues) =  await ExecuteOracleStoredProcedureWithCursorAsync<User>(
                OracleProcedures.GetAllUsers,
                cancellationToken: cancellationToken);
            return data;
        }

        public Task<int> AddAsync(User user, CancellationToken cancellationToken = default)
        {
   
           return ExecuteStoredProcedureAsync(
                OracleProcedures.InsertUser,
                UserParameters.InsertUser(user.Id, user.Email, user.USER_ID, user.USER_NAME),
                cancellationToken);
        }
        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var (data, outValues) = await ExecuteOracleStoredProcedureWithCursorAsync<User>(
                OracleProcedures.GetUserByEmail,
                UserParameters.GetUserByEmail(email),
                cancellationToken: cancellationToken);
            return data.SingleOrDefault();

        }

   
        public async Task<PagedResult<UserDto>> GetPagedAsync(int pageNumber=1, int pageSize=10,
            CancellationToken cancellationToken = default)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
            if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));

            var offset = (pageNumber - 1) * pageSize;
            var parameters = UserParameters.GetPagedUsers(offset, pageSize);
            return await ExecutePagedProcedureAsync<UserDto>(
                                       procedureName: "PKG_USERS.GET_PAGED",
                                       pageNumber: pageNumber,
                                       pageSize: pageSize,
                                       parameters: parameters,
                                       totalCountParameterName: "o_total_count",
                                       cursorParameterName: "p_cursor",
                                       cancellationToken: cancellationToken);

        }

        public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
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

        public async Task<int> UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("Updating user with ID: {UserId}, Email: {Email}", user.Id, user.Email);

            var result = await ExecuteStoredProcedureAsync(
                OracleProcedures.UpdateUser,
                UserParameters.UpdateUser(user.Id, user.Email),
                cancellationToken);

            Logger.LogInformation("Successfully updated user with ID: {UserId}", user.Id);

            return result;
        }

        public async Task<int> DeleteAsync(User user, CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("Deleting user with ID: {UserId}", user.Id);

            var result = await ExecuteStoredProcedureAsync(
                OracleProcedures.DeleteUser,
                UserParameters.DeleteUser(user.Id),
                cancellationToken);

            Logger.LogInformation("Successfully deleted user with ID: {UserId}", user.Id);

            return result;
        }
    }
}
