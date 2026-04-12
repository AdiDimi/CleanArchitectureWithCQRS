# Exception Handling Examples

## Usage in Handlers

### NotFoundException
```csharp
// Example 1: Simple message
throw new NotFoundException("User with specified email not found.");

// Example 2: With entity name and key (recommended)
throw new NotFoundException(nameof(User), userId);
// Output: Entity "User" (123e4567-e89b-12d3-a456-426614174000) was not found.
```

### ConflictException
```csharp
// Example 1: Simple message
throw new ConflictException("A user with this email already exists.");

// Example 2: With entity name and key (recommended)
throw new ConflictException(nameof(User), email);
// Output: Entity "User" (test@example.com) already exists or conflicts with existing data.
```

### ValidationException
```csharp
// Example: With validation errors
var errors = new Dictionary<string, string[]>
{
    { "Email", new[] { "Email is required.", "Email format is invalid." } },
    { "Password", new[] { "Password must be at least 8 characters." } }
};
throw new ValidationException(errors);
```

## HTTP Response Examples

### 404 Not Found
```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "Entity \"User\" (123e4567-e89b-12d3-a456-426614174000) was not found."
}
```

### 409 Conflict
```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
  "title": "Conflict",
  "status": 409,
  "detail": "Entity \"User\" (test@example.com) already exists or conflicts with existing data."
}
```

### 400 Bad Request (Validation)
```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation failures have occurred.",
  "errors": {
    "email": ["Email is required.", "Email format is invalid."],
    "password": ["Password must be at least 8 characters."]
  }
}
```

### 500 Internal Server Error
```json
{
  "type": "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred. Please try again later."
}
```

## Architecture Notes

- **Application Layer**: Exception definitions (`NotFoundException`, `ConflictException`, `ValidationException`)
- **API Layer**: Exception handling middleware (`GlobalExceptionHandlerMiddleware`)
- **Domain Layer**: Domain-specific exceptions (e.g., `InsufficientBalanceException`)
- **Infrastructure Layer**: Infrastructure-specific exceptions (e.g., `DatabaseConnectionException`)

The middleware automatically handles FluentValidation exceptions and converts them to proper HTTP responses.
