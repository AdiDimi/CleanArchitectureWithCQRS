using CleanArchitectureDemo.Application.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Data.Common;

public abstract class OracleRepositoryBase
{
    protected readonly IDbConnectionFactory ConnectionFactory;
    protected readonly ILogger Logger;

    protected IDbConnection? Connection;
    protected IDbTransaction? Transaction;

    protected OracleRepositoryBase(
        IDbConnectionFactory connectionFactory,
        ILogger logger)
    {
        ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual void SetTransaction(IDbConnection connection, IDbTransaction transaction)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    protected bool HasExternalTransaction => Connection is not null;

    protected IDbConnection GetConnection()
        => Connection ?? ConnectionFactory.CreateConnection();

    protected async Task<DbConnection> GetOpenDbConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = GetConnection();

        if (connection is not DbConnection dbConnection)
        {
            throw new InvalidOperationException(
                $"Connection must inherit from {nameof(DbConnection)}.");
        }

        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }

        return dbConnection;
    }

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

    protected void DisposeOwnedConnection(IDbConnection connection)
    {
        if (!HasExternalTransaction)
        {
            connection.Dispose();
        }
    }

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

    protected static string[] GetDistinctParameterNames(DynamicParameters? parameters, params string[] excludedNames)
    {
        if (parameters is null)
        {
            return Array.Empty<string>();
        }

        var excluded = new HashSet<string>(excludedNames ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        return parameters.ParameterNames
            .Where(p => !excluded.Contains(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    protected static DynamicParameters CloneParameters(DynamicParameters? source)
    {
        var clone = new DynamicParameters();

        if (source is null)
        {
            return clone;
        }

        foreach (var name in source.ParameterNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            clone.Add(name, source.Get<object?>(name));
        }

        return clone;
    }

    #region Query Methods

    protected async Task<T?> QueryFirstOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();

        try
        {
            if (connection is DbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }

            return await connection.QueryFirstOrDefaultAsync<T>(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: Transaction,
                    commandType: commandType,
                    cancellationToken: cancellationToken));
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    protected async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();

        try
        {
            if (connection is DbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }

            return await connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: Transaction,
                    commandType: commandType,
                    cancellationToken: cancellationToken));
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    protected async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();

        try
        {
            if (connection is DbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }

            var result = await connection.QueryAsync<T>(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: Transaction,
                    commandType: commandType,
                    cancellationToken: cancellationToken));

            return result.AsList();
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    protected async Task<(IReadOnlyList<T1> Set1, IReadOnlyList<T2> Set2)> QueryMultipleAsync<T1, T2>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();

        try
        {
            if (connection is DbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }

            using var grid = await connection.QueryMultipleAsync(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: Transaction,
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

    protected async Task<(IReadOnlyList<T1> Set1, IReadOnlyList<T2> Set2, IReadOnlyList<T3> Set3)> QueryMultipleAsync<T1, T2, T3>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();

        try
        {
            if (connection is DbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }

            using var grid = await connection.QueryMultipleAsync(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: Transaction,
                    commandType: commandType,
                    cancellationToken: cancellationToken));

            var set1 = (await grid.ReadAsync<T1>()).AsList();
            var set2 = (await grid.ReadAsync<T2>()).AsList();
            var set3 = (await grid.ReadAsync<T3>()).AsList();

            return (set1, set2, set3);
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    protected async Task<IReadOnlyList<TReturn>> QueryAsync<T1, T2, TReturn>(
        string sql,
        Func<T1, T2, TReturn> map,
        object? parameters = null,
        string splitOn = "Id",
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();

        try
        {
            if (connection is DbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }

            var result = await connection.QueryAsync(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: Transaction,
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

    protected async Task<IReadOnlyList<TReturn>> QueryAsync<T1, T2, T3, TReturn>(
        string sql,
        Func<T1, T2, T3, TReturn> map,
        object? parameters = null,
        string splitOn = "Id",
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();

        try
        {
            if (connection is DbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }

            var result = await connection.QueryAsync(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: Transaction,
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

    #endregion

    #region Command Methods

    protected async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();

        try
        {
            if (connection is DbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }

            return await connection.ExecuteAsync(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: Transaction,
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

    #endregion

    #region Stored Procedure Query Helpers

    protected Task<T?> QueryStoredProcedureFirstOrDefaultAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return QueryFirstOrDefaultAsync<T>(
            procedureName,
            parameters,
            CommandType.StoredProcedure,
            cancellationToken);
    }

    protected Task<T?> QueryStoredProcedureSingleOrDefaultAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return QuerySingleOrDefaultAsync<T>(
            procedureName,
            parameters,
            CommandType.StoredProcedure,
            cancellationToken);
    }

    protected Task<IReadOnlyList<T>> QueryStoredProcedureAsync<T>(
        string procedureName,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return QueryAsync<T>(
            procedureName,
            parameters,
            CommandType.StoredProcedure,
            cancellationToken);
    }

    #endregion

    #region Oracle Scalar Function

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

    protected async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.Text,
        CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();

        try
        {
            if (connection is DbConnection dbConnection && dbConnection.State != ConnectionState.Open)
            {
                await dbConnection.OpenAsync(cancellationToken);
            }

            return await connection.ExecuteScalarAsync<T?>(
                new CommandDefinition(
                    commandText: sql,
                    parameters: parameters,
                    transaction: Transaction,
                    commandType: commandType,
                    cancellationToken: cancellationToken));
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    #endregion

    #region Oracle REF CURSOR Function

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
            command.Transaction = Transaction as OracleTransaction;

            var parameterNames = GetDistinctParameterNames(parameters, "result");
            var namedArguments = BuildOracleNamedArgumentList(parameterNames);

            command.CommandText = string.IsNullOrWhiteSpace(namedArguments)
                ? $"BEGIN :result := {functionName}; END;"
                : $"BEGIN :result := {functionName}({namedArguments}); END;";

            AddOracleInputParameters(command, parameters, parameterNames);

            var resultParameter = new OracleParameter
            {
                ParameterName = "result",
                OracleDbType = OracleDbType.RefCursor,
                Direction = ParameterDirection.ReturnValue
            };

            command.Parameters.Add(resultParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return await ReadRefCursorAsync<T>(resultParameter, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error executing Oracle function cursor {FunctionName} with parameters {@Parameters}",
                functionName, parameters);
            throw;
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    #endregion

    #region Oracle REF CURSOR Stored Procedure

    protected async Task<IReadOnlyList<T>> ExecuteOracleStoredProcedureWithCursorAsync<T>(
        string procedureName,
        DynamicParameters? parameters = null,
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
            command.Transaction = Transaction as OracleTransaction;

            var parameterNames = GetDistinctParameterNames(parameters, cursorParameterName);
            AddOracleInputParameters(command, parameters, parameterNames);

            var cursorParameter = new OracleParameter
            {
                ParameterName = cursorParameterName,
                OracleDbType = OracleDbType.RefCursor,
                Direction = ParameterDirection.Output
            };

            command.Parameters.Add(cursorParameter);

            await command.ExecuteNonQueryAsync(cancellationToken);

            return await ReadRefCursorAsync<T>(cursorParameter, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Error executing Oracle procedure cursor {ProcedureName} with parameters {@Parameters}",
                procedureName, parameters);
            throw;
        }
        finally
        {
            DisposeOwnedConnection(connection);
        }
    }

    #endregion

    #region Oracle Helpers

    protected virtual void AddOracleInputParameters(
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

            var parameter = new OracleParameter
            {
                ParameterName = name,
                Value = value ?? DBNull.Value,
                Direction = ParameterDirection.Input
            };

            command.Parameters.Add(parameter);
        }
    }

    protected async Task<IReadOnlyList<T>> ReadRefCursorAsync<T>(
        OracleParameter cursorParameter,
        CancellationToken cancellationToken)
    {
        if (cursorParameter.Value is not OracleRefCursor refCursor)
        {
            return Array.Empty<T>();
        }

        await using var reader = refCursor.GetDataReader();

        var parser = reader.GetRowParser<T>();
        var results = new List<T>();

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(parser(reader));
        }

        return results;
    }

    #endregion

    #region Paging Helpers

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

    #region Convenience Helpers

    protected async Task<bool> ExistsAsync(
        string tableName,
        string whereClause,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var sql = $"SELECT COUNT(1) FROM {tableName} WHERE {whereClause}";
        var count = await ExecuteScalarAsync<int>(sql, parameters, CommandType.Text, cancellationToken);
        return count > 0;
    }

    protected async Task<int> GetCountAsync(
        string tableName,
        string? whereClause = null,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var sql = string.IsNullOrWhiteSpace(whereClause)
            ? $"SELECT COUNT(*) FROM {tableName}"
            : $"SELECT COUNT(*) FROM {tableName} WHERE {whereClause}";

        return await ExecuteScalarAsync<int>(sql, parameters, CommandType.Text, cancellationToken);
    }

    #endregion
}