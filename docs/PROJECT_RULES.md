# Project Rules

These rules are mandatory.

---

## Architecture

* Controller → Service → Repository → DbContext/Database only.
* Controllers must never access repositories directly.
* Controllers must never access `DbContext` directly.
* Repositories must never call services.

---

## Database

* Entity Framework Core Migrations are mandatory for schema changes.
* Database changes require a migration.
* Never modify a migration already applied to a shared or production database.
* Create a new migration for subsequent schema changes.

---

## API

* Use Action-Based endpoints.
* Return `ApiResponse<T>`.
* Never expose Entity objects.

---

## Dependency Injection

* Constructor Injection only.
* Use the built-in ASP.NET Core dependency injection container.
* Service Locator usage inside application/business code is prohibited when constructor injection is possible.

---

## Validation

* Validate every request using DataAnnotations.
* Business validation belongs in the service layer.

---

## Mapping

* Mapperly is mandatory for Entity ↔ DTO mapping.
* Manual mapping inside controllers is prohibited.

---

## Logging

* Never use `Console.WriteLine()` for application logging.
* Use `ILogger<T>`.
* Serilog may be used for structured logging.

---

## Transactions

* Transaction boundaries belong to the service layer.
* Controllers must never start or manage database transactions.

---

## Security

* Passwords must be hashed using BCrypt.
* JWT authentication is required.
* Refresh Token support is required.
* Secrets must never be hard-coded or committed.

---

## Configuration

* Use `appsettings.json` for non-secret base configuration.
* Use environment-specific `appsettings.{Environment}.json` files only for non-secret environment overrides.
* Use environment variables or .NET User Secrets for secrets.
* Never commit production secrets.

---

## General

* Follow SOLID.
* Follow Clean Code.
* Keep methods short.
* Keep classes focused.
* Write readable code.
* Enable nullable reference types.
