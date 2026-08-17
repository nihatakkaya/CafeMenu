# Architectural Decisions

This document records important architectural decisions made for the project.

The filename is intentionally `DECISIONS.md` (the original `DESICIONS.md` spelling has been corrected).

---

## ADR-001

**Decision**

Use C# 14 on .NET 10 LTS.

**Reason**

* Long-term support release
* Modern C# language features
* Cross-platform runtime
* First-class ASP.NET Core and EF Core support

---

## ADR-002

**Decision**

Use ASP.NET Core 10 for the web API.

**Reason**

* Native dependency injection
* Built-in configuration and logging abstractions
* Strong middleware pipeline
* OpenAPI support
* High performance and cross-platform deployment

---

## ADR-003

**Decision**

Use PostgreSQL.

**Reason**

* Open source
* High performance
* Strong transactional and relational capabilities
* Mature EF Core support through Npgsql

---

## ADR-004

**Decision**

Use Entity Framework Core 10 and EF Core Migrations.

**Reason**

* Native .NET ORM
* LINQ-based data access
* Version-controlled schema migrations
* Strong ASP.NET Core integration

---

## ADR-005

**Decision**

Use Docker for project infrastructure and Docker Compose for local orchestration.

**Reason**

* Consistent environments
* Easy onboarding
* Reproducible local infrastructure
* Production-like dependency setup

---

## ADR-006

**Decision**

Use Layered Architecture.

**Reason**

* Simple
* Maintainable
* Familiar to .NET developers
* Clear separation between API, business logic and data access

---

## ADR-007

**Decision**

Use Action-Based API endpoints.

Example

```text
/User/GetUserById/{id}
```

**Reason**

Project-wide consistency and compatibility with the original project conventions.

Do not switch to REST-style resource endpoints without an explicit architectural decision.

---

## ADR-008

**Decision**

Never expose Entity objects directly through the API.

**Reason**

Security, flexibility, API stability and separation of persistence models from public contracts.

---

## ADR-009

**Decision**

Use Constructor Injection with ASP.NET Core's built-in dependency injection container.

**Reason**

* Explicit dependencies
* Easier testing
* Cleaner code
* Better immutability

---

## ADR-010

**Decision**

Use Mapperly for Entity ↔ DTO mappings.

**Reason**

* Build-time generated mapping code
* No runtime reflection for ordinary mappings
* Avoid repetitive controller mapping code
* Apache 2.0 licensed

---

## ADR-011

**Decision**

Use DataAnnotations for request-model validation and service-layer validation for business rules.

**Reason**

* Native ASP.NET Core model validation support
* Minimal additional dependencies
* Clear distinction between input-shape validation and business validation

---

## ADR-012

**Decision**

Use `ILogger<T>` as the application logging abstraction. Serilog may be configured as the provider for structured logging and external sinks.

**Reason**

* Native ASP.NET Core integration
* Testable logging abstraction
* Structured logging support
* Avoid direct console logging in application code
