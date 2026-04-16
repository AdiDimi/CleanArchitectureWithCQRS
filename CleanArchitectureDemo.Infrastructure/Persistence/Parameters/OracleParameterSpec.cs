
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace CleanArchitectureDemo.Infrastructure.Persistence.Parameters;

public sealed record OracleParameterSpec
{
    public required string Name { get; init; }
    public object? Value { get; init; }
    public ParameterDirection Direction { get; init; } = ParameterDirection.Input;

    public OracleDbType? OracleDbType { get; init; }
    public DbType? DbType { get; init; }

    public int? Size { get; init; }
    public byte? Precision { get; init; }
    public byte? Scale { get; init; }

    public bool IsNullable { get; init; } = true;

    // For associative array / array binding scenarios
    public OracleCollectionType? CollectionType { get; init; }
    public int? ArrayBindSize { get; init; }
    public int? ArrayBindCount { get; init; }

    public string? SourceColumn { get; init; }
}