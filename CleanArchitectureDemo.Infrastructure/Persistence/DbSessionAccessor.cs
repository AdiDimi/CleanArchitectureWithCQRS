using CleanArchitectureDemo.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace CleanArchitectureDemo.Infrastructure.Persistence
{
    public sealed class DbSessionAccessor : IDbSessionAccessor
    {
        public IDbConnection? Connection { get; private set; }
        public IDbTransaction? Transaction { get; private set; }

        public bool HasActiveTransaction =>
            Connection is not null && Transaction is not null;

        public void SetSession(IDbConnection connection, IDbTransaction transaction)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        public void ClearSession()
        {
            Transaction = null;
            Connection = null;
        }
    }
}
