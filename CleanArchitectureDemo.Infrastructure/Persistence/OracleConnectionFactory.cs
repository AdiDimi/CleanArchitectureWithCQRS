using CleanArchitectureDemo.Application.Interfaces;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace CleanArchitectureDemo.Infrastructure.Persistence
{
    public class OracleConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public OracleConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new OracleConnection(_connectionString);
        }
    }
}
