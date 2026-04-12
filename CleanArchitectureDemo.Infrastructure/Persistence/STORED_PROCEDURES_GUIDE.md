# Oracle Stored Procedures Guide

## Overview
This project now uses Oracle stored procedures and functions instead of inline SQL queries. This provides better performance, security, and maintainability.

## Architecture Benefits

### ✅ Performance
- **Precompiled execution plans** - Oracle optimizes stored procedures once
- **Reduced network traffic** - Only procedure name and parameters sent
- **Better caching** - Execution plans cached in Oracle SGA

### ✅ Security
- **SQL Injection prevention** - Parameters properly handled by Oracle
- **Fine-grained permissions** - Grant EXECUTE on specific procedures
- **Code encapsulation** - Business logic hidden from application

### ✅ Maintainability
- **Centralized logic** - Query changes made in one place (database)
- **Version control** - SQL scripts tracked in repository
- **Easier optimization** - DBA can tune without code changes

## Database Objects Created

### 📂 Functions (READ Operations)

#### 1. `FN_GET_USER_BY_ID`
Returns user by ID as cursor.
```sql
SELECT * FROM TABLE(FN_GET_USER_BY_ID('550e8400-e29b-41d4-a716-446655440000'));
```

**Parameters:**
- `p_id` (VARCHAR2) - User GUID as string

**Returns:** `SYS_REFCURSOR` with columns: `Id`, `Email`

---

#### 2. `FN_GET_USER_BY_EMAIL`
Returns user by email as cursor.
```sql
SELECT * FROM TABLE(FN_GET_USER_BY_EMAIL('john.doe@example.com'));
```

**Parameters:**
- `p_email` (VARCHAR2) - User email address

**Returns:** `SYS_REFCURSOR` with columns: `Id`, `Email`

---

#### 3. `FN_GET_ALL_USERS`
Returns all users ordered by email.
```sql
DECLARE
    v_cursor SYS_REFCURSOR;
BEGIN
    v_cursor := FN_GET_ALL_USERS();
    -- Process cursor
END;
```

**Parameters:** None

**Returns:** `SYS_REFCURSOR` with columns: `Id`, `Email`

---

#### 4. `FN_GET_PAGED_USERS`
Returns paginated users.
```sql
SELECT * FROM TABLE(FN_GET_PAGED_USERS(0, 10)); -- First page, 10 items
```

**Parameters:**
- `p_offset` (NUMBER) - Number of rows to skip
- `p_page_size` (NUMBER) - Number of rows to return

**Returns:** `SYS_REFCURSOR` with columns: `Id`, `Email`

---

#### 5. `FN_USER_EXISTS`
Checks if user exists by ID.
```sql
SELECT FN_USER_EXISTS('550e8400-e29b-41d4-a716-446655440000') FROM DUAL;
-- Returns: 1 (exists) or 0 (not exists)
```

**Parameters:**
- `p_id` (VARCHAR2) - User GUID as string

**Returns:** `NUMBER` (1 = exists, 0 = not exists)

---

#### 6. `FN_EMAIL_EXISTS`
Checks if email exists.
```sql
SELECT FN_EMAIL_EXISTS('john.doe@example.com') FROM DUAL;
-- Returns: 1 (exists) or 0 (not exists)
```

**Parameters:**
- `p_email` (VARCHAR2) - Email address to check

**Returns:** `NUMBER` (1 = exists, 0 = not exists)

---

#### 7. `FN_GET_USER_COUNT`
Returns total count of users.
```sql
SELECT FN_GET_USER_COUNT() FROM DUAL;
-- Returns: 42
```

**Parameters:** None

**Returns:** `NUMBER` - Total user count

---

### 📝 Procedures (WRITE Operations)

#### 1. `SP_INSERT_USER`
Inserts a new user.
```sql
EXEC SP_INSERT_USER('550e8400-e29b-41d4-a716-446655440000', 'john.doe@example.com');
```

**Parameters:**
- `p_id` (VARCHAR2) - User GUID as string
- `p_email` (VARCHAR2) - User email address

**Exceptions:**
- `-20001` - Duplicate ID or Email
- `-20002` - General insert error

---

#### 2. `SP_UPDATE_USER`
Updates existing user.
```sql
EXEC SP_UPDATE_USER('550e8400-e29b-41d4-a716-446655440000', 'new.email@example.com');
```

**Parameters:**
- `p_id` (VARCHAR2) - User GUID as string
- `p_email` (VARCHAR2) - New email address

**Exceptions:**
- `-20003` - User not found
- `-20004` - Email already exists
- `-20005` - General update error

---

#### 3. `SP_DELETE_USER`
Deletes a user.
```sql
EXEC SP_DELETE_USER('550e8400-e29b-41d4-a716-446655440000');
```

**Parameters:**
- `p_id` (VARCHAR2) - User GUID as string

**Exceptions:**
- `-20006` - User not found
- `-20007` - General delete error

---

## C# Integration (Dapper)

### Query with Function (Returns Cursor)
```csharp
var users = await connection.QueryAsync<User>(
    "FN_GET_ALL_USERS",
    commandType: CommandType.StoredProcedure);
```

### Query with Scalar Function
```csharp
var count = await connection.ExecuteScalarAsync<int>(
    "SELECT FN_GET_USER_COUNT() FROM DUAL");
```

### Execute Procedure (Command)
```csharp
connection.Execute(
    "SP_INSERT_USER",
    new { p_id = userId, p_email = email },
    transaction,
    commandType: CommandType.StoredProcedure);
```

### Handling Oracle Exceptions
```csharp
try
{
    connection.Execute("SP_INSERT_USER", params, transaction, 
        commandType: CommandType.StoredProcedure);
}
catch (OracleException ex) when (ex.Number == 20001)
{
    throw new ConflictException("User already exists");
}
```

## Setup Instructions

### 1. Run Setup Script
```bash
sqlplus your_user/your_password@your_service
@OracleSetup.sql
```

### 2. Verify Installation
```sql
-- List all functions
SELECT object_name, object_type 
FROM user_objects 
WHERE object_type = 'FUNCTION'
ORDER BY object_name;

-- List all procedures
SELECT object_name, object_type 
FROM user_objects 
WHERE object_type = 'PROCEDURE'
ORDER BY object_name;
```

### 3. Test Functions
```sql
-- Test user count
SELECT FN_GET_USER_COUNT() AS TotalUsers FROM DUAL;

-- Test email exists
SELECT FN_EMAIL_EXISTS('test@example.com') AS EmailExists FROM DUAL;
```

### 4. Test Procedures
```sql
-- Insert test user
EXEC SP_INSERT_USER('550e8400-e29b-41d4-a716-446655440000', 'test@example.com');

-- Verify insert
SELECT * FROM Users WHERE Id = '550e8400-e29b-41d4-a716-446655440000';

-- Update user
EXEC SP_UPDATE_USER('550e8400-e29b-41d4-a716-446655440000', 'updated@example.com');

-- Delete user
EXEC SP_DELETE_USER('550e8400-e29b-41d4-a716-446655440000');
```

## Performance Tips

### 1. Use Bind Variables
Oracle caches execution plans based on SQL text. Stored procedures automatically use bind variables.

### 2. Analyze Execution Plans
```sql
EXPLAIN PLAN FOR
SELECT Id, Email FROM Users WHERE Id = '550e8400-e29b-41d4-a716-446655440000';

SELECT * FROM TABLE(DBMS_XPLAN.DISPLAY);
```

### 3. Monitor Performance
```sql
-- Find slow procedures
SELECT sql_text, elapsed_time, executions
FROM v$sql
WHERE sql_text LIKE '%SP_%'
ORDER BY elapsed_time DESC;
```

### 4. Optimize Indexes
```sql
-- Verify index usage
SELECT index_name, table_name, uniqueness
FROM user_indexes
WHERE table_name = 'USERS';
```

## Maintenance

### Rebuild Procedures After Changes
```sql
-- Recompile procedure
ALTER PROCEDURE SP_INSERT_USER COMPILE;

-- Recompile function
ALTER FUNCTION FN_GET_USER_BY_ID COMPILE;

-- Recompile all invalid objects
BEGIN
    DBMS_UTILITY.COMPILE_SCHEMA(schema => USER);
END;
/
```

### Check for Invalid Objects
```sql
SELECT object_name, object_type, status
FROM user_objects
WHERE status = 'INVALID';
```

### View Procedure Source
```sql
SELECT text
FROM user_source
WHERE name = 'SP_INSERT_USER'
ORDER BY line;
```

## Migration from Inline SQL

### Before (Inline SQL)
```csharp
var sql = "SELECT Id, Email FROM Users WHERE Id = :Id";
return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id.ToString() });
```

### After (Stored Procedure)
```csharp
var result = await connection.QueryAsync<User>(
    "FN_GET_USER_BY_ID",
    new { p_id = id.ToString() },
    commandType: CommandType.StoredProcedure);
return result.FirstOrDefault();
```

## Security Best Practices

### 1. Grant Minimum Permissions
```sql
-- Create application user
CREATE USER app_user IDENTIFIED BY secure_password;

-- Grant only EXECUTE permissions
GRANT EXECUTE ON FN_GET_USER_BY_ID TO app_user;
GRANT EXECUTE ON SP_INSERT_USER TO app_user;

-- Do NOT grant SELECT/INSERT/UPDATE/DELETE on tables
```

### 2. Use Separate Schemas
```sql
-- Schema for tables
CREATE USER data_owner IDENTIFIED BY password1;
GRANT CREATE TABLE TO data_owner;

-- Schema for procedures
CREATE USER app_owner IDENTIFIED BY password2;
GRANT CREATE PROCEDURE TO app_owner;

-- Application user (minimal permissions)
CREATE USER app_user IDENTIFIED BY password3;
GRANT EXECUTE ON app_owner.SP_INSERT_USER TO app_user;
```

### 3. Audit Logging
```sql
-- Enable auditing on procedures
AUDIT EXECUTE ON SP_DELETE_USER BY ACCESS;
```

## Troubleshooting

### Issue: ORA-00942: table or view does not exist
**Solution:** Grant EXECUTE permissions to application user
```sql
GRANT EXECUTE ON FN_GET_USER_BY_ID TO app_user;
```

### Issue: Function returns empty cursor
**Solution:** Check if cursor is being fetched correctly in C#
```csharp
// Correct way
var users = await connection.QueryAsync<User>(
    "FN_GET_ALL_USERS",
    commandType: CommandType.StoredProcedure);
```

### Issue: ORA-06550: line 1, column 7
**Solution:** Verify procedure name and parameter names match exactly
```csharp
// Correct parameter name
new { p_id = userId }  // ✅
new { id = userId }    // ❌
```

## Next Steps

1. ✅ Run `OracleSetup.sql` to create all procedures
2. ✅ Update connection string in `appsettings.json`
3. ✅ Test CRUD operations through API
4. ✅ Monitor performance and optimize as needed
5. Consider adding:
   - Bulk insert procedures
   - Advanced search procedures
   - Audit logging procedures
   - Data archival procedures

## Additional Resources

- [Oracle PL/SQL Documentation](https://docs.oracle.com/en/database/oracle/oracle-database/21/lnpls/)
- [Dapper Stored Procedure Guide](https://github.com/DapperLib/Dapper#stored-procedures)
- [Oracle Performance Tuning](https://docs.oracle.com/en/database/oracle/oracle-database/21/tgdba/)
