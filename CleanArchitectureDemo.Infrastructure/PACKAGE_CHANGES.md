# Required NuGet Package Changes

## Infrastructure Project (CleanArchitectureDemo.Infrastructure)

### ❌ REMOVE These Packages:
```bash
dotnet remove package Microsoft.EntityFrameworkCore
dotnet remove package Microsoft.EntityFrameworkCore.SqlServer
dotnet remove package Microsoft.EntityFrameworkCore.Tools
dotnet remove package Microsoft.EntityFrameworkCore.Design
```

### ✅ ADD These Packages:
```bash
dotnet add package Dapper --version 2.1.35
dotnet add package Oracle.ManagedDataAccess.Core --version 23.4.0
```

Or add directly to `.csproj`:
```xml
<ItemGroup>
  <PackageReference Include="Dapper" Version="2.1.35" />
  <PackageReference Include="Oracle.ManagedDataAccess.Core" Version="23.4.0" />
</ItemGroup>
```

## Files to Remove/Archive

### Can be deleted:
- `CleanArchitectureDemo.Infrastructure\Persistence\AppDbContext.cs` (replaced by OracleConnectionFactory)
- `CleanArchitectureDemo.Application\Interfaces\IAppDbContext.cs` (replaced by IDbConnectionFactory)
- Any EF Core migration files (if they exist)

### Keep for reference:
- Move old files to a `_Archive` folder if you want to keep them for reference

## Verify Installation

Run in terminal:
```bash
dotnet restore
dotnet build
```

You should see:
- No errors about missing EntityFrameworkCore
- Dapper and Oracle.ManagedDataAccess.Core restored successfully
