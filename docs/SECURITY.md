# Security Guide

## Authentication

* ASP.NET Core Authentication
* JWT Access Token
* JWT Refresh Token

---

## Passwords

* BCrypt
* Use a maintained .NET BCrypt implementation.
* Never store plain text passwords.
* Never return password hashes through API DTOs.

---

## Authorization

* Role-Based Authorization
* Policy-Based Authorization when appropriate
* Use ASP.NET Core `[Authorize]` attributes and authorization policies

Authorization rules should be configured centrally and applied consistently.

---

## Secrets

Store secrets outside committed source code.

Allowed mechanisms include:

* Environment variables
* .NET User Secrets for local development
* A deployment secret manager when available

Never commit secrets.

Do not put production secrets in `appsettings.json`, `appsettings.Production.json`, Dockerfiles or GitHub workflow files.

---

## Token Expiration

Access Token

* Short lifetime

Refresh Token

* Longer lifetime

Refresh tokens should be revocable and stored/handled securely according to the authentication design.

---

## HTTPS

Production environments must always use HTTPS.

Authentication tokens must not be transmitted over unencrypted HTTP in production.

---

## CORS

Only trusted origins should be allowed.

Do not use unrestricted CORS in production unless there is a documented requirement and security review.

---

## Logging

Never log:

* Passwords
* Password hashes
* JWT Access Tokens
* Refresh Tokens
* API keys
* Database passwords
* Secrets

Use structured logging through `ILogger<T>`.

---

## File Upload

Validate:

* File type
* File size
* File extension and content type where relevant
* Destination/path handling

Never trust a client-supplied filename as a safe server path.

---

## SQL Injection

Use Entity Framework Core LINQ queries or parameterized queries.

Never build SQL using untrusted string concatenation or interpolation.

If raw SQL is necessary, use EF Core parameterization APIs and review the query carefully.

---

## Configuration

Bind security-related settings through ASP.NET Core configuration/options.

Validate required security settings at startup where practical.

Never hard-code JWT secrets or credentials in source code.

---

## GitHub

Never commit credentials to GitHub.

For CI/CD, store sensitive values in GitHub Actions Secrets or the deployment platform's secret store.

Do not print secrets in workflow logs.
