using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Types;

namespace CleanArchitectureDemo.Infrastructure.Persistence.TypeHandlers;

#region INT

public sealed class OracleDecimalToIntHandler : SqlMapper.TypeHandler<int>
{
    public override int Parse(object value) => value switch
    {
        OracleDecimal od => od.ToInt32(),
        decimal d => (int)d,
        int i => i,
        long l => (int)l,
        _ => Convert.ToInt32(value)
    };

    public override void SetValue(IDbDataParameter parameter, int value)
        => parameter.Value = value;
}

public sealed class OracleDecimalToNullableIntHandler : SqlMapper.TypeHandler<int?>
{
    public override int? Parse(object value)
    {
        if (value is null || value is DBNull) return null;

        return value switch
        {
            OracleDecimal od => od.IsNull ? null : od.ToInt32(),
            decimal d => (int)d,
            int i => i,
            long l => (int)l,
            _ => Convert.ToInt32(value)
        };
    }

    public override void SetValue(IDbDataParameter parameter, int? value)
        => parameter.Value = value ?? (object)DBNull.Value;
}

#endregion

#region LONG

public sealed class OracleDecimalToLongHandler : SqlMapper.TypeHandler<long>
{
    public override long Parse(object value) => value switch
    {
        OracleDecimal od => od.ToInt64(),
        decimal d => (long)d,
        long l => l,
        int i => i,
        _ => Convert.ToInt64(value)
    };

    public override void SetValue(IDbDataParameter parameter, long value)
        => parameter.Value = value;
}

public sealed class OracleDecimalToNullableLongHandler : SqlMapper.TypeHandler<long?>
{
    public override long? Parse(object value)
    {
        if (value is null || value is DBNull) return null;

        return value switch
        {
            OracleDecimal od => od.IsNull ? null : od.ToInt64(),
            decimal d => (long)d,
            long l => l,
            int i => i,
            _ => Convert.ToInt64(value)
        };
    }

    public override void SetValue(IDbDataParameter parameter, long? value)
        => parameter.Value = value ?? (object)DBNull.Value;
}

#endregion

#region DECIMAL

public sealed class OracleDecimalToDecimalHandler : SqlMapper.TypeHandler<decimal>
{
    public override decimal Parse(object value) => value switch
    {
        OracleDecimal od => od.Value,
        decimal d => d,
        _ => Convert.ToDecimal(value)
    };

    public override void SetValue(IDbDataParameter parameter, decimal value)
        => parameter.Value = value;
}

public sealed class OracleDecimalToNullableDecimalHandler : SqlMapper.TypeHandler<decimal?>
{
    public override decimal? Parse(object value)
    {
        if (value is null || value is DBNull) return null;

        return value switch
        {
            OracleDecimal od => od.IsNull ? null : od.Value,
            decimal d => d,
            _ => Convert.ToDecimal(value)
        };
    }

    public override void SetValue(IDbDataParameter parameter, decimal? value)
        => parameter.Value = value ?? (object)DBNull.Value;
}

#endregion

#region BOOL (NUMBER(1))

public sealed class OracleDecimalToBoolHandler : SqlMapper.TypeHandler<bool>
{
    public override bool Parse(object value) => value switch
    {
        OracleDecimal od => od.ToInt32() == 1,
        decimal d => d == 1,
        int i => i == 1,
        long l => l == 1,
        _ => Convert.ToInt32(value) == 1
    };

    public override void SetValue(IDbDataParameter parameter, bool value)
        => parameter.Value = value ? 1 : 0;
}

public sealed class OracleDecimalToNullableBoolHandler : SqlMapper.TypeHandler<bool?>
{
    public override bool? Parse(object value)
    {
        if (value is null || value is DBNull) return null;

        return value switch
        {
            OracleDecimal od => od.IsNull ? null : od.ToInt32() == 1,
            decimal d => d == 1,
            int i => i == 1,
            long l => l == 1,
            _ => Convert.ToInt32(value) == 1
        };
    }

    public override void SetValue(IDbDataParameter parameter, bool? value)
        => parameter.Value = value.HasValue
            ? (value.Value ? 1 : 0)
            : DBNull.Value;
}

#endregion

#region DATETIME

public sealed class OracleDateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override DateTime Parse(object value) => value switch
    {
        OracleDate od => od.Value,
        OracleTimeStamp ots => ots.Value,
        DateTime dt => dt,
        _ => Convert.ToDateTime(value)
    };

    public override void SetValue(IDbDataParameter parameter, DateTime value)
        => parameter.Value = value;
}

public sealed class OracleNullableDateTimeHandler : SqlMapper.TypeHandler<DateTime?>
{
    public override DateTime? Parse(object value)
    {
        if (value is null || value is DBNull) return null;

        return value switch
        {
            OracleDate od => od.IsNull ? null : od.Value,
            OracleTimeStamp ots => ots.IsNull ? null : ots.Value,
            DateTime dt => dt,
            _ => Convert.ToDateTime(value)
        };
    }

    public override void SetValue(IDbDataParameter parameter, DateTime? value)
        => parameter.Value = value ?? (object)DBNull.Value;
}

#endregion

#region GUID

public sealed class OracleGuidHandler : SqlMapper.TypeHandler<Guid>
{
    public override Guid Parse(object value) => value switch
    {
        Guid g => g,
        string s when !string.IsNullOrWhiteSpace(s) => Guid.Parse(s.Trim()),
        byte[] bytes when bytes.Length == 16 => new Guid(bytes),
        OracleBinary ob when !ob.IsNull => new Guid(ob.Value),
        _ => throw new DataException($"Cannot convert {value.GetType().Name} to Guid.")
    };

    public override void SetValue(IDbDataParameter parameter, Guid value)
        => parameter.Value = value.ToString("D");
}

public sealed class OracleNullableGuidHandler : SqlMapper.TypeHandler<Guid?>
{
    public override Guid? Parse(object value)
    {
        if (value is null || value is DBNull) return null;

        return value switch
        {
            Guid g => g,
            string s when !string.IsNullOrWhiteSpace(s) => Guid.Parse(s.Trim()),
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            OracleBinary ob when ob.IsNull => null,
            OracleBinary ob => new Guid(ob.Value),
            _ => throw new DataException($"Cannot convert {value.GetType().Name} to Guid?.")
        };
    }

    public override void SetValue(IDbDataParameter parameter, Guid? value)
        => parameter.Value = value.HasValue ? value.Value.ToString("D") : DBNull.Value;
}

#endregion

#region STRING TRIMMING

public sealed class OracleTrimmedStringHandler : SqlMapper.TypeHandler<string>
{
    public override string Parse(object value) => value switch
    {
        null => string.Empty,
        DBNull => string.Empty,
        string s => s.TrimEnd(),
        OracleString os when os.IsNull => string.Empty,
        OracleString os => os.Value.TrimEnd(),
        _ => Convert.ToString(value)?.TrimEnd() ?? string.Empty
    };

    public override void SetValue(IDbDataParameter parameter, string value)
        => parameter.Value = value ?? (object)DBNull.Value;
}

#endregion

#region ENUM BASE HANDLERS

public sealed class IntEnumTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Parse(object value)
    {
        var intValue = value switch
        {
            OracleDecimal od => od.ToInt32(),
            decimal d => (int)d,
            int i => i,
            long l => checked((int)l),
            _ => Convert.ToInt32(value)
        };

        if (!Enum.IsDefined(typeof(TEnum), intValue))
        {
            throw new DataException($"Value '{intValue}' is not valid for enum {typeof(TEnum).Name}.");
        }

        return (TEnum)Enum.ToObject(typeof(TEnum), intValue);
    }

    public override void SetValue(IDbDataParameter parameter, TEnum value)
        => parameter.Value = Convert.ToInt32(value);
}

public sealed class NullableIntEnumTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum?>
    where TEnum : struct, Enum
{
    public override TEnum? Parse(object value)
    {
        if (value is null || value is DBNull)
        {
            return null;
        }

        var intValue = value switch
        {
            OracleDecimal od => od.ToInt32(),
            decimal d => (int)d,
            int i => i,
            long l => checked((int)l),
            _ => Convert.ToInt32(value)
        };

        if (!Enum.IsDefined(typeof(TEnum), intValue))
        {
            throw new DataException($"Value '{intValue}' is not valid for enum {typeof(TEnum).Name}.");
        }

        return (TEnum)Enum.ToObject(typeof(TEnum), intValue);
    }

    public override void SetValue(IDbDataParameter parameter, TEnum? value)
        => parameter.Value = value.HasValue ? Convert.ToInt32(value.Value) : DBNull.Value;
}

#endregion

#region REGISTRATION

public static class OracleTypeHandlerRegistration
{
    private static bool _registered;
    private static readonly object Sync = new();

    public static void Register()
    {
        if (_registered) return;

        lock (Sync)
        {
            if (_registered) return;

            SqlMapper.AddTypeHandler(new OracleDecimalToIntHandler());
            SqlMapper.AddTypeHandler(new OracleDecimalToNullableIntHandler());

            SqlMapper.AddTypeHandler(new OracleDecimalToLongHandler());
            SqlMapper.AddTypeHandler(new OracleDecimalToNullableLongHandler());

            SqlMapper.AddTypeHandler(new OracleDecimalToDecimalHandler());
            SqlMapper.AddTypeHandler(new OracleDecimalToNullableDecimalHandler());

            SqlMapper.AddTypeHandler(new OracleDecimalToBoolHandler());
            SqlMapper.AddTypeHandler(new OracleDecimalToNullableBoolHandler());

            SqlMapper.AddTypeHandler(new OracleDateTimeHandler());
            SqlMapper.AddTypeHandler(new OracleNullableDateTimeHandler());

            SqlMapper.AddTypeHandler(new OracleGuidHandler());
            SqlMapper.AddTypeHandler(new OracleNullableGuidHandler());

            SqlMapper.AddTypeHandler(new OracleTrimmedStringHandler());

            _registered = true;
        }
    }

    public static void RegisterEnum<TEnum>() where TEnum : struct, Enum
    {
        SqlMapper.AddTypeHandler(new IntEnumTypeHandler<TEnum>());
        SqlMapper.AddTypeHandler(new NullableIntEnumTypeHandler<TEnum>());
    }
}

#endregion