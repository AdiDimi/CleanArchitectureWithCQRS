# User Repository Pattern Implementation

## Overview
The Repository Pattern provides an abstraction layer between the Application Layer and the Data Access Layer, following Clean Architecture principles.

## Architecture

### Layer Structure
```
CleanArchitectureDemo.Application\Interfaces\
├── IUserRepository.cs (Interface - Abstraction)

CleanArchitectureDemo.Infrastructure\Persistence\
├── UserRepository.cs (Implementation)
```

## IUserRepository Interface

### Query Methods (Read Operations)
```csharp
// Get single user by ID
Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

// Get single user by email (common lookup)
Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

// Get all users (uses AsNoTracking for performance)
Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

// Get paginated users (for large datasets)
Task<IReadOnlyList<User>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

// Check if user exists by ID
Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

// Check if email is already in use
Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

// Get total count of users
Task<int> GetCountAsync(CancellationToken cancellationToken = default);
```

### Command Methods (Write Operations)
```csharp
// Add new user (doesn't call SaveChanges - handled by UnitOfWork)
void Add(User user);

// Update existing user
void Update(User user);

// Delete user
void Delete(User user);
```

## Usage Examples

### CreateUserCommandHandler
```csharp
public async Task<Guid> Handle(CreateUserCommand request, CancellationToken ct)
{
    // Check if user with email already exists
    if (await _userRepository.EmailExistsAsync(request.Email, ct))
    {
        throw new ConflictException("User", request.Email);
    }

    var user = new User(request.Email);
    _userRepository.Add(user);
    
    // SaveChanges is called by TransactionBehavior via UnitOfWork
    return user.Id;
}
```

### GetUserByIdHandler
```csharp
public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken ct)
{
    var user = await _userRepository.GetByIdAsync(request.Id, ct);

    if (user == null)
    {
        throw new NotFoundException(nameof(User), request.Id);
    }

    return new UserDto(user.Id, user.Email);
}
```

### UpdateUserCommandHandler
```csharp
public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken ct)
{
    var user = await _userRepository.GetByIdAsync(request.Id, ct);
    if (user == null)
    {
        throw new NotFoundException(nameof(User), request.Id);
    }

    // Check if another user with the same email exists
    var existingUser = await _userRepository.GetByEmailAsync(request.Email, ct);
    if (existingUser != null && existingUser.Id != request.Id)
    {
        throw new ConflictException("User", request.Email);
    }

    _userRepository.Update(user);
    return Unit.Value;
}
```

### DeleteUserCommandHandler
```csharp
public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken ct)
{
    var user = await _userRepository.GetByIdAsync(request.Id, ct);
    if (user == null)
    {
        throw new NotFoundException(nameof(User), request.Id);
    }

    _userRepository.Delete(user);
    return Unit.Value;
}
```

### GetAllUsersHandler (with Pagination)
```csharp
public async Task<GetAllUsersResponse> Handle(GetAllUsersQuery request, CancellationToken ct)
{
    var users = await _userRepository.GetPagedAsync(
        request.PageNumber, 
        request.PageSize, 
        ct);

    var totalCount = await _userRepository.GetCountAsync(ct);
    var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

    var userDtos = users.Select(u => new UserDto(u.Id, u.Email)).ToList();

    return new GetAllUsersResponse(
        userDtos,
        totalCount,
        request.PageNumber,
        request.PageSize,
        totalPages);
}
```

## Benefits of Repository Pattern

### 1. **Separation of Concerns**
   - Application layer doesn't know about EF Core
   - Easy to switch data access technologies

### 2. **Testability**
   - Easy to mock IUserRepository in unit tests
   - No need to mock DbContext or DbSet

### 3. **Reusability**
   - Common queries (GetByEmail, EmailExists) centralized
   - No duplicate query logic across handlers

### 4. **Clean Architecture Compliance**
   - Application layer depends on abstraction (IUserRepository)
   - Infrastructure provides implementation
   - Dependency Rule maintained

### 5. **Performance Optimization**
   - AsNoTracking applied in repository for read-only queries
   - Consistent implementation across all queries

## API Endpoints

### GET /api/users?pageNumber=1&pageSize=10
Returns paginated list of users with metadata

### GET /api/users/{id}
Returns single user by ID (404 if not found)

### POST /api/users
Creates new user (409 if email exists)

### PUT /api/users/{id}
Updates existing user (404 if not found, 409 if email conflict)

### DELETE /api/users/{id}
Deletes user (404 if not found)

## Transaction Management

All commands (Create, Update, Delete) implement `ITransactionalCommand`:
- Wrapped in database transaction by `TransactionBehavior`
- SaveChanges called by `UnitOfWork.CommitTransactionAsync()`
- Automatic rollback on errors

## Registration

```csharp
// Program.cs
builder.Services.AddScoped<IUserRepository, UserRepository>();
```

## Notes

- Repository methods that modify data (Add, Update, Delete) don't call SaveChanges
- SaveChanges is managed by UnitOfWork through TransactionBehavior
- Queries use AsNoTracking for better performance
- All async methods support CancellationToken
