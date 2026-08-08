# Implementation Plan

## Current Repository State

The repository currently contains project standards and documentation only:

* `AGENTS.md`
* `docs/`

No application solution, source project or test project exists yet.

## Documentation Completed Before Application Code

Product-specific documents:

* `docs/PRODUCT_REQUIREMENTS.md`
* `docs/DATA_MODEL.md`
* `docs/MULTI_TENANCY.md`
* `docs/UI_BRANDING.md`

These documents are product-specific and do not replace the general repository standards.

## Convention Check

The product direction is compatible with the repository rules with these decisions:

* Keep action-based API endpoints.
* Keep `ApiResponse<T>` for all API responses.
* Keep Controller -> Service -> Repository -> DbContext flow.
* Keep PostgreSQL and EF Core migrations.
* Keep Mapperly for Entity/DTO mapping.
* Keep DataAnnotations for request validation.
* Keep JWT access token and refresh token authentication.
* Keep BCrypt for passwords.
* Keep server-side tenant authorization.

Potential conflict reviewed:

* The product asks for Blazor Web App. The repository currently defines ASP.NET Core API architecture and does not forbid Blazor. Use Blazor Web App only as the web UI layer while preserving backend API conventions, service/repository separation and authorization rules.

## Proposed Solution Structure

Start with a modular monolith:

```text
CafeMenu.sln

src/
└── CafeMenu.Api/
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
    ├── Utilities/
    └── Migrations/

tests/
└── CafeMenu.Tests/

docker-compose.yml
```

If Blazor UI is separated later:

```text
src/
├── CafeMenu.Api/
└── CafeMenu.Web/
```

The first implementation can also host Blazor in the ASP.NET Core application if that keeps the project simpler.

## Proposed Domain/Data Model

Initial entities:

* `AppUserEntity`
* `RoleEntity`
* `CafeEntity`
* `CafeMembershipEntity`
* `CafeThemeEntity`
* `CategoryEntity`
* `ProductEntity`
* `RefreshTokenEntity`
* `AuditLogEntity`

Follow `docs/DATA_MODEL.md` for fields, relationships, constraints, indexes and soft-delete behavior.

## Multi-Tenant Authorization Strategy

Administration access must be resolved server-side:

1. Authenticate user with JWT access token.
2. Resolve active user identity.
3. For platform actions, require `PLATFORM_ADMIN`.
4. For cafe actions, verify active `CafeMembershipEntity`.
5. Verify the role/policy allows the action.
6. Load target resources scoped by `CafeId`.
7. Reject cross-tenant access even when the attacker supplies valid Cafe B identifiers.

Do not trust `CafeId` supplied by the client as proof of cafe access.

Public menu access is anonymous and resolved by cafe slug with public visibility filters.

## Required Infrastructure

Initial infrastructure:

* PostgreSQL database
* Docker Compose for local PostgreSQL
* ASP.NET Core configuration/options
* Central exception handling
* JWT authentication and authorization policies
* EF Core DbContext and migrations
* Local filesystem file storage implementation
* OpenAPI/Swagger
* xUnit test project

## Required NuGet Dependencies

Expected dependencies:

* `Microsoft.EntityFrameworkCore`
* `Microsoft.EntityFrameworkCore.Design`
* `Npgsql.EntityFrameworkCore.PostgreSQL`
* `Riok.Mapperly`
* `Microsoft.AspNetCore.Authentication.JwtBearer`
* `BCrypt.Net-Next`
* `QRCoder`
* `xunit`
* `Microsoft.NET.Test.Sdk`
* `Microsoft.AspNetCore.Mvc.Testing`
* `Microsoft.EntityFrameworkCore.InMemory` or a PostgreSQL test-container strategy for tests

Use exact versions compatible with .NET 10 and the repository standards during implementation.

## Incremental Implementation Progression

Do not build everything in one uncontrolled change.

Recommended increments:

1. Foundation solution, shared response model, exception handling, configuration and Docker Compose.
2. Authentication entities, token flow, BCrypt password hashing and refresh token persistence.
3. Platform cafe management.
4. Cafe membership and tenant authorization policies.
5. Cafe branding/theme management.
6. Category management.
7. Product management.
8. Image/file storage.
9. Public QR menu API and public UI.
10. QR generation and download.
11. Admin UI.
12. Public mobile UI hardening.
13. Test coverage and Docker verification.

Each feature must follow `docs/DEVELOPMENT_GUIDE.md`:

```text
Entity / Domain Model
EF Core Configuration / DbContext changes
EF Core Migration
Repository
DTOs
Mapper
Service
Controller
Validation
OpenAPI / Swagger Documentation
Tests
```

## Testing Requirements

Minimum tests:

* Authentication
* Authorization
* Tenant isolation
* Cafe access
* Category creation
* Category update
* Category soft delete
* Product creation
* Product update
* Product soft delete
* Unavailable product behavior
* Hidden product behavior
* Public menu filtering
* Unique cafe slug enforcement

Critical tenant isolation tests:

* User authorized for Cafe A cannot read Cafe B private administration data.
* User authorized for Cafe A cannot update or delete Cafe B resources even when submitting Cafe B identifiers.

## Final Verification Checklist

Before claiming Version 1 complete:

* Restore dependencies.
* Build the entire solution.
* Run all tests.
* Verify EF Core migrations.
* Verify PostgreSQL startup path.
* Verify Docker Compose startup.
* Verify authorization.
* Verify cross-tenant isolation tests.
* Verify anonymous public menu behavior.
* Verify soft-delete filtering.
* Verify file upload validation.
* Verify no production secret was committed.

