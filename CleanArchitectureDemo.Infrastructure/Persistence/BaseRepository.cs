using CleanArchitectureDemo.Application.Interfaces;
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
    /// <summary>
    /// Base repository providing common data access operations using Dapper with Oracle.
    /// Supports transactions via UnitOfWork pattern and full CancellationToken support.
    /// </summary>
    public abstract class BaseRepository
    {
        protected readonly IDbConnectionFactory ConnectionFactory;
        protected readonly ILogger Logger;
        protected IDbConnection? Connection;
        protected IDbTransaction? Transaction;

        protected BaseRepository(IDbConnectionFactory connectionFactory, ILogger logger)
        {
            ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sets the transaction context (called by UnitOfWork).
        /// </summary>
        public virtual void SetTransaction(IDbConnection connection, IDbTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        /// <summary>
        /// Gets the current connection (transaction connection or creates new one).
        /// </summary>
        protected IDbConnection GetConnection()
        {
            return Connection ?? ConnectionFactory.CreateConnection();
        }

        #region Query Methods (Async with CancellationToken)

        /// <summary>
        /// Executes a query and returns a single result or default.
        /// </summary>
        protected async Task<T?> QueryFirstOrDefaultAsync<T>(
            string sql,
            DynamicParameters? parameters = null,
            CommandType commandType = CommandType.Text,
            CancellationToken cancellationToken = default)
        {
            using var connection = GetConnection();
            
            var result = await connection.QueryAsync<T>(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    commandType: commandType,
                    cancellationToken: cancellationToken));
            
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Executes a query and returns a list of results.
        /// </summary>
        protected async Task<IReadOnlyList<T>> QueryAsync<T>(
            string sql,
            DynamicParameters? parameters = null,
            CommandType commandType = CommandType.Text,
            CancellationToken cancellationToken = default)
        {
            using var connection = GetConnection();
            
            var result = await connection.QueryAsync<T>(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    commandType: commandType,
                    cancellationToken: cancellationToken));
            
            return result.ToList();
        }

        /// <summary>
        /// Executes a scalar query (COUNT, SUM, etc.) and returns single value.
        /// </summary>
        protected async Task<T> ExecuteScalarAsync<T>(
            string sql,
            DynamicParameters? parameters = null,
            CommandType commandType = CommandType.Text,
            CancellationToken cancellationToken = default)
        {
            using var connection = GetConnection();
            
            return await connection.ExecuteScalarAsync<T>(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    commandType: commandType,
                    cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Executes a query with multi-mapping (joins).
        /// </summary>
        protected async Task<IReadOnlyList<TReturn>> QueryAsync<T1, T2, TReturn>(
            string sql,
            Func<T1, T2, TReturn> map,
            DynamicParameters? parameters = null,
            string splitOn = "Id",
            CommandType commandType = CommandType.Text,
            CancellationToken cancellationToken = default)
        {
            using var connection = GetConnection();
            
            var result = await connection.QueryAsync(
                sql,
                map,
                parameters,
                splitOn: splitOn,
                commandType: commandType);
            
            return result.ToList();
        }

        /// <summary>
        /// Executes a query with three-way multi-mapping.
        /// </summary>
        protected async Task<IReadOnlyList<TReturn>> QueryAsync<T1, T2, T3, TReturn>(
            string sql,
            Func<T1, T2, T3, TReturn> map,
            DynamicParameters? parameters = null,
            string splitOn = "Id",
            CommandType commandType = CommandType.Text,
            CancellationToken cancellationToken = default)
        {
            using var connection = GetConnection();
            
            var result = await connection.QueryAsync(
                sql,
                map,
                parameters,
                splitOn: splitOn,
                commandType: commandType);
            
            return result.ToList();
        }

        #endregion

        #region Command Methods (Synchronous - within transaction)

        /// <summary>
        /// Executes a command (INSERT, UPDATE, DELETE) and returns affected rows.
        /// Must be called within a transaction context.
        /// </summary>
        protected int Execute(
            string sql,
            DynamicParameters? parameters = null,
            CommandType commandType = CommandType.Text)
        {
            var connection = Connection ?? throw new InvalidOperationException(
                "Transaction not started. Command operations must be executed within a transaction.");

            try
            {
                return connection.Execute(
                    sql,
                    parameters,
                    Transaction,
                    commandType: commandType);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error executing command: {Sql} with parameters: {Parameters}", 
                    sql, parameters);
                throw;
            }
        }

        /// <summary>
        /// Executes a stored procedure (INSERT, UPDATE, DELETE) and returns affected rows.
        /// Must be called within a transaction context.
        /// </summary>
        protected int ExecuteStoredProcedure(
            string procedureName,
            DynamicParameters? parameters = null)
        {
            return Execute(procedureName, parameters, CommandType.StoredProcedure);
        }

        #endregion

        #region Stored Procedure Helpers

        /// <summary>
        /// Executes a stored procedure query that returns a single result.
        /// </summary>
        protected async Task<T?> QueryStoredProcedureFirstOrDefaultAsync<T>(
            string procedureName,
            DynamicParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            return await QueryFirstOrDefaultAsync<T>(
                procedureName,
                parameters,
                CommandType.StoredProcedure,
                cancellationToken);
        }

        /// <summary>
        /// Executes a stored procedure query that returns a list of results.
        /// </summary>
        protected async Task<IReadOnlyList<T>> QueryStoredProcedureAsync<T>(
            string procedureName,
            DynamicParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            return await QueryAsync<T>(
                procedureName,
                parameters,
                CommandType.StoredProcedure,
                cancellationToken);
        }

        /// <summary>
        /// Executes an Oracle function that returns a scalar value.
        /// Uses DynamicParameters for compile-time safety (no reflection).
        /// </summary>
        protected async Task<T> ExecuteOracleFunctionAsync<T>(
            string functionName,
            DynamicParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            // Oracle functions are called like: SELECT FUNCTION_NAME(params) FROM DUAL
            var sql = $"SELECT {functionName}";

            if (parameters != null)
            {
                // Use DynamicParameters.ParameterNames instead of reflection
                var paramNames = string.Join(", ", parameters.ParameterNames.Select(p => $":{p}"));
                sql += $"({paramNames})";
            }

            sql += " FROM DUAL";

            return await ExecuteScalarAsync<T>(sql, parameters, CommandType.Text, cancellationToken);
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Executes multiple commands in a batch (must be within transaction).
        /// </summary>
        protected int ExecuteBatch(IEnumerable<(string Sql, object Parameters)> commands)
        {
            var connection = Connection ?? throw new InvalidOperationException(
                "Transaction not started. Batch operations must be executed within a transaction.");

            int totalAffected = 0;

            foreach (var (sql, parameters) in commands)
            {
                totalAffected += connection.Execute(sql, parameters, Transaction);
            }

            return totalAffected;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if a record exists.
        /// </summary>
        protected async Task<bool> ExistsAsync(
            string tableName,
            string whereClause,
            DynamicParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT COUNT(1) FROM {tableName} WHERE {whereClause}";
            var count = await ExecuteScalarAsync<int>(sql, parameters, CommandType.Text, cancellationToken);
            return count > 0;
        }

        /// <summary>
        /// Gets the count of records.
        /// </summary>
        protected async Task<int> GetCountAsync(
            string tableName,
            string? whereClause = null,
            DynamicParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT COUNT(*) FROM {tableName}";
            
            if (!string.IsNullOrEmpty(whereClause))
            {
                sql += $" WHERE {whereClause}";
            }

            return await ExecuteScalarAsync<int>(sql, parameters, CommandType.Text, cancellationToken);
        }

        #endregion
    }
}
