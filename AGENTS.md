# AGENTS.md

# ASP.NET Core Base Project - AI Agent Instructions

## Purpose

This document defines the mandatory development rules for AI coding agents working in this repository.

These instructions apply to all AI assistants, including but not limited to:

* GitHub Copilot
* GitHub Copilot Agent
* Claude Code
* OpenAI Codex
* Cursor
* JetBrains AI
* Any future coding assistant

If multiple instructions conflict, this file has the highest priority.

---

# Project Goal

This repository is a reusable ASP.NET Core Base Project.

Every generated feature must be production-ready.

Generated code must be clean, maintainable and consistent.

---

# Technology Stack

Language / Platform

* C# 14
* .NET 10 LTS

Framework

* ASP.NET Core 10

Database

* PostgreSQL

Migration

* Entity Framework Core Migrations

ORM

* Entity Framework Core 10
* Npgsql provider for PostgreSQL

Authentication / Authorization

* ASP.NET Core Authentication / Authorization
* JWT Access Token
* JWT Refresh Token

API Documentation

* OpenAPI
* Swagger UI when a UI is required

Containerization

* Docker
* Docker Compose

Build Tool

* .NET CLI
* MSBuild

Mapping

* Mapperly

Validation

* DataAnnotations

Logging

* Microsoft.Extensions.Logging (`ILogger<T>`)
* Serilog may be used for structured logging

Testing

* xUnit

---

# Mandatory Documents

Always read these documents before generating code.

1. docs/PROJECT_RULES.md
2. docs/API_CONVENTIONS.md
3. docs/CODE_CONVENTIONS.md
4. docs/DATABASE_CONVENTIONS.md
5. docs/ARCHITECTURE.md
6. docs/DEVELOPMENT_GUIDE.md
7. docs/SECURITY.md

If a generated implementation conflicts with these documents, follow the documentation.

---

# Required Architecture

Always follow this dependency flow.

```text
Controller
    ↓
Service
    ↓
Repository
    ↓
DbContext / Database
```

Never bypass a layer.

Forbidden

```text
Controller → Repository

Controller → DbContext

Repository → Service
```

---

# Required Project Structure

Every module must follow the same logical structure.

```text
Controllers/

Services/

Repositories/

Entities/

DTOs/
    Requests/
    Responses/

Mappings/
```

Shared infrastructure may additionally use:

```text
Data/
Configuration/
Exceptions/
Security/
Common/
```

---

# API Rules

Use Action-Based endpoints.

Examples

GET

```text
/User/GetUserById/{id}

/User/GetUsers
```

POST

```text
/User/CreateUser
```

PUT

```text
/User/UpdateUser
```

DELETE

```text
/User/DeleteUser/{id}
```

Do not generate REST-style endpoints unless explicitly requested.

---

# Response Rules

Every endpoint must return the standard response model:

```text
ApiResponse<T>
```

Never return Entity objects.

Always use DTOs.

---

# Entity Rules

Entities:

* represent database tables
* contain persistence mappings and persistence-related state only

Entities must never be exposed by controllers.

---

# DTO Rules

Always create dedicated:

* Request DTOs
* Response DTOs

Never reuse Entity classes as DTOs.

---

# Mapper Rules

Use Mapperly for Entity ↔ DTO mapping.

Never manually map objects inside controllers.

Mapping configuration must be kept in mapper classes.

---

# Dependency Injection

Use ASP.NET Core's built-in dependency injection container.

Always use Constructor Injection.

Preferred

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

Never:

* instantiate infrastructure dependencies directly inside controllers or services
* use service locator patterns for ordinary application dependencies
* resolve services from `IServiceProvider` inside business logic when constructor injection is possible

---

# Validation

Validate every incoming request using DataAnnotations.

Examples

* `[Required]`
* `[EmailAddress]`
* `[StringLength(...)]`
* `[RegularExpression(...)]`
* `[Range(...)]`

Business validation belongs inside the service layer.

---

# Transactions

Transactions belong to the service layer.

Use EF Core transaction APIs only when a business operation requires an explicit transaction.

Never start or manage database transactions inside controllers.

---

# Database Rules

Use PostgreSQL.

All schema changes require Entity Framework Core migrations.

Never change a migration that has already been applied to a shared or production database.

Create a new migration instead.

Use `snake_case` for:

* tables
* columns
* constraints
* indexes

---

# Security Rules

Passwords

* BCrypt only
* use a maintained .NET BCrypt implementation

Authentication

* JWT Access Token
* JWT Refresh Token

Never:

* store plain text passwords
* log tokens
* log passwords
* commit secrets

---

# Logging

Use `ILogger<T>`.

Never use `Console.WriteLine()` for application logging.

Log meaningful business events only.

Never log sensitive values.

---

# Code Style

Follow SOLID.

Follow Clean Code.

Keep methods small.

Keep classes focused.

Avoid duplicate code.

Prefer readability over cleverness.

Enable nullable reference types.

---

# Naming

Classes / Records / Interfaces / Enums

PascalCase

Interfaces

`I` prefix + PascalCase

Methods

PascalCase

Properties

PascalCase

Local variables and parameters

camelCase

Private fields

`_camelCase`

Constants

PascalCase

Namespaces

PascalCase

Database

snake_case

---

# Feature Development Workflow

Whenever implementing a new feature, create components in this order.

1. Entity / Domain Model
2. EF Core Configuration / DbContext changes
3. EF Core Migration
4. Repository
5. DTOs
6. Mapper
7. Service
8. Controller
9. Validation
10. OpenAPI / Swagger Documentation
11. Tests

Do not skip required steps.

---

# Prohibited

Never:

* Return Entity objects from API endpoints
* Access Repository from Controller
* Access `DbContext` directly from Controller
* Place business logic inside Controller
* Place business logic inside Repository
* Hard-code production secrets
* Commit secrets or production `.env` files
* Use `Console.WriteLine()` for application logging
* Introduce a different architecture without an explicit decision

---

# Expected Output

Generated code must:

* compile successfully with `dotnet build`
* follow project conventions
* follow C#/.NET naming conventions defined in this repository
* include validation
* include error handling
* include logging when appropriate
* include tests when required by the feature workflow
* be production-ready

---

# When Unsure

If requirements are ambiguous, prefer consistency with existing project conventions over generating new patterns.

Never introduce a different architectural style without an explicit request or architectural decision.
