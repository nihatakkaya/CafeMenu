# API Conventions

## Purpose

This document defines the API standards used throughout the project.

All developers must follow these conventions.

---

# Base URL

```text
https://example.com
```

Replace the example domain with the actual deployment domain in environment-specific deployment documentation.

---

# Controller Naming

Every controller name must be singular and written in **PascalCase**.

### Examples

```text
UserController
RoleController
CategoryController
ProductController
```

---

# Route Convention

Every controller must use its controller name, without the `Controller` suffix, as the base route.

Example:

```csharp
[ApiController]
[Route("User")]
public sealed class UserController : ControllerBase
{
}
```

```csharp
[ApiController]
[Route("Role")]
public sealed class RoleController : ControllerBase
{
}
```

```csharp
[ApiController]
[Route("Category")]
public sealed class CategoryController : ControllerBase
{
}
```

---

# Endpoint Naming

All endpoint names must use **PascalCase**.

Action names should clearly describe what the endpoint does.

## GET

```text
GET /User/GetUserById/{id}

GET /User/GetUsers

GET /User/GetPagedUsers

GET /User/SearchUsers
```

ASP.NET Core example:

```csharp
[HttpGet("GetUserById/{id:long}")]
public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetUserById(long id)
{
    // Call service only.
}
```

---

## POST

```text
POST /User/CreateUser

POST /User/Login

POST /User/Register

POST /User/RefreshToken
```

---

## PUT

```text
PUT /User/UpdateUser

PUT /User/ChangePassword

PUT /User/UpdateProfile
```

---

## DELETE

```text
DELETE /User/DeleteUser/{id}
```

---

# Route Parameters

IDs should be passed as route parameters whenever possible.

Good

```text
GET /User/GetUserById/15
```

Bad

```text
GET /User/GetUserById?id=15
```

---

# Query Parameters

Query parameters should only be used for filtering, searching, sorting and pagination.

Example

```text
GET /User/GetUsers?page=1&pageSize=20

GET /User/SearchUsers?name=John

GET /User/GetUsers?sort=name
```

---

# HTTP Methods

| Operation | HTTP Method |
| --------- | ----------- |
| Read      | GET         |
| Create    | POST        |
| Update    | PUT         |
| Delete    | DELETE      |

---

# Response Structure

Every endpoint must return the same response model:

```text
ApiResponse<T>
```

Success example

```json
{
    "success": true,
    "message": "User created successfully.",
    "data": {}
}
```

Error example

```json
{
    "success": false,
    "message": "User not found.",
    "data": null
}
```

If the project includes an error-code field, use the codes defined in `ERROR_CODES.md` consistently.

---

# Status Codes

| Code | Description           |
| ---- | --------------------- |
| 200  | Success               |
| 201  | Resource Created      |
| 400  | Bad Request           |
| 401  | Unauthorized          |
| 403  | Forbidden             |
| 404  | Not Found             |
| 409  | Conflict              |
| 500  | Internal Server Error |

---

# Controller Responsibilities

Controllers should only:

* Receive HTTP requests
* Validate request models
* Call services
* Return responses

Controllers must NOT:

* Access repositories
* Access `DbContext`
* Write business logic
* Execute SQL
* Implement authentication business logic
* Manage database transactions

---

# Service Responsibilities

Services contain business logic.

Services may:

* Validate business rules
* Call repositories
* Call external services
* Manage transaction boundaries
* Use mappers

Services should not depend on HTTP-specific controller behavior.

---

# Repository Responsibilities

Repositories are responsible only for database access.

Repositories must never contain business logic.

Repositories may use EF Core and the project `DbContext`.

---

# DTO Usage

Entities must never be returned directly from controllers.

Always use DTO objects.

Good

```text
UserEntity
      ↓
UserResponseDto
      ↓
ApiResponse<UserResponseDto>
```

Bad

```text
UserEntity
      ↓
HTTP Response
```

---

# Validation

All incoming request models must be validated using DataAnnotations.

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

    [Range(1, 150)]
    public int Age { get; init; }
}
```

Business rules that require database access or domain knowledge belong in the service layer.

---

# Naming Standards

Examples

```text
GetUserById

GetUsers

CreateUser

UpdateUser

DeleteUser

Register

Login

Logout

RefreshToken
```

---

# File Upload

```text
POST /File/Upload
```

File type and file size must be validated according to `SECURITY.md`.

---

# Health Check

```text
GET /System/Health
```

---

# Versioning

Future versions should use URL versioning.

Example

```text
/api/v1/User/GetUsers

/api/v2/User/GetUsers
```

---

# OpenAPI

All public endpoints should be represented in the generated OpenAPI document.

Endpoint summaries, parameters, response types and relevant status codes should be documented.

---

# General Rules

* Use PascalCase for controller names.
* Use PascalCase for endpoint names.
* Keep endpoint names descriptive.
* Use route parameters for identifiers.
* Use query parameters only for filtering, searching, sorting and pagination.
* Always return the standard API response model.
* Never expose Entity objects.
* Keep controllers thin.
* Keep business logic inside services.
* Keep repositories responsible only for data access.
