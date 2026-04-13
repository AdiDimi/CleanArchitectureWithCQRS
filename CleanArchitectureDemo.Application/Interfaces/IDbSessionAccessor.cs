using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace CleanArchitectureDemo.Application.Interfaces
{
    public interface IDbSessionAccessor
    {
        IDbConnection? Connection { get; }
        IDbTransaction? Transaction { get; }
        bool HasActiveTransaction { get; }

        void SetSession(IDbConnection connection, IDbTransaction transaction);
        void ClearSession();
    }
}
