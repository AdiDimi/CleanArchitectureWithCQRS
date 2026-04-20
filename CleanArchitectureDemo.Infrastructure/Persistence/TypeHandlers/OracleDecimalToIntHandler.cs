using Dapper;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace CleanArchitectureDemo.Infrastructure.Persistence.TypeHandlers
{
    public sealed class OracleDecimalToIntHandler : SqlMapper.TypeHandler<int>
    {
        public override int Parse(object value)
        {
            return value switch
            {
                OracleDecimal od => od.ToInt32(),
                decimal d => (int)d,
                int i => i,
                _ => Convert.ToInt32(value)
            };
        }

        public override void SetValue(IDbDataParameter parameter, int value)
        {
            parameter.Value = value;
        }
    }
}
