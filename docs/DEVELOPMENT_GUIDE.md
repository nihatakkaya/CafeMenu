# Development Guide

## Purpose

This document describes the standard development workflow for the project.

Every new feature should follow this guide.

---

# Development Workflow

Every feature should be developed in the following order:

```text
EF Core Migration
        ↓
Entity
        ↓
Repository
        ↓
DTO
        ↓
Mapper
        ↓
Service
        ↓
Controller
        ↓
Validation
        ↓
OpenAPI / Swagger Documentation
        ↓
Testing
```

Do not skip required steps.

---

# Step 1 - Create EF Core Migration

Every database schema change must be managed using Entity Framework Core Migrations.

Examples

```bash
dotnet ef migrations add InitialSchema

dotnet ef migrations add CreateUserTable

dotnet ef migrations add CreateRoleTable
```

Apply migrations locally when appropriate:

```bash
dotnet ef database update
```

Never change a migration already applied to a shared or production database.

Create a new migration instead.

Keep migrations in the project's configured `Migrations/` directory.

---

# Step 2 - Create Entity

Rules

* One primary Entity per table where the model maps directly to a table
* Use EF Core-compatible entity classes and relationships
* Keep entities focused on persistence state
* Do not place application business orchestration inside entities
* Never expose entities directly through API controllers

Example

```text
UserEntity
CategoryEntity
ProductEntity
```

---

# Step 3 - Create Repository

Responsibilities

* Database operations
* EF Core queries
* Custom queries when needed

Repositories must never contain business logic.

Use interfaces where the project architecture requires abstraction.

Example

```text
IUserRepository / UserRepository
ICategoryRepository / CategoryRepository
IProductRepository / ProductRepository
```

---

# Step 4 - Create DTOs

Each module should have dedicated request and response DTOs.

Example

```text
CreateUserRequest
UpdateUserRequest
UserResponseDto
LoginRequest
LoginResponse
```

Never expose Entity objects through the API.

---

# Step 5 - Create Mapper

Use Mapperly for object mapping.

Responsibilities

* Entity → DTO
* DTO → Entity where appropriate

Example

```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserResponseDto ToResponse(UserEntity entity);

    public partial UserEntity ToEntity(CreateUserRequest request);
}
```

Do not manually map objects inside controllers.

---

# Step 6 - Create Service

Responsibilities

* Business rules
* Business validation
* Transaction boundaries
* Repository calls
* External integrations when required

Services should remain focused on business logic.

Use an interface such as `IUserService` when this is the project convention.

---

# Step 7 - Create Controller

Responsibilities

* Receive requests
* Rely on ASP.NET Core model validation
* Call services
* Return `ApiResponse<T>`

Controllers should remain thin.

Controllers must not access repositories or `DbContext` directly.

---

# Step 8 - Validation

Validate every incoming request.

Examples

```csharp
[Required]
[EmailAddress]
[StringLength(100)]
[RegularExpression("...")]
[Range(1, 100)]
```

Business validation belongs to the service layer.

---

# Step 9 - OpenAPI / Swagger Documentation

Every public endpoint should be represented in the OpenAPI document.

Include where appropriate:

* Summary
* Description
* Parameters
* Request model
* Response model
* Status codes
* Response examples

ASP.NET Core's OpenAPI support should be configured centrally.

---

# Step 10 - Testing

Every new feature should include tests.

Minimum

* Service tests
* Repository tests when repository behavior is non-trivial

Integration tests should be added for critical business flows.

Use xUnit as the default test framework.

---

# Folder Structure Example

```text
Users/
├── Controllers/
├── DTOs/
│   ├── Requests/
│   └── Responses/
├── Entities/
├── Mappings/
├── Repositories/
└── Services/
```

If the project uses global layer folders instead of feature folders, preserve the existing repository structure. Do not introduce a different organizational style without an explicit decision.

---

# Feature Checklist

Before completing a feature:

* EF Core migration created when the schema changed
* Entity implemented
* Repository created
* DTOs created
* Mapperly mapper implemented
* Service implemented
* Controller implemented
* Validation added
* OpenAPI documentation updated
* Tests completed

---

# Error Handling

All unhandled application exceptions must be handled centrally using ASP.NET Core `IExceptionHandler` or a global exception-handling middleware.

Controllers should never catch exceptions unless absolutely necessary.

Known business exceptions should be translated to the project's standard error response and error codes.

---

# Logging

Every important business operation should be logged through `ILogger<T>`.

Examples

* User login
* Password change
* User creation
* Permission updates

Sensitive information must never be written to logs.

Do not use `Console.WriteLine()` as application logging.

---

# Transactions

Transaction boundaries belong to the service layer.

Use explicit EF Core transactions only when required by a business operation.

Controllers must never manage database transactions.

---

# Code Review Checklist

Before requesting a review:

* `dotnet build` succeeds
* `dotnet test` succeeds
* Code follows project conventions
* No duplicate code
* DTOs are used
* Validation exists
* Logging is added where appropriate
* No debug code remains
* No commented-out code remains without a valid reason
* Documentation is updated if required
* No secrets are committed

---

# Definition of Done

A feature is considered complete only if:

* Business requirements are implemented
* Required database migration exists
* API follows conventions
* Validation is complete
* Error handling is complete
* Tests pass
* Documentation is updated
* Code review is completed when the team workflow requires it
