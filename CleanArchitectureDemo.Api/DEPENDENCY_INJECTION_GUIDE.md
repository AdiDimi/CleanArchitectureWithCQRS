# Dependency Injection Organization

## Overview
Dependency Injection registrations are now organized by layer, following Clean Architecture principles. Each layer is responsible for registering its own services.

## Layer Structure

### 1. Infrastructure Layer (`CleanArchitectureDemo.Infrastructure\DependencyInjection.cs`)

**Responsibility**: Register infrastructure implementations (data access, external services)

**Registered Services**:
- `AppDbContext` - Entity Framework Core DbContext
- `IAppDbContext` - DbContext abstraction
- `IUnitOfWork` → `UnitOfWork` - Transaction management
- `IUserRepository` → `UserRepository` - Data access repository

**Usage**:
```csharp
builder.Services.AddInfrastructure(builder.Configuration);
```

**Configuration Dependencies**:
- Connection string from `appsettings.json`: `ConnectionStrings:Db`
- Automatically configures migrations assembly

---

### 2. Application Layer (`CleanArchitectureDemo.Application\DependencyInjection.cs`)

**Responsibility**: Register application services (use cases, behaviors, validators)

**Registered Services**:
- **MediatR** - All handlers (Commands, Queries) from the assembly
- **FluentValidation** - All validators from the assembly
- **Pipeline Behaviors**:
  - `ValidationBehavior<,>` - Validates requests using FluentValidation
  - `TransactionBehavior<,>` - Wraps transactional commands in database transactions

**Usage**:
```csharp
builder.Services.AddApplication();
```

**Auto-Discovery**:
- All `IRequestHandler<,>` implementations are automatically registered
- All `AbstractValidator<>` implementations are automatically registered
- Uses reflection to scan the Application assembly

---

### 3. API Layer (`CleanArchitectureDemo.Api\Program.cs`)

**Responsibility**: Register API-specific services (controllers, middleware, OpenAPI)

**Registered Services**:
- `Controllers` - ASP.NET Core MVC controllers
- `OpenAPI` - Swagger/OpenAPI documentation
- `GlobalExceptionHandlerMiddleware` - Custom exception handling middleware

**Usage**:
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register layer services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Register API services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
```

---

## Benefits

### 1. **Separation of Concerns**
Each layer manages its own dependencies:
- Infrastructure doesn't know about Application
- Application doesn't know about Infrastructure implementations
- API orchestrates all layers

### 2. **Maintainability**
Adding new services is straightforward:
```csharp
// Infrastructure/DependencyInjection.cs
services.AddScoped<IProductRepository, ProductRepository>();

// Application/DependencyInjection.cs
// Handlers auto-registered via assembly scanning
```

### 3. **Testability**
Easy to create test-specific DI configurations:
```csharp
// In integration tests
services.AddApplication(); // Real application services
services.AddTestInfrastructure(); // Test database
```

### 4. **Reusability**
Layers can be used in different hosting models:
```csharp
// Web API
builder.Services.AddInfrastructure(config);
builder.Services.AddApplication();

// Console App
services.AddInfrastructure(config);
services.AddApplication();

// Azure Function
services.AddInfrastructure(config);
services.AddApplication();
```

---

## Registration Order

**Important**: Register in dependency order (bottom-up):

```csharp
1. AddInfrastructure()  // Infrastructure implementations
2. AddApplication()     // Application services (depend on Infrastructure abstractions)
3. AddControllers()     // API services (depend on Application)
```

---

## Adding New Services

### Infrastructure Services
```csharp
// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(...)
{
    // ... existing registrations ...
    
    // Add new infrastructure service
    services.AddScoped<IEmailService, SmtpEmailService>();
    services.AddScoped<IBlobStorage, AzureBlobStorage>();
    
    return services;
}
```

### Application Services
```csharp
// Application/DependencyInjection.cs
public static IServiceCollection AddApplication(...)
{
    // ... existing registrations ...
    
    // Add new pipeline behavior
    services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    
    return services;
}
```

### API Services
```csharp
// Program.cs
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add new API-specific service
builder.Services.AddCors(options => { /* ... */ });
builder.Services.AddAuthentication();
```

---

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "Db": "Server=(localdb)\\mssqllocaldb;Database=CleanArchitectureDb;Trusted_Connection=true"
  }
}
```

### Environment-Specific Settings
- `appsettings.Development.json` - Development configuration
- `appsettings.Production.json` - Production configuration

Infrastructure layer automatically uses the correct connection string based on environment.

---

## Clean Architecture Compliance

✅ **Dependency Rule**: Dependencies point inward
- API → Application → Domain
- API → Infrastructure (for DI only)
- Infrastructure → Application (abstractions only)

✅ **Single Responsibility**: Each layer registers its own services

✅ **Open/Closed**: Easy to extend with new services without modifying existing code

✅ **Dependency Inversion**: All layers depend on abstractions, not implementations
