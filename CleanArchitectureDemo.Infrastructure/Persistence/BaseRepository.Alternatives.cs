using CleanArchitectureDemo.Application.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitectureDemo.Infrastructure.Persistence
{
    /// <summary>
    /// ALTERNATIVE VERSION: BaseRepository with reflection caching for better performance.
    /// Use this if ExecuteOracleFunctionAsync is called frequently.
    /// </summary>
    public abstract class BaseRepositoryOptimized
    {
        protected readonly IDbConnectionFactory ConnectionFactory;
        protected readonly ILogger Logger;
        protected IDbConnection? Connection;
        protected IDbTransaction? Transaction;

        // Cache for reflection results (property names by type)
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

        protected BaseRepositoryOptimized(IDbConnectionFactory connectionFactory, ILogger logger)
        {
            ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes an Oracle function that returns a scalar value.
        /// OPTIMIZED VERSION: Caches reflection results.
        /// </summary>
        protected async Task<T> ExecuteOracleFunctionAsync<T>(
            string functionName,
            object? parameters = null,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT {functionName}";
            
            if (parameters != null)
            {
                // Get properties from cache or add to cache
                var props = PropertyCache.GetOrAdd(
                    parameters.GetType(), 
                    type => type.GetProperties());
                
                var paramNames = string.Join(", ", props.Select(p => $":{p.Name}"));
                sql += $"({paramNames})";
            }
            
            sql += " FROM DUAL";

            return await ExecuteScalarAsync<T>(sql, parameters, CommandType.Text, cancellationToken);
        }

        // Helper method to get cached properties
        protected static PropertyInfo[] GetCachedProperties(Type type)
        {
            return PropertyCache.GetOrAdd(type, t => t.GetProperties());
        }

        #region Other BaseRepository methods (same as before)
        // ... QueryAsync, ExecuteScalarAsync, etc.
        #endregion

        private async Task<T> ExecuteScalarAsync<T>(string sql, object? parameters, CommandType commandType, CancellationToken cancellationToken)
        {
            // Implementation here
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ALTERNATIVE VERSION: Completely avoid reflection using explicit parameter names.
    /// Most performant but less convenient API.
    /// </summary>
    public abstract class BaseRepositoryNoReflection
    {
        /// <summary>
        /// Executes an Oracle function with explicit parameter names (no reflection).
        /// </summary>
        protected async Task<T> ExecuteOracleFunctionAsync<T>(
            string functionName,
            string[] parameterNames,
            object parameters,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT {functionName}";
            
            if (parameterNames?.Length > 0)
            {
                var paramList = string.Join(", ", parameterNames.Select(p => $":{p}"));
                sql += $"({paramList})";
            }
            
            sql += " FROM DUAL";

            return await ExecuteScalarAsync<T>(sql, parameters, CommandType.Text, cancellationToken);
        }

        // Usage example:
        // await ExecuteOracleFunctionAsync<int>(
        //     "FN_EMAIL_EXISTS", 
        //     new[] { "p_email" },
        //     new { p_email = email }, 
        //     ct);

        private async Task<T> ExecuteScalarAsync<T>(string sql, object? parameters, CommandType commandType, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
