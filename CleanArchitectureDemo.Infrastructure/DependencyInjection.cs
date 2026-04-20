using CleanArchitectureDemo.Application.Interfaces;
using CleanArchitectureDemo.Infrastructure.Persistence;
using CleanArchitectureDemo.Infrastructure.Persistence.TypeHandlers;
using Dapper;
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

            // DbSession Accessor
            services.AddScoped<IDbSessionAccessor, DbSessionAccessor>();
            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            SqlMapper.AddTypeHandler(new OracleDecimalToIntHandler());
            SqlMapper.AddTypeHandler(new OracleDecimalToNullableIntHandler());

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>(); // Concrete for UnitOfWork injection
            //services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<UserRepository>());
            //services.AddSingleton<IDbConnectionFactory, OracleConnectionFactory>();

            return services;
        }
    }
}
