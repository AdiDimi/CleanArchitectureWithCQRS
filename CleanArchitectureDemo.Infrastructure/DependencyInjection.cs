using CleanArchitectureDemo.Application.Interfaces;
using CleanArchitectureDemo.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitectureDemo.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Oracle Database Connection
            var connectionString = configuration.GetConnectionString("OracleDb") 
                ?? throw new InvalidOperationException("Oracle connection string 'OracleDb' not found.");

            services.AddSingleton<IDbConnectionFactory>(sp => 
                new OracleConnectionFactory(connectionString));

            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Repositories
            services.AddScoped<UserRepository>(); // Concrete for UnitOfWork injection
            services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<UserRepository>());

            return services;
        }
    }
}
