using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace CleanArchitectureDemo.Infrastructure.Persistence.Parameters;

public static class OracleParameterFactory
{
    public static OracleParameterSpec Varchar2(
        string name,
        string? value,
        int size,
        ParameterDirection direction = ParameterDirection.Input) =>
        new()
        {
            Name = name,
            Value = value,
            OracleDbType = OracleDbType.Varchar2,
            Size = size,
            Direction = direction
        };

    public static OracleParameterSpec NVarchar2(
        string name,
        string? value,
        int size,
        ParameterDirection direction = ParameterDirection.Input) =>
        new()
        {
            Name = name,
            Value = value,
            OracleDbType = OracleDbType.NVarchar2,
            Size = size,
            Direction = direction
        };

    public static OracleParameterSpec Char(
        string name,
        string? value,
        int size,
        ParameterDirection direction = ParameterDirection.Input) =>
        new()
        {
            Name = name,
            Value = value,
            OracleDbType = OracleDbType.Char,
            Size = size,
            Direction = direction
        };

    public static OracleParameterSpec Int32(
        string name,
        int? value,
        ParameterDirection direction = ParameterDirection.Input) =>
        new()
        {
            Name = name,
            Value = value,
            OracleDbType = OracleDbType.Int32,
            Direction = direction
        };

    public static OracleParameterSpec Int64(
        string name,
        long? value,
        ParameterDirection direction = ParameterDirection.Input) =>
        new()
        {
            Name = name,
            Value = value,
            OracleDbType = OracleDbType.Int64,
            Direction = direction
        };

    public static OracleParameterSpec Decimal(
        string name,
        decimal? value,
        byte precision,
        byte scale,
        ParameterDirection direction = ParameterDirection.Input) =>
        new()
        {
            Name = name,
            Value = value,
            OracleDbType = OracleDbType.Decimal,
            Precision = precision,
            Scale = scale,
            Direction = direction
        };

    public static OracleParameterSpec Date(
        string name,
        DateTime? value,
        ParameterDirection direction = ParameterDirection.Input) =>
        new()
        {
            Name = name,
            Value = value,
            OracleDbType = OracleDbType.Date,
            Direction = direction
        };

    public static OracleParameterSpec TimeStamp(
        string name,
        DateTime? value,
        ParameterDirection direction = ParameterDirection.Input) =>
        new()
        {
            Name = name,
            Value = value,
            OracleDbType = OracleDbType.TimeStamp,
            Direction = direction
        };

    public static OracleParameterSpec Clob(
        string name,
        string? value,
        ParameterDirection direction = ParameterDirection.Input) =>
        new()
        {
            Name = name,
            Value = value,
            OracleDbType = OracleDbType.Clob,
            Direction = direction
        };

    public static OracleParameterSpec Blob(
        string name,
        byte[]? value,
        ParameterDirection direction = ParameterDirection.Input) =>
        new()
        {
            Name = name,
            Value = value,
            OracleDbType = OracleDbType.Blob,
            Direction = direction
        };

    public static OracleParameterSpec Raw(
        string name,
        byte[]? value,
        int size,
        ParameterDirection direction = ParameterDirection.Input) =>
        new()
        {
            Name = name,
            Value = value,
            OracleDbType = OracleDbType.Raw,
            Size = size,
            Direction = direction
        };

    public static OracleParameterSpec RefCursorOut(string name) =>
        new()
        {
            Name = name,
            OracleDbType = OracleDbType.RefCursor,
            Direction = ParameterDirection.Output
        };

    public static OracleParameterSpec RefCursorReturn(string name = "result") =>
        new()
        {
            Name = name,
            OracleDbType = OracleDbType.RefCursor,
            Direction = ParameterDirection.ReturnValue
        };

    // Example for array binding / associative array
    public static OracleParameterSpec Varchar2Array(
        string name,
        string?[] values,
        int elementSize) =>
        new()
        {
            Name = name,
            Value = values,
            OracleDbType = OracleDbType.Varchar2,
            CollectionType = OracleCollectionType.PLSQLAssociativeArray,
            ArrayBindCount = values.Length,
            ArrayBindSize = elementSize,
            Direction = ParameterDirection.Input
        };
}