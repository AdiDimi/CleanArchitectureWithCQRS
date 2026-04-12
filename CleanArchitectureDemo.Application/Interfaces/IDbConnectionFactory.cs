using System.Data;

namespace CleanArchitectureDemo.Application.Interfaces
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
