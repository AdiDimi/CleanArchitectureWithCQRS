# 🚀 Action Checklist - Oracle + Dapper Migration

## ✅ Code Changes (COMPLETED)

- [x] Updated User entity for Dapper compatibility
- [x] Created IDbConnectionFactory interface
- [x] Created OracleConnectionFactory implementation
- [x] Rewrote UserRepository using Dapper
- [x] Rewrote UnitOfWork for ADO.NET transactions
- [x] Updated Infrastructure DependencyInjection
- [x] Updated appsettings.json with Oracle connection string
- [x] Created database setup script (OracleSetup.sql)
- [x] Created migration documentation

## ⏳ Actions Required (DO THESE NOW)

### Step 1: Install NuGet Packages (REQUIRED)

Open terminal in solution directory and run:

```bash
# Navigate to Infrastructure project
cd CleanArchitectureDemo.Infrastructure

# Remove EF Core packages
dotnet remove package Microsoft.EntityFrameworkCore
dotnet remove package Microsoft.EntityFrameworkCore.SqlServer  
dotnet remove package Microsoft.EntityFrameworkCore.Tools

# Add Dapper and Oracle packages
dotnet add package Dapper --version 2.1.35
dotnet add package Oracle.ManagedDataAccess.Core --version 23.4.0

# Navigate back to solution root
cd ..

# Restore all packages
dotnet restore
```

**OR** manually edit `CleanArchitectureDemo.Infrastructure.csproj`:

Remove these lines:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" ... />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" ... />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" ... />
```

Add these lines:
```xml
<PackageReference Include="Dapper" Version="2.1.35" />
<PackageReference Include="Oracle.ManagedDataAccess.Core" Version="23.4.0" />
```

Then run: `dotnet restore`

---

### Step 2: Setup Oracle Database

#### Option A: Oracle XE (Free, for Development)
1. Download Oracle XE: https://www.oracle.com/database/technologies/xe-downloads.html
2. Install and note the password for SYS/SYSTEM
3. Default connection: `localhost:1521/XEPDB1`

#### Option B: Use Existing Oracle Database
Use your existing Oracle instance

#### Option C: Docker (Quickest for Testing)
```bash
docker run -d -p 1521:1521 -e ORACLE_PASSWORD=MyPassword123 gvenzl/oracle-xe:latest
```

---

### Step 3: Create Database Schema

1. Connect to Oracle using SQL Developer, SQL*Plus, or any Oracle client
2. Run the setup script located at:
   `CleanArchitectureDemo.Infrastructure\Persistence\Scripts\OracleSetup.sql`

```sql
CREATE TABLE Users (
    Id VARCHAR2(36) PRIMARY KEY,
    Email VARCHAR2(255) NOT NULL UNIQUE
);

CREATE INDEX IX_Users_Email ON Users(Email);
```

---

### Step 4: Update Connection String

Edit `CleanArchitectureDemo.Api\appsettings.json`:

```json
{
  "ConnectionStrings": {
    "OracleDb": "YOUR_CONNECTION_STRING_HERE"
  }
}
```

**Connection String Examples:**

**Local Oracle XE:**
```
Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XEPDB1)));User Id=system;Password=YourPassword123;
```

**Remote Oracle:**
```
Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=your-server.com)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=ORCL)));User Id=your_user;Password=your_password;
```

**Oracle with TNS:**
```
Data Source=your_tns_name;User Id=your_user;Password=your_password;
```

---

### Step 5: Clean Up Old Files (Optional)

You can delete these EF Core files:
- `CleanArchitectureDemo.Infrastructure\Persistence\AppDbContext.cs`
- `CleanArchitectureDemo.Application\Interfaces\IAppDbContext.cs`

Or move them to a backup folder.

---

### Step 6: Build and Test

```bash
# Build solution
dotnet build

# Run API
dotnet run --project CleanArchitectureDemo.Api

# Test endpoints
# POST http://localhost:5000/api/users
# GET http://localhost:5000/api/users
# GET http://localhost:5000/api/users/{id}
```

---

## 🧪 Verify Migration

### Test Checklist:
- [ ] Solution builds without errors
- [ ] Can create a user (POST /api/users)
- [ ] Can get user by ID (GET /api/users/{id})
- [ ] Can get all users (GET /api/users)
- [ ] Can update user (PUT /api/users/{id})
- [ ] Can delete user (DELETE /api/users/{id})
- [ ] Validation works (try creating user with invalid email)
- [ ] Transactions work (check database rollback on errors)
- [ ] Exception handling works (404, 409, etc.)

---

## 🔧 Troubleshooting

### Error: "Oracle.ManagedDataAccess.Client not found"
**Solution:** Run `dotnet restore` after adding packages

### Error: "ORA-12154: TNS:could not resolve the connect identifier"
**Solution:** Check your connection string, ensure Oracle service is running

### Error: "ORA-01017: invalid username/password"
**Solution:** Verify credentials in connection string

### Error: "Table or view does not exist"
**Solution:** Run the OracleSetup.sql script

### Build errors about DbContext
**Solution:** Remove old `AppDbContext.cs` file

---

## 📚 Documentation Created

- ✅ `MIGRATION_SUMMARY.md` - This file
- ✅ `ORACLE_DAPPER_MIGRATION.md` - Detailed migration guide
- ✅ `PACKAGE_CHANGES.md` - NuGet package instructions
- ✅ `Scripts/OracleSetup.sql` - Database schema

---

## 🎯 What You've Achieved

You've successfully migrated from:
- ❌ SQL Server → ✅ Oracle Database
- ❌ Entity Framework Core → ✅ Dapper

**WITHOUT changing:**
- ✅ Business Logic (Handlers)
- ✅ API Controllers
- ✅ Validation
- ✅ Exception Handling
- ✅ CQRS Pattern
- ✅ Clean Architecture Structure

**This is the power of Clean Architecture!** 🎉

---

## ⏭️ Next Steps After Migration

1. Performance testing and optimization
2. Add more complex queries using Oracle-specific features
3. Implement caching for frequently accessed data
4. Add logging for Dapper queries
5. Create integration tests with Oracle test database

---

## 📞 Need Help?

Refer to these resources:
- Dapper documentation: https://github.com/DapperLib/Dapper
- Oracle .NET docs: https://docs.oracle.com/en/database/oracle/oracle-database/
- Clean Architecture: https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html
