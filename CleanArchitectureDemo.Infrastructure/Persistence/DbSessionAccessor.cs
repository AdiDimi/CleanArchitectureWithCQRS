
using CleanArchitectureDemo.Application.Interfaces;
using System.Data;

namespace CleanArchitectureDemo.Infrastructure.Persistence;

public sealed class DbSessionAccessor : IDbSessionAccessor
{
    public IDbConnection? Connection { get; private set; }
    public IDbTransaction? Transaction { get; private set; }

    public bool HasActiveTransaction => Connection != null && Transaction != null;

    public void SetSession(IDbConnection connection, IDbTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    public void ClearSession()
    {
        Connection = null;
        Transaction = null;
    }
}
