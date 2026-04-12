# Logging Implementation Guide

## Overview
This project uses **Serilog** for structured logging with comprehensive logging throughout the Clean Architecture layers.

## Components

### 1. **LoggingBehavior** (MediatR Pipeline)
- **Location**: `CleanArchitectureDemo.Application\Behaviors\LoggingBehavior.cs`
- **Purpose**: Logs all MediatR requests (commands and queries) with performance tracking
- **Features**:
  - Unique request ID generation
  - Request serialization for debugging
  - Execution time measurement
  - Error logging with full exception details
  - Structured logging with named parameters

### 2. **Serilog Configuration**
- **Location**: `CleanArchitectureDemo.Api\Program.cs`
- **Sinks**:
  - **Console**: Real-time log output during development
  - **File**: Daily rolling log files in `logs/` directory
- **Log Levels**:
  - Application: Information
  - ASP.NET Core: Warning (reduces noise)
  - System: Warning

### 3. **Repository Logging**
- **Location**: `CleanArchitectureDemo.Infrastructure\Persistence\UserRepository.cs`
- **Operations Logged**:
  - User creation (Add)
  - User updates (Update)
  - User deletion (Delete)
- **Includes**: User ID and email for traceability

## Log Output Examples

### Request Logging (LoggingBehavior)
```
[12:34:56 INF] Handling CreateUserCommand [a1b2c3d4-...] - Request: {"Email":"user@example.com"}
[12:34:56 INF] Adding user with ID: e5f6g7h8-..., Email: user@example.com
[12:34:56 INF] Successfully added user with ID: e5f6g7h8-...
[12:34:56 INF] Handled CreateUserCommand [a1b2c3d4-...] - Completed in 45ms
```

### Error Logging
```
[12:45:23 ERR] Error handling GetUserByIdQuery [x9y8z7w6-...] - Failed after 12ms
System.Exception: User not found
   at CleanArchitectureDemo.Application.Queries.GetUserById.GetUserByIdHandler...
```

### Database Operations
```
[13:15:30 INF] Adding user with ID: a1b2c3d4-..., Email: john.doe@example.com
[13:15:30 INF] Successfully added user with ID: a1b2c3d4-...
```

## Log Files

### File Location
- **Path**: `logs/log-YYYYMMDD.txt`
- **Rolling**: Daily (new file each day)
- **Format**: Timestamp + Level + Message + Properties + Exception

### Sample Log File Entry
```
2026-04-09 12:34:56.789 +00:00 [INF] Handling CreateUserCommand [a1b2c3d4-e5f6-7890-abcd-ef1234567890] {"RequestName":"CreateUserCommand","RequestId":"a1b2c3d4-e5f6-7890-abcd-ef1234567890"}
```

## Configuration

### appsettings.json
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Changing Log Levels
You can adjust log levels in `Program.cs`:
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()  // Change to Debug, Information, Warning, Error
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    // ...
```

## Pipeline Behavior Order

The MediatR pipeline executes behaviors in registration order:
1. **LoggingBehavior** - Logs request start
2. **ValidationBehavior** - Validates request
3. **TransactionBehavior** - Manages database transactions
4. **Handler** - Executes business logic
5. **LoggingBehavior** - Logs request completion/error

## Benefits

### 1. **Structured Logging**
- Named parameters for easy querying
- JSON serialization for complex objects
- Consistent log format

### 2. **Performance Tracking**
- Execution time for every request
- Identify slow operations
- Performance bottleneck detection

### 3. **Error Diagnostics**
- Full exception stack traces
- Request context included in error logs
- Correlation via Request IDs

### 4. **Audit Trail**
- All CRUD operations logged
- User actions tracked
- Database operations recorded

### 5. **Production-Ready**
- File-based logging for persistence
- Daily log rotation to manage disk space
- Configurable log levels via configuration

## Adding Logging to New Components

### In Handlers
The `LoggingBehavior` automatically logs all MediatR requests. No additional code needed.

### In Repositories
Inject `ILogger<T>` and log operations:
```csharp
public class MyRepository
{
    private readonly ILogger<MyRepository> _logger;
    
    public MyRepository(ILogger<MyRepository> logger)
    {
        _logger = logger;
    }
    
    public void Add(MyEntity entity)
    {
        _logger.LogInformation("Adding entity with ID: {EntityId}", entity.Id);
        // ... database operation
        _logger.LogInformation("Successfully added entity with ID: {EntityId}", entity.Id);
    }
}
```

### In Services
Same pattern - inject `ILogger<T>` via constructor.

## Best Practices

### ✅ DO:
- Use structured logging with named parameters: `_logger.LogInformation("User {UserId} created", userId)`
- Log at appropriate levels (Information for normal flow, Error for exceptions)
- Include correlation IDs for request tracking
- Log before and after critical operations

### ❌ DON'T:
- Don't log sensitive data (passwords, tokens, etc.)
- Avoid string concatenation: `_logger.LogInformation("User " + userId)` ❌
- Don't log inside tight loops (performance impact)
- Avoid logging at Debug level in production

## Monitoring Production Logs

For production environments, consider integrating with:
- **Seq** - Structured log server (great for development)
- **Application Insights** - Azure monitoring
- **ELK Stack** - Elasticsearch, Logstash, Kibana
- **Datadog** - Cloud monitoring platform

Add additional Serilog sinks via NuGet packages:
```
dotnet add package Serilog.Sinks.Seq
dotnet add package Serilog.Sinks.ApplicationInsights
```

## Troubleshooting

### Logs Not Appearing
1. Check log level configuration in `appsettings.json`
2. Verify Serilog is configured in `Program.cs`
3. Ensure `UseSerilog()` is called on the host builder

### Log Files Not Created
1. Check application has write permissions to `logs/` directory
2. Verify file path in `WriteTo.File()` configuration
3. Check disk space availability

### Performance Issues
1. Reduce log level to Warning or Error
2. Remove file sink in high-traffic scenarios
3. Use async logging for better performance
