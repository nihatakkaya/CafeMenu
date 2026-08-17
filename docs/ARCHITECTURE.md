# Architecture

## Purpose

This document describes the architecture of the project.

The project follows a layered architecture.

Each layer has a single responsibility.

---

# Architecture Overview

```text
                HTTP Request
                      │
                      ▼
              Controller Layer
                      │
                      ▼
               Service Layer
                      │
                      ▼
             Repository Layer
                      │
                      ▼
                 EF Core
                      │
                      ▼
                PostgreSQL Database
```

---

# Solution / Folder Structure

A standard single-API solution may use the following structure:

```text
MyProject.sln

src/
└── MyProject.Api/
    ├── Common/
    ├── Configuration/
    ├── Controllers/
    ├── Data/
    ├── DTOs/
    │   ├── Requests/
    │   └── Responses/
    ├── Entities/
    ├── Exceptions/
    ├── Mappings/
    ├── Repositories/
    ├── Security/
    ├── Services/
    └── Utilities/

tests/
└── MyProject.Tests/
```

Use the actual solution/project name instead of `MyProject`.

Do not introduce Clean Architecture, CQRS, MediatR or another architectural style unless the project explicitly adopts it through an architectural decision.

---

# Layer Responsibilities

## Controller

Responsibilities

* Receive HTTP requests
* Receive and validate request DTOs
* Call services
* Return API responses

Controllers must NOT:

* Execute SQL
* Access repositories directly
* Access `DbContext` directly
* Contain business logic
* Manage transactions

---

## Service

Responsibilities

* Business logic
* Business validation
* Transaction boundaries
* Repository calls
* External service integrations
* Mapping orchestration when required

Services may:

* Call repositories
* Call other services
* Use Mapperly

Services must NOT:

* Depend on controller implementation details
* Return Entity objects directly to controllers

---

## Repository

Responsibilities

* Read data
* Write data
* Execute database queries through Entity Framework Core

Repositories may:

* Use the project `DbContext`
* Use LINQ queries
* Use parameterized raw SQL only when necessary

Repositories must NOT:

* Contain business logic
* Perform business validation
* Call application services
* Call external services

---

## Entity

Responsibilities

* Represent database tables
* Define persistence relationships
* Store persistent data

Entities must NOT:

* Be returned from controllers
* Contain controller logic
* Contain application business orchestration

---

## DTO

Responsibilities

* Request models
* Response models
* API communication

DTOs are used between:

```text
Controller
    ↓
Service
```

Entities never leave the service boundary as API response models.

---

## Mapper

Responsibilities

* Convert Entity → DTO
* Convert DTO → Entity where appropriate

Mapperly is used for object mapping.

Mapping rules must be defined in Mapperly mapper classes.

Controllers must never contain manual Entity ↔ DTO mapping logic.

---

## Security

Responsibilities

* Authentication
* Authorization
* JWT validation
* Authorization policies / roles
* Authentication and authorization middleware/configuration

Business logic must not exist inside security infrastructure.

---

## Configuration

Responsibilities

* Dependency injection registrations
* Options binding
* OpenAPI configuration
* Authentication / authorization configuration
* CORS configuration
* JSON serialization configuration
* Database configuration

Configuration should be sourced from `appsettings.json`, environment-specific configuration, environment variables and secret providers as defined in `ENVIRONMENT.md`.

---

## Exception Handling

Responsibilities

* Global exception handling
* Standard error responses
* Mapping known exceptions to HTTP status codes and project error codes

Use ASP.NET Core `IExceptionHandler` or a centralized exception-handling middleware.

Controllers should not contain repetitive try/catch blocks.

---

## Utilities

Responsibilities

* Generic helper methods
* Utility classes without business ownership

Utilities must not contain business logic.

---

# Dependency Flow

Allowed

```text
Controller
    ↓
Service
    ↓
Repository
    ↓
DbContext
```

Allowed

```text
Controller
    ↓
Service
    ↓
Another Service
```

Not Allowed

```text
Controller
    ↓
Repository
```

Not Allowed

```text
Controller
    ↓
DbContext
```

Not Allowed

```text
Repository
    ↓
Service
```

---

# Request Flow

```text
HTTP Request
    ↓
Controller
    ↓
Model Validation
    ↓
Service
    ↓
Business Validation
    ↓
Repository
    ↓
DbContext / EF Core
    ↓
PostgreSQL
    ↓
Repository
    ↓
Service
    ↓
Mapperly
    ↓
DTO
    ↓
Controller
    ↓
ApiResponse<T>
    ↓
HTTP Response
```

---

# Dependency Injection

Always use Constructor Injection through ASP.NET Core's built-in dependency injection container.

Registration example

```csharp
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
```

Consumption example

```csharp
public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
}
```

Do not use a service locator pattern inside business code.

---

# Transactions

Transactions belong to the service layer.

Use explicit EF Core transactions only when the business operation requires multiple database operations to succeed or fail as one unit.

Example

```csharp
await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

// Perform the coordinated operation.

await transaction.CommitAsync(cancellationToken);
```

Controllers must never manage database transactions.

---

# Validation

Request-model validation must happen before business logic.

Use DataAnnotations.

```csharp
[Required]
[EmailAddress]
[StringLength(100)]
[Range(1, 100)]
```

Business validation belongs to the service layer.

---

# Exception Flow

```text
Exception
    ↓
IExceptionHandler / Global Exception Middleware
    ↓
ApiResponse<T> / Standard Error Response
    ↓
HTTP Response
```

Controllers should never catch exceptions unless there is a specific controller-level reason.

---

# API Response

Every endpoint returns the same response model.

```text
ApiResponse<T>
```

Example

```json
{
    "success": true,
    "message": "Operation completed successfully.",
    "data": {}
}
```

---

# Future Modules

The architecture should support adding new modules without changing the existing structure.

Example

```text
Authentication
User
Role
Permission
Category
Product
Order
Customer
```

Each module follows the same architecture.

---

# Design Principles

The project follows:

* SOLID
* Clean Code
* Layered Architecture
* Separation of Concerns
* Dependency Injection
* DRY
* KISS

---

# Development Rules

Every new feature should include, when applicable:

* Entity / Domain Model
* EF Core Configuration / DbContext changes
* EF Core Migration
* Repository
* DTOs
* Mapper
* Service
* Controller
* Validation
* OpenAPI documentation
* Tests

No layer may bypass another layer.

All code must follow this architecture.
