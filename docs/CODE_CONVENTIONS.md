# Code Conventions

## Purpose

This document defines the C#/.NET coding standards for the project.

All developers must follow these rules.

---

# General Principles

* Keep code clean.
* Keep methods short.
* Prefer readability over cleverness.
* Follow SOLID principles.
* Follow Clean Code principles.
* Avoid duplicate code.
* Write self-explanatory code.
* Enable nullable reference types.
* Prefer asynchronous APIs for I/O-bound work.

---

# Project Structure

```text
MyProject.Api

├── Common
├── Configuration
├── Controllers
├── Data
├── DTOs
├── Entities
├── Exceptions
├── Mappings
├── Repositories
├── Security
├── Services
└── Utilities
```

Use the real project namespace instead of `MyProject.Api`.

---

# Type Naming

Use PascalCase for classes, records, structs and enums.

Interfaces must use the `I` prefix.

Good

```csharp
UserController
UserService
IUserService
UserRepository
IUserRepository
UserEntity
UserResponseDto
```

Bad

```csharp
userController
user_service
USERSERVICE
userRepository
```

---

# Variable and Field Naming

Local variables and parameters use camelCase.

Private instance fields use `_camelCase`.

Public properties use PascalCase.

Good

```csharp
var firstName = request.FirstName;
private readonly IUserRepository _userRepository;
public string FirstName { get; init; } = string.Empty;
```

Bad

```csharp
var First_Name = request.FirstName;
private readonly IUserRepository UserRepository;
public string firstName { get; set; }
```

---

# Method Naming

Use PascalCase.

Methods should start with a verb and describe what they do.

Async methods should end with `Async` when they return `Task`/`Task<T>` and represent asynchronous work.

Good

```csharp
GetUserByIdAsync()
CreateUserAsync()
UpdateUserAsync()
DeleteUserAsync()
LoginAsync()
RegisterAsync()
```

Bad

```csharp
user()
data()
process()
method1()
GetUserByIdTask()
```

---

# Namespace Naming

Namespaces must use PascalCase segments and follow the solution/project structure.

Good

```text
MyProject.Api.Controllers
MyProject.Api.Repositories
MyProject.Api.Services
MyProject.Api.Security
```

Bad

```text
myproject.api.controllers
MyProject.Api.user_service
```

---

# Constant Naming

Use PascalCase for constants and static readonly fields unless an existing project convention explicitly defines otherwise.

Good

```csharp
private const int MaxPageSize = 50;
private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
```

Avoid magic numbers and unexplained literal values.

---

# Controller Rules

Controllers should only:

* Receive requests
* Validate request models
* Call services
* Return responses

Controllers must NEVER:

* Access repositories
* Access `DbContext`
* Execute SQL
* Contain business logic
* Manage database transactions

Good

```csharp
return Ok(await _userService.GetUserByIdAsync(id, cancellationToken));
```

Bad

```csharp
var user = await _dbContext.Users.FindAsync(id);
```

---

# Service Rules

Services contain business logic.

Services may:

* Validate business rules
* Call repositories
* Manage transaction boundaries
* Call external APIs
* Use Mapperly

Services should not depend on HTTP request/response details.

---

# Repository Rules

Repositories are responsible only for database operations.

Repositories may use Entity Framework Core and the project `DbContext`.

Repositories must never contain business logic.

---

# Entity Rules

Entities represent database tables and persistence relationships.

Entities must never:

* Contain controller logic
* Contain application orchestration logic
* Be returned directly from controllers

---

# DTO Rules

Always use DTOs for API request and response contracts.

Never expose Entity objects.

Good

```text
Entity
  ↓
DTO
  ↓
ApiResponse<T>
  ↓
JSON Response
```

Bad

```text
Entity
  ↓
JSON Response
```

Request and response DTOs must remain separate when they represent different contracts.

---

# Mapper Rules

All Entity ↔ DTO conversions should be performed by Mapperly unless a specific exception is documented.

Never manually map objects inside controllers.

Good

```text
Controller
    ↓
Service
    ↓
Mapperly
    ↓
DTO
```

Bad

```csharp
// Inside a controller
var dto = new UserResponseDto
{
    Id = entity.Id,
    Email = entity.Email
};
```

---

# Dependency Injection

Always use Constructor Injection.

Good

```csharp
public sealed class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }
}
```

Dependency registration belongs in application startup/configuration.

```csharp
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
```

Bad

```csharp
public sealed class UserService
{
    private readonly UserRepository _repository = new UserRepository(...);
}
```

Do not use `IServiceProvider` as a service locator in ordinary business code.

---

# C# Language Features

Allowed and encouraged when they improve clarity:

* `record` / `record class` for immutable DTO-like contracts where appropriate
* `init` accessors
* `required` members where appropriate
* pattern matching
* nullable reference types
* `async` / `await` for I/O-bound work

Avoid language features that make simple code harder to understand.

---

# Validation

Always validate incoming request DTOs using DataAnnotations.

Example

```csharp
public sealed class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;
}
```

Business validation belongs to the service layer.

---

# Exception Handling

Do not place repetitive try/catch blocks inside controllers.

Use ASP.NET Core `IExceptionHandler` or centralized exception middleware.

Catch exceptions only when the current layer can meaningfully handle, translate or recover from them.

---

# Logging

Never use:

```csharp
Console.WriteLine("User created");
```

Use `ILogger<T>`:

```csharp
_logger.LogInformation("User {UserId} created", userId);
```

Use structured logging placeholders rather than string concatenation.

Never log passwords, JWTs, refresh tokens, secrets or other sensitive credentials.

Serilog may be configured as the logging provider when structured sinks are required.

---

# Transactions

Business transaction boundaries belong in the service layer.

Use explicit EF Core transactions only when needed.

Controllers must never manage transactions.

---

# Magic Numbers

Avoid magic numbers.

Bad

```csharp
if (pageSize > 50)
{
}
```

Good

```csharp
private const int MaxPageSize = 50;

if (pageSize > MaxPageSize)
{
}
```

---

# Comments

Write code that explains itself.

Use comments only when they explain intent, constraints or non-obvious behavior.

Bad

```csharp
// Increment i
i++;
```

Good

```csharp
currentRetryCount++;
```

---

# Method Size

A method should ideally fit on one screen.

Prefer methods under approximately 30 lines whenever practical.

Split complex behavior into focused private methods or services instead of creating deeply nested methods.

---

# Class Size

Avoid classes with hundreds of lines and multiple unrelated responsibilities.

Split responsibilities into multiple focused classes.

---

# Null Handling

Nullable reference types must be enabled.

Use nullable types only when `null` has a valid meaning.

Never return `null` collections.

Good

```csharp
return Array.Empty<UserResponseDto>();
```

Bad

```csharp
return null;
```

Use guard clauses and validation rather than allowing invalid null values to propagate.

---

# Async Code

Use asynchronous EF Core and HTTP APIs for I/O-bound operations.

Prefer:

```csharp
await repository.GetByIdAsync(id, cancellationToken);
```

over blocking calls.

Pass `CancellationToken` through controller, service and repository calls when practical.

Avoid `.Result` and `.Wait()` on asynchronous operations.

---

# Clean Code Checklist

Before committing, ask yourself:

* Is the code readable?
* Is the method short and focused?
* Does the class have a single responsibility?
* Is duplicate code avoided?
* Is validation implemented?
* Are DTOs used?
* Is business logic inside the service layer?
* Are exceptions handled centrally?
* Are logs meaningful and free of secrets?
* Are async I/O operations implemented correctly?
* Does the code follow project naming conventions?

If every answer is YES, the code is ready for review.
