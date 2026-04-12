# Migration from SQL Server + EF Core to Oracle + Dapper

## Overview
This guide documents the migration from SQL Server with Entity Framework Core to Oracle Database with Dapper.

## ✅ What Changed

### 1. **Domain Layer** - Minor Changes
- **User.cs**: Changed property setters from `private set` to `public set` (required by Dapper)
- Made parameterless constructor `public` instead of `private`

### 2. **Application Layer** - New Interface
- **IDbConnectionFactory.cs** (NEW): Replaces EF Core-specific `IAppDbContext`
- **IUserRepository** (UNCHANGED): Repository abstraction remains the same
- **IUnitOfWork** (UNCHANGED): Interface remains the same
- All Handlers, Commands, Queries (UNCHANGED)

### 3. **Infrastructure Layer** - Complete Rewrite
- **OracleConnectionFactory.cs** (NEW): Creates Oracle DB connections
- **UserRepository.cs** (REWRITTEN): Uses Dapper instead of EF Core
- **UnitOfWork.cs** (REWRITTEN): Uses ADO.NET transactions instead of EF Core
- **DependencyInjection.cs** (UPDATED): Registers Oracle + Dapper services
- **AppDbContext.cs** (REMOVE): No longer needed
- **Scripts/OracleSetup.sql** (NEW): Database setup script

### 4. **API Layer** - Minimal Changes
- **appsettings.json**: Updated connection string to Oracle format
- **Program.cs** (UNCHANGED): Still uses same DI pattern

## 📦 Required NuGet Packages

### Remove (EF Core Packages):
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" />
```

### Add (Dapper + Oracle Packages):
```xml
<!-- In CleanArchitectureDemo.Infrastructure.csproj -->
<PackageReference Include="Dapper" Version="2.1.35" />
<PackageReference Include="Oracle.ManagedDataAccess.Core" Version="23.4.0" />
```

## 🗄️ Database Setup

### 1. Run the Setup Script
Execute `Scripts/OracleSetup.sql` in your Oracle database:
```sql
CREATE TABLE Users (
    Id VARCHAR2(36) PRIMARY KEY,
    Email VARCHAR2(255) NOT NULL UNIQUE
);

CREATE INDEX IX_Users_Email ON Users(Email);
```

### 2. Update Connection String
In `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "OracleDb": "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=your_username;Password=your_password;"
  }
}
```

Replace:
- `localhost` - Your Oracle server host
- `1521` - Your Oracle port
- `XEPDB1` - Your service name
- `your_username` - Your Oracle username
- `your_password` - Your Oracle password

## 🔄 Key Architectural Changes

### Before (EF Core):
```csharp
// AppDbContext with DbSet
public class AppDbContext : DbContext, IAppDbContext
{
    public DbSet<User> Users => Set<User>();
}

// Repository using EF Core
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
    }
}

// UnitOfWork using EF Core transactions
public async Task CommitTransactionAsync()
{
    await _context.SaveChangesAsync();
    await _transaction.CommitAsync();
}
```

### After (Dapper):
```csharp
// Connection Factory
public class OracleConnectionFactory : IDbConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        return new OracleConnection(_connectionString);
    }
}

// Repository using Dapper
public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    
    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();
        var sql = "SELECT Id, Email FROM Users WHERE Id = :Id";
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id.ToString() });
    }
}

// UnitOfWork using ADO.NET transactions
public async Task CommitTransactionAsync()
{
    _transaction?.Commit(); // No SaveChanges needed with Dapper
}
```

## 🎯 Benefits of This Migration

### Performance
- ✅ **Dapper**: Lightweight, minimal overhead
- ✅ **Direct SQL**: Full control over queries
- ✅ **No tracking**: Dapper doesn't track entities (faster reads)

### Oracle-Specific
- ✅ **Oracle.ManagedDataAccess**: Official Oracle driver
- ✅ **Native Oracle features**: Can use Oracle-specific SQL
- ✅ **Better Oracle performance**: Optimized for Oracle DB

### Architecture
- ✅ **Clean Architecture preserved**: Handlers unchanged!
- ✅ **Repository pattern**: Abstraction layer protected us
- ✅ **Unit of Work pattern**: Transaction management still works
- ✅ **CQRS + MediatR**: No changes needed

## 🔍 What Stayed the Same

Thanks to Clean Architecture:
- ✅ All Command/Query Handlers
- ✅ All Validators (FluentValidation)
- ✅ All Behaviors (Validation, Transaction)
- ✅ All DTOs
- ✅ All Controllers
- ✅ Exception handling
- ✅ DI structure

## 📝 Dapper vs EF Core Comparison

| Feature | EF Core | Dapper |
|---------|---------|--------|
| **Performance** | Good | Excellent |
| **SQL Control** | Limited (LINQ) | Full (Raw SQL) |
| **Learning Curve** | Medium | Low |
| **Change Tracking** | Automatic | Manual |
| **Migrations** | Built-in | Manual |
| **Type Safety** | Strong | Medium |
| **Mapping** | Automatic | Convention-based |

## 🧪 Testing Changes

All unit tests should continue to work by mocking `IUserRepository`. Integration tests need to:
1. Update to use Oracle test database
2. Seed data using SQL scripts instead of EF Core

## 🚀 Running the Application

1. Install Oracle Database (or use Oracle XE for development)
2. Run `Scripts/OracleSetup.sql`
3. Update connection string in `appsettings.json`
4. Install NuGet packages: `Dapper` and `Oracle.ManagedDataAccess.Core`
5. Remove EF Core packages
6. Run the application - all endpoints work the same!

## 📊 Query Examples

### Create User (INSERT)
```csharp
var sql = "INSERT INTO Users (Id, Email) VALUES (:Id, :Email)";
connection.Execute(sql, new { Id = user.Id.ToString(), user.Email }, transaction);
```

### Get User (SELECT)
```csharp
var sql = "SELECT Id, Email FROM Users WHERE Id = :Id";
return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id.ToString() });
```

### Update User
```csharp
var sql = "UPDATE Users SET Email = :Email WHERE Id = :Id";
connection.Execute(sql, new { Id = user.Id.ToString(), user.Email }, transaction);
```

### Delete User
```csharp
var sql = "DELETE FROM Users WHERE Id = :Id";
connection.Execute(sql, new { Id = user.Id.ToString() }, transaction);
```

### Pagination (Oracle Syntax)
```csharp
var sql = @"
    SELECT Id, Email 
    FROM Users 
    ORDER BY Email
    OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";
var users = await connection.QueryAsync<User>(sql, new { Offset, PageSize });
```

## ⚠️ Important Notes

1. **GUIDs**: Oracle doesn't have native GUID type - we use `VARCHAR2(36)`
2. **Parameters**: Oracle uses `:ParameterName` syntax (not `@ParameterName`)
3. **Transactions**: Must manually manage connections and transactions
4. **Connection Management**: Each repository method creates/disposes connections (or uses UnitOfWork connection)
5. **NULL Handling**: Dapper maps `NULL` to `default(T)` - ensure proper null checks

## 🎓 Clean Architecture Victory

This migration demonstrates the power of Clean Architecture:
- Infrastructure changes isolated
- Application logic untouched
- Domain model minimally affected
- API layer unchanged

**The abstraction layers (IUserRepository, IUnitOfWork) protected us from data access implementation details!**
