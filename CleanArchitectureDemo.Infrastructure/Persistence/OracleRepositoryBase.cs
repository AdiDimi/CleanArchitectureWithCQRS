using CleanArchitectureDemo.Application.DTOs;
using CleanArchitectureDemo.Application.Interfaces;
using CleanArchitectureDemo.Infrastructure.Persistence.Parameters;
using Dapper;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Data.Common;

public abstract class OracleRepositoryBase
{
    protected readonly IDbConnectionFactory ConnectionFactory;
    protected readonly IDbSessionAccessor DbSessionAccessor;
    protected readonly ILogger Logger;

    protected OracleRepositoryBase(
        IDbConnectionFactory connectionFactory,
        IDbSessionAccessor dbSessionAccessor,
        ILogger logger)
    {
        ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        DbSessionAccessor = dbSessionAccessor ?? throw new ArgumentNullException(nameof(dbSessionAccessor));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets whether the repository is currently using a shared connection/transaction
    /// provided by the active Unit of Work session.
    /// </summary>
    protected bool HasSharedSession => DbSessionAccessor.HasActiveTransaction;

    /// <summary>
    /// Returns the current shared connection when a Unit of Work session is active,
    /// otherwise creates a new owned connection.
    /// </summary>
    protected IDbConnection GetConnection()
        => DbSessionAccessor.Connection ?? ConnectionFactory.CreateConnection();

    /// <summary>
    /// Returns the current shared transaction, or null when no transaction is active.
    /// </summary>
    protected IDbTransaction? GetTransaction()
        => DbSessionAccessor.Transaction;

    /// <summary>
    /// Disposes the connection only when it is owned by the repository.
    /// Shared Unit of Work connections must not be disposed here.
    /// </summary>
    protected void DisposeOwnedConnection(IDbConnection connection)
    {
        if (!HasSharedSession)
        {
            connection.Dispose();
        }
    }

    /// <summary>
    /// Opens and returns the current connection as a DbConnection.
    /// Throws when the underlying connection does not support async operations.
    /// </summary>
    protected async Task<DbConnection> GetOpenDbConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = GetConnection();

        if (connection is not DbConnection dbConnection)
        {
            throw new InvalidOperationException(
                $"The connection must inherit from {nameof(DbConnection)}.");
        }

        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }

        return dbConnection;
    }

    /// <summary>
    /// Opens and returns the current connection as an OracleConnection.
    /// Throws when the configured connection is not Oracle.
    /// </summary>
    protected async Task<OracleConnection> GetOpenOracleConnectionAsync(CancellationToken cancellationToken)
    {
        var dbConnection = await GetOpenDbConnectionAsync(cancellationToken);

        if (dbConnection is not OracleConnection oracleConnection)
        {
            throw new InvalidOperationException(
                $"Expected {nameof(OracleConnection)} but got {dbConnection.GetType().Name}.");
        }

        return oracleConnection;
    }

    /// <summary>
    /// Returns distinct DynamicParameters names excluding the specified names.
    /// Useful for filtering cursor or output parameter names out of input parameter lists.
    /// </summary>
    protected static string[] GetDistinctParameterNames(
        DynamicParameters? parameters,
        params string[] excludedNames)
    {
        if (parameters is null)
        {
            return Array.Empty<string>();
        }

        var excluded = new HashSet<string>(
            excludedNames ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        return parameters.ParameterNames
            .Where(p => !excluded.Contains(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Builds an Oracle positional argument list such as ":p_id, :p_name".
    /// Used for scalar Oracle function calls.
    /// </summary>
    protected static string BuildOracleArgumentList(IEnumerable<string>? parameterNames)
    {
        if (parameterNames is null)
        {
            return string.Empty;
        }

        var names = parameterNames.ToArray();

        return names.Length == 0
            ? string.Empty
            : string.Join(", ", names.Select(static p => $":{p}"));
    }

    /// <summary>
    /// Builds an Oracle named argument list such as "p_id => :p_id".
    /// This is safer than positional binding in PL/SQL blocks.
    /// </summary>
    protected static string BuildOracleNamedArgumentList(IEnumerable<string>? parameterNames)
    {
        if (parameterNames is null)
        {
            return string.Empty;
        }

        var names = parameterNames.ToArray();

        return names.Length == 0
            ? string.Empty
            : string.Join(", ", names.Select(static p => $"{p} => :{p}"));
    }

    #region Query Methods

    /// <summary>
    /// Executes a query and returns the first row or default.
    /// Uses the shared transaction when one is active.
    /// </summary>
    protected async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenDbConnectionAsync(cancellationToken);

        try
        {
            return await connection.QueryFirstOrDefaultAsync<T>(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: GetTransaction(),
                    commandType: commandType,
                    cancellationToken: cancellationToken));
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    /// <summary>
    /// Executes a query and returns a single row or default.
    /// Throws if more than one row is returned.
    /// </summary>
    protected async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenDbConnectionAsync(cancellationToken);

        try
        {
            return await connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: GetTransaction(),
                    commandType: commandType,
                    cancellationToken: cancellationToken));
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    /// <summary>
    /// Executes a query and returns all mapped rows.
    /// </summary>
    protected async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenDbConnectionAsync(cancellationToken);

        try
        {
            var result = await connection.QueryAsync<T>(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: GetTransaction(),
                    commandType: commandType,
                    cancellationToken: cancellationToken));

            return result.AsList();
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    /// <summary>
    /// Executes a Dapper two-type multi-mapping query.
    /// splitOn must be the first column name of the second mapped type in the SQL projection.
    /// </summary>
    protected async Task<IReadOnlyList<TReturn>> QueryAsync<T1, T2, TReturn>(
        string sql,
        Func<T1, T2, TReturn> map,
        object? parameters = null,
        string splitOn = "Id",
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenDbConnectionAsync(cancellationToken);

        try
        {
            var result = await connection.QueryAsync(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: GetTransaction(),
                    commandType: commandType,
                    cancellationToken: cancellationToken),
                map,
                splitOn);

            return result.AsList();
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    /// <summary>
    /// Executes a Dapper three-type multi-mapping query.
    /// splitOn must match the SQL projection order.
    /// </summary>
    protected async Task<IReadOnlyList<TReturn>> QueryAsync<T1, T2, T3, TReturn>(
        string sql,
        Func<T1, T2, T3, TReturn> map,
        object? parameters = null,
        string splitOn = "Id",
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenDbConnectionAsync(cancellationToken);

        try
        {
            var result = await connection.QueryAsync(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: GetTransaction(),
                    commandType: commandType,
                    cancellationToken: cancellationToken),
                map,
                splitOn);

            return result.AsList();
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    /// <summary>
    /// Executes a multi-result SQL batch and returns two typed result sets.
    /// Keep this only for direct SQL multi-result scenarios.
    /// </summary>
    protected async Task<(IReadOnlyList<T1> Set1, IReadOnlyList<T2> Set2)> QueryMultipleAsync<T1, T2>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenDbConnectionAsync(cancellationToken);

        try
        {
            using var grid = await connection.QueryMultipleAsync(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: GetTransaction(),
                    commandType: commandType,
                    cancellationToken: cancellationToken));

            var set1 = (await grid.ReadAsync<T1>()).AsList();
            var set2 = (await grid.ReadAsync<T2>()).AsList();

            return (set1, set2);
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    #endregion

    #region Command Methods

    /// <summary>
    /// Executes a non-query command and returns the affected row count.
    /// Logs failures with SQL and command type context.
    /// </summary>
    protected async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenDbConnectionAsync(cancellationToken);

        try
        {
            return await connection.ExecuteAsync(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: GetTransaction(),
                    commandType: commandType,
                    cancellationToken: cancellationToken));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error executing command. Sql: {Sql}, CommandType: {CommandType}, Parameters: {@Parameters}",
                sql, commandType, parameters);
            throw;
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    /// <summary>
    /// Executes a stored procedure command and returns affected rows.
    /// Intended for insert, update, delete, and command-style procedures.
    /// </summary>
    protected Task<int> ExecuteStoredProcedureAsync(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            procedureName,
            parameters,
            CommandType.StoredProcedure,
            cancellationToken);
    }

    /// <summary>
    /// Executes a scalar query and returns a single value.
    /// Useful for count, existence, and lookup queries.
    /// </summary>
    protected async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenDbConnectionAsync(cancellationToken);

        try
        {
            return await connection.ExecuteScalarAsync<T?>(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: GetTransaction(),
                    commandType: commandType,
                    cancellationToken: cancellationToken));
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    #endregion

    #region Oracle Function Helpers

    /// <summary>
    /// Executes an Oracle scalar function using SELECT ... FROM DUAL.
    /// Suitable for scalar return values only.
    /// </summary>
    protected async Task<T?> ExecuteOracleFunctionAsync<T>(
        string functionName,
        DynamicParameters? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var parameterNames = GetDistinctParameterNames(parameters);
        var argumentList = BuildOracleArgumentList(parameterNames);

        var sql = string.IsNullOrWhiteSpace(argumentList)
            ? $"SELECT {functionName} FROM DUAL"
            : $"SELECT {functionName}({argumentList}) FROM DUAL";

        return await ExecuteScalarAsync<T>(sql, parameters, CommandType.Text, cancellationToken);
    }

    /// <summary>
    /// Executes an Oracle function that returns SYS_REFCURSOR.
    /// Uses a PL/SQL block with a REF CURSOR return value.
    /// </summary>
    /// <remarks>
    /// Dapper Oracle type handlers should be registered at startup for reliable conversion
    /// of provider-specific values such as OracleDecimal, OracleDate, and OracleTimeStamp.
    /// </remarks>
    protected async Task<IReadOnlyList<T>> ExecuteOracleFunctionWithCursorAsync<T>(
        string functionName,
        DynamicParameters? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenOracleConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.BindByName = true;
            command.CommandType = CommandType.Text;
            command.Transaction = GetTransaction() as OracleTransaction;

            var parameterNames = GetDistinctParameterNames(parameters, "result");
            var namedArguments = BuildOracleNamedArgumentList(parameterNames);

            command.CommandText = string.IsNullOrWhiteSpace(namedArguments)
                ? $"BEGIN :result := {functionName}; END;"
                : $"BEGIN :result := {functionName}({namedArguments}); END;";

            AddOracleParameters(command, parameters, parameterNames);

            command.Parameters.Add(new OracleParameter
            {
                ParameterName = "result",
                OracleDbType = OracleDbType.RefCursor,
                Direction = ParameterDirection.ReturnValue
            });

            await command.ExecuteNonQueryAsync(cancellationToken);

            var resultParameter = (OracleParameter)command.Parameters["result"];
            return await ReadRefCursorAsync<T>(resultParameter, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error executing Oracle function {FunctionName} with REF CURSOR. Parameters: {@Parameters}",
                functionName, parameters);
            throw;
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    /// <summary>
    /// Executes an Oracle stored procedure with an OUT SYS_REFCURSOR parameter
    /// and optional additional OUT parameters.
    /// </summary>
    protected async Task<(IReadOnlyList<T> Data, Dictionary<string, object?> OutParams)>
        ExecuteOracleStoredProcedureWithCursorAsync<T>(
            string procedureName,
            DynamicParameters? parameters = null,
            IEnumerable<OracleParameterSpec>? outParameters = null,
            string cursorParameterName = "p_cursor",
            CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenOracleConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.BindByName = true;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;
            command.Transaction = GetTransaction() as OracleTransaction;

            var parameterNames = GetDistinctParameterNames(parameters, cursorParameterName);
            AddOracleParameters(command, parameters, parameterNames);

            if (outParameters is not null)
            {
                foreach (var spec in outParameters)
                {
                    command.Parameters.Add(CreateOracleParameter(spec));
                }
            }

            command.Parameters.Add(new OracleParameter
            {
                ParameterName = cursorParameterName,
                OracleDbType = OracleDbType.RefCursor,
                Direction = ParameterDirection.Output
            });

            await command.ExecuteNonQueryAsync(cancellationToken);

            var cursorParameter = (OracleParameter)command.Parameters[cursorParameterName];
            var data = await ReadRefCursorAsync<T>(cursorParameter, cancellationToken);

            var outValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (OracleParameter param in command.Parameters)
            {
                if ((param.Direction == ParameterDirection.Output ||
                     param.Direction == ParameterDirection.InputOutput) &&
                    param.OracleDbType != OracleDbType.RefCursor)
                {
                    outValues[param.ParameterName] =
                        param.Value == DBNull.Value ? null : param.Value;
                }
            }

            return (data, outValues);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error executing Oracle procedure {ProcedureName} with REF CURSOR. Parameters: {@Parameters}",
                procedureName, parameters);
            throw;
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    /// <summary>
    /// Executes a paging stored procedure that returns both a REF CURSOR and an OUT total count.
    /// </summary>
    protected async Task<PagedResult<T>> ExecutePagedProcedureAsync<T>(
        string procedureName,
        int pageNumber,
        int pageSize,
        DynamicParameters? parameters = null,
        string totalCountParameterName = "o_total_count",
        string cursorParameterName = "p_cursor",
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be >= 1.");
        }

        var execParameters = parameters is null
            ? new DynamicParameters()
            : new DynamicParameters(parameters);

        var outParameters = new[]
        {
            OracleParameterFactory.OutNumber(totalCountParameterName)
        };

        var (data, outValues) = await ExecuteOracleStoredProcedureWithCursorAsync<T>(
            procedureName,
            execParameters,
            outParameters,
            cursorParameterName,
            cancellationToken);

        var totalCount = GetRequiredOutInt32(outValues, totalCountParameterName);

        return new PagedResult<T>
        {
            Items = data,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    #endregion

    #region Oracle Parameter / Cursor Helpers

    /// <summary>
    /// Returns a required OUT parameter as Int32.
    /// Supports OracleDecimal and common numeric CLR types.
    /// </summary>
    protected static int GetRequiredOutInt32(
        IReadOnlyDictionary<string, object?> outValues,
        string parameterName)
    {
        if (!outValues.TryGetValue(parameterName, out var value) || value is null)
        {
            throw new InvalidOperationException(
                $"Required OUT parameter '{parameterName}' was not returned.");
        }

        return value switch
        {
            int i => i,
            long l => checked((int)l),
            OracleDecimal od => od.ToInt32(),
            decimal d => decimal.ToInt32(d),
            short s => s,
            byte b => b,
            _ => Convert.ToInt32(value)
        };
    }

    /// <summary>
    /// Returns an optional OUT parameter as Int32 when present.
    /// </summary>
    protected static int? GetOptionalOutInt32(
        IReadOnlyDictionary<string, object?> outValues,
        string parameterName)
    {
        if (!outValues.TryGetValue(parameterName, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int i => i,
            long l => checked((int)l),
            OracleDecimal od => od.ToInt32(),
            decimal d => decimal.ToInt32(d),
            short s => s,
            byte b => b,
            _ => Convert.ToInt32(value)
        };
    }

    /// <summary>
    /// Returns an optional OUT parameter as string when present.
    /// </summary>
    protected static string? GetOptionalOutString(
        IReadOnlyDictionary<string, object?> outValues,
        string parameterName)
    {
        if (!outValues.TryGetValue(parameterName, out var value) || value is null)
        {
            return null;
        }

        return Convert.ToString(value);
    }

    /// <summary>
    /// Adds Oracle parameters from the supplied DynamicParameters collection.
    /// Supports both OracleParameterSpec and inferred OracleParameter creation.
    /// </summary>
    protected virtual void AddOracleParameters(
        OracleCommand command,
        DynamicParameters? parameters,
        IEnumerable<string> parameterNames)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var name in parameterNames)
        {
            var value = parameters.Get<object?>(name);

            OracleParameter parameter = value is OracleParameterSpec spec
                ? CreateOracleParameter(spec)
                : InferOracleParameter(name, value);

            command.Parameters.Add(parameter);
        }
    }

    /// <summary>
    /// Creates a strongly typed OracleParameter from an OracleParameterSpec.
    /// Use this for explicit OracleDbType, size, precision, scale, and output parameters.
    /// </summary>
    protected virtual OracleParameter CreateOracleParameter(OracleParameterSpec spec)
    {
        var parameter = new OracleParameter
        {
            ParameterName = spec.Name,
            Direction = spec.Direction,
            IsNullable = spec.IsNullable,
            Value = spec.Value ?? DBNull.Value
        };

        if (spec.OracleDbType.HasValue)
        {
            parameter.OracleDbType = spec.OracleDbType.Value;
        }

        if (spec.DbType.HasValue)
        {
            parameter.DbType = spec.DbType.Value;
        }

        if (spec.Size.HasValue)
        {
            parameter.Size = spec.Size.Value;
        }

        if (spec.Precision.HasValue)
        {
            parameter.Precision = spec.Precision.Value;
        }

        if (spec.Scale.HasValue)
        {
            parameter.Scale = spec.Scale.Value;
        }

        if (spec.CollectionType.HasValue)
        {
            parameter.CollectionType = spec.CollectionType.Value;
        }

        if (spec.ArrayBindCount.HasValue)
        {
            parameter.Size = spec.ArrayBindCount.Value;
        }

        if (!string.IsNullOrWhiteSpace(spec.SourceColumn))
        {
            parameter.SourceColumn = spec.SourceColumn;
        }

        return parameter;
    }

    /// <summary>
    /// Creates an OracleParameter by inferring the Oracle type from the CLR value.
    /// This is intended for common simple input parameters only.
    /// </summary>
    protected virtual OracleParameter InferOracleParameter(string name, object? value)
    {
        var parameter = new OracleParameter
        {
            ParameterName = name,
            Direction = ParameterDirection.Input,
            Value = value ?? DBNull.Value
        };

        if (value is null)
        {
            return parameter;
        }

        switch (value)
        {
            case string s:
                parameter.OracleDbType = OracleDbType.Varchar2;
                parameter.Size = Math.Max(1, s.Length);
                break;

            case int:
                parameter.OracleDbType = OracleDbType.Int32;
                break;

            case long:
                parameter.OracleDbType = OracleDbType.Int64;
                break;

            case decimal:
                parameter.OracleDbType = OracleDbType.Decimal;
                break;

            case DateTime:
                parameter.OracleDbType = OracleDbType.TimeStamp;
                break;

            case byte[] bytes:
                parameter.OracleDbType = OracleDbType.Raw;
                parameter.Size = bytes.Length;
                break;

            case Guid guid:
                parameter.OracleDbType = OracleDbType.Varchar2;
                parameter.Value = guid.ToString("D");
                parameter.Size = 36;
                break;
        }

        return parameter;
    }

    /// <summary>
    /// Reads rows from an Oracle REF CURSOR and maps them to the target type.
    /// </summary>
    /// <remarks>
    /// This method relies on Dapper row parsing. Oracle-specific provider values such as
    /// OracleDecimal should be supported through registered Dapper type handlers.
    /// </remarks>
    protected async Task<IReadOnlyList<T>> ReadRefCursorAsync<T>(
        OracleParameter cursorParameter,
        CancellationToken cancellationToken)
    {
        if (cursorParameter.Value is not OracleRefCursor refCursor)
        {
            return Array.Empty<T>();
        }

        using var reader = refCursor.GetDataReader();
        var parser = reader.GetRowParser<T>();
        var results = new List<T>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(parser(reader));
        }

        return results;
    }

    #endregion

    #region Convenience Helpers

    /// <summary>
    /// Executes a paged direct SQL query using OFFSET/FETCH.
    /// Keep this only when direct SQL paging is still needed in repositories.
    /// </summary>
    protected async Task<IReadOnlyList<T>> QueryPagedAsync<T>(
        string baseSql,
        string orderByClause,
        int pageNumber,
        int pageSize,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be >= 1.");
        }

        var offset = (pageNumber - 1) * pageSize;

        var sql = $"""
            SELECT *
            FROM (
                {baseSql}
            )
            {orderByClause}
            OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY
            """;

        var dynamicParameters = new DynamicParameters(parameters);
        dynamicParameters.Add("offset", offset);
        dynamicParameters.Add("pageSize", pageSize);

        return await QueryAsync<T>(sql, dynamicParameters, CommandType.Text, cancellationToken);
    }

    #endregion
}
