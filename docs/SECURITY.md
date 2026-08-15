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

## Admin Web Session Token Store

CafeMenu.Web must not expose backend JWT access or refresh tokens to browser JavaScript or store them in browser storage.

The `CafeMenu.Admin` browser cookie stores the authenticated admin identity claims and an opaque server-generated session identifier. Backend JWT access tokens and refresh tokens stay server-side in `IAdminSessionTokenStore`.

The development `MemoryAdminSessionTokenStore` is process-local. It is allowed only in the `Development` environment because a Web process restart loses the in-memory refresh token and cannot revoke it during logout.

Production-like environments must set `AdminSession:Provider=Redis` and provide `AdminSession:RedisConnectionString` through environment variables or a deployment secret manager. The application fails fast if the process-local memory store would be used outside `Development`, if the Redis connection string is missing for the Redis provider, or if unsupported session provider/TTL configuration is supplied.

Admin session cache entries expire no later than the stored refresh token expiry. Expired or malformed session entries must be treated as missing sessions and must not expose token values in responses, logs or error messages.

Production deployments must also use shared ASP.NET Core Data Protection key management across Web instances so the `CafeMenu.Admin` cookie can be validated after restarts and across instances.

## Web Data Protection Key Ring

CafeMenu.Web uses ASP.NET Core Data Protection to protect the `CafeMenu.Admin` authentication cookie.

All production CafeMenu.Web instances for the same deployment must use the same `DataProtection:ApplicationName` and the same persistent/shared `DataProtection:KeyRingPath`. Without a shared key ring, admin cookies may become unreadable after container restarts, redeployments or when traffic moves between Web instances.

`DataProtection:KeyRingPath` must point to persistent operational storage outside source control. Key ring files are sensitive operational material and should be readable only by the Web process identity and authorized operators. Backup and recovery plans must preserve the key ring alongside other deployment state.

Do not commit key ring files or place production key material in the repository. When a deployment provider is selected, configure an appropriate provider-specific encryption-at-rest mechanism separately.

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

Uploaded menu images must use server-generated opaque filenames and be stored outside the source tree. For V1, accepted image formats are JPEG, PNG and WebP only. SVG, GIF and unknown formats must be rejected. Image validation must check extension, content type and file signature, and accepted images should be decoded and re-encoded before storage to avoid preserving unsafe metadata.

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
