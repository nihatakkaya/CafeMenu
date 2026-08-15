# Environment Configuration

## ASP.NET Core Environments

Use environment names consistently.

Recommended environments:

* Development
* Test
* Staging
* Production

Set the active environment with:

```text
ASPNETCORE_ENVIRONMENT=
ASPNETCORE_HTTP_PORTS=
```

---

## Configuration Files

Base non-secret configuration:

```text
appsettings.json
```

Environment-specific non-secret overrides may use:

```text
appsettings.Development.json
appsettings.Test.json
appsettings.Staging.json
appsettings.Production.json
```

Do not store production secrets in these files.

---

## Environment Variables

ASP.NET Core maps double underscores (`__`) to nested configuration keys.

Examples

```text
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_HTTP_PORTS=8080

ConnectionStrings__DefaultConnection=

Jwt__Secret=
Jwt__Issuer=
Jwt__Audience=
Jwt__AccessTokenExpirationMinutes=
Jwt__RefreshTokenExpirationDays=

Database__Retry__Enabled=true
Database__Retry__MaxRetryCount=3
Database__Retry__MaxRetryDelaySeconds=5

ImageStorage__Provider=Local
ImageStorage__LocalRoot=/var/cafemenu/media
ImageStorage__PublicBaseUrl=https://example.com/media
ImageStorage__MaxFileSizeBytes=5242880

HttpClients__DefaultTimeoutSeconds=15

AdminSession__Provider=Memory
AdminSession__KeyPrefix=cafemenu:admin-session:
AdminSession__RedisConnectionString=
AdminSession__MinimumCacheTtlSeconds=1

DataProtection__ApplicationName=CafeMenu.Web
DataProtection__KeyRingPath=

RateLimiting__Login__PermitLimit=10
RateLimiting__Login__WindowSeconds=60
RateLimiting__Refresh__PermitLimit=60
RateLimiting__Refresh__WindowSeconds=60
RateLimiting__AccountSetup__PermitLimit=5
RateLimiting__AccountSetup__WindowSeconds=300
RateLimiting__PlatformUserSetup__PermitLimit=20
RateLimiting__PlatformUserSetup__WindowSeconds=60
```

`AdminSession__Provider=Memory` is only valid for `Development`. Staging and Production must use `AdminSession__Provider=Redis` with `AdminSession__RedisConnectionString` supplied from environment variables or a secret manager. Do not commit Redis passwords or production connection strings.

## Web Data Protection

CafeMenu.Web uses `DataProtection__ApplicationName` and `DataProtection__KeyRingPath` for ASP.NET Core Data Protection key management.

`DataProtection__ApplicationName` defaults to `CafeMenu.Web` and must be identical across Web instances in the same deployment.

In `Development`, `DataProtection__KeyRingPath` may be empty so ASP.NET Core can use its default local development key storage.

Outside `Development`, CafeMenu.Web fails fast if `DataProtection__KeyRingPath` is empty, whitespace, relative or malformed. Configure it to an absolute path on persistent/shared storage, such as a mounted volume managed by the deployment platform.

The key ring is sensitive operational data. Do not store it in the repository or expose it broadly. If multiple Web instances serve the same deployment, they must share the same key ring path and application name. When a hosting provider is selected, configure provider-specific at-rest protection for the key ring separately.

When CafeMenu.Web runs in the production container image, it runs as the Microsoft .NET image built-in non-root `APP_UID` user. Any mounted `DataProtection__KeyRingPath`, such as `/var/cafemenu/data-protection`, must be writable by that non-root user.

## Container Runtime Paths

CafeMenu.Api and CafeMenu.Web container images listen on internal port `8080` through `ASPNETCORE_HTTP_PORTS=8080`.

CafeMenu.Api local media storage uses `/var/cafemenu/media` when `ImageStorage__Provider=Local`. Production deployments that use local media storage must mount persistent storage at that path and ensure the non-root container user can write to it.

Do not bake production secrets, media files or Data Protection keys into Docker images.

## PostgreSQL Transient Retry

CafeMenu.Api uses EF Core's Npgsql execution strategy for provider-detected transient PostgreSQL failures.

Retry configuration is managed through:

```text
Database__Retry__Enabled=true
Database__Retry__MaxRetryCount=3
Database__Retry__MaxRetryDelaySeconds=5
```

Defaults are enabled with `3` retries and a maximum retry delay of `5` seconds. When enabled, `MaxRetryCount` must be between `1` and `10`, and `MaxRetryDelaySeconds` must be between `1` and `60`.

The retry strategy applies only to database failures that the Npgsql provider classifies as transient. Permanent errors such as validation failures, authorization failures, invalid SQL, missing schema or business-rule conflicts are not hidden by retry behavior.

Retry is not a replacement for `/health/ready`; readiness still reports whether PostgreSQL is currently reachable. Explicit EF Core transactions must use the provider execution strategy pattern so retries and user-initiated transactions remain compatible.

## Migration Bundle Configuration

Production database migrations are applied with an EF Core Migration Bundle built from the matching application release:

```powershell
.\scripts\database\build-migration-bundle.ps1 -OutputDirectory .artifacts\migrations
```

The build script does not embed the production connection string. At bundle execution time, provide the database connection string through environment variables or a deployment secret manager:

```text
ConnectionStrings__DefaultConnection=
```

Base `appsettings.json` may travel with application artifacts because it must not contain secrets. Environment-specific appsettings files are only for non-secret overrides. A production deployment must inject the real connection string at runtime.

EF bundle supports a runtime `--connection` option, but using command-line arguments for production secrets can expose them through shell history or process listings. Prefer environment/secret injection unless an operator deliberately chooses a safer platform-specific mechanism.

The migration bundle uses EF Core migration history (`__ef_migrations_history`) to skip already applied migrations. Readiness probes continue to check reachability only; they do not apply schema changes.

## Outbound HTTP Clients

CafeMenu.Web calls CafeMenu.Api through ASP.NET Core `IHttpClientFactory` clients. All registered Web -> API clients use:

```text
HttpClients__DefaultTimeoutSeconds=15
```

The value must be between `1` and `120` seconds and is validated at startup. CafeMenu V1 uses `HttpClient.Timeout` as the single timeout ownership layer for outbound HTTP calls; no separate resilience timeout policy is configured.

Admin/auth/setup/category/product/branding/image upload/platform mutation calls are not automatically retried because they can create, update, revoke, rotate or delete state. Public menu reads are also currently not retried; callers receive the existing safe failure/not-found UI states. Request cancellation tokens are passed through to outbound HTTP calls so browser/request cancellation is not replaced with unrelated background work.

This timeout configuration is independent from PostgreSQL retry, Redis behavior and health/readiness probes. Do not log backend access tokens, refresh tokens, request bodies or full response bodies when handling outbound HTTP failures.

## Rate Limiting

CafeMenu.Api and CafeMenu.Web use the `RateLimiting` section for authentication and setup abuse protection.

Each policy has:

```text
RateLimiting__<PolicyName>__PermitLimit=
RateLimiting__<PolicyName>__WindowSeconds=
```

Both values must be greater than or equal to `1`; invalid values fail startup validation.

Default V1 policies are:

* `Login`: `10` requests per `60` seconds.
* `Refresh`: `60` requests per `60` seconds.
* `AccountSetup`: `5` requests per `300` seconds.
* `PlatformUserSetup`: `20` requests per `60` seconds.

Rejected requests return `429 Too Many Requests`. Anonymous rate limits use `HttpContext.Connection.RemoteIpAddress` after trusted forwarded headers have been applied. Do not use raw `X-Forwarded-For` values as configuration or partition keys.

The built-in ASP.NET Core limiter is process-local. If CafeMenu is horizontally scaled, configure edge, reverse-proxy or distributed rate limiting separately when global limits are required.

## Health Probes

CafeMenu.Api and CafeMenu.Web expose anonymous health probe endpoints:

```text
GET /health/live
GET /health/ready
```

`/health/live` reports whether the application process and HTTP pipeline are alive. It does not check PostgreSQL or Redis, so transient dependency outages do not cause orchestrators to restart otherwise running containers.

`/health/ready` reports whether the application is ready to receive traffic:

* CafeMenu.Api readiness checks PostgreSQL connectivity through the configured EF Core `CafeMenuDbContext`.
* CafeMenu.Web readiness checks Redis only when `AdminSession__Provider=Redis`.
* CafeMenu.Web with `AdminSession__Provider=Memory` remains ready in `Development` without requiring Redis.

Healthy probes return HTTP `200`. Readiness dependency failures return HTTP `503`. Health responses intentionally expose only a small status contract and must not include connection strings, hostnames, credentials or exception details.

## Reverse Proxy / Forwarded Headers

CafeMenu.Api and CafeMenu.Web can be configured to trust forwarded headers from a controlled reverse proxy.

Default local configuration keeps this disabled:

```text
ReverseProxy__Enabled=false
ReverseProxy__ForwardLimit=1
ReverseProxy__KnownProxies__0=
ReverseProxy__KnownIPNetworks__0=
```

When `ReverseProxy__Enabled=true`, startup validation requires `ReverseProxy__ForwardLimit` to be at least `1` and at least one trusted proxy entry in `ReverseProxy__KnownProxies` or `ReverseProxy__KnownIPNetworks`.

Use `ReverseProxy__KnownProxies` for individual proxy IP addresses and `ReverseProxy__KnownIPNetworks` for trusted CIDR ranges, for example `10.0.0.0/24` or `2001:db8::/64`.

Only controlled proxy IP addresses or CIDR ranges may be trusted. Do not clear trusted proxy lists to accept all forwarded headers, and do not enable platform-wide trust-all forwarding switches such as `ASPNETCORE_FORWARDEDHEADERS_ENABLED`.

If the Docker configuration uses separate database variables, they may be defined as:

```text
DB_HOST=
DB_PORT=
DB_NAME=
DB_USERNAME=
DB_PASSWORD=
```

The application configuration must map them consistently.

---

## Local Development Secrets

For local development, prefer .NET User Secrets for application secrets:

```bash
dotnet user-secrets init

dotnet user-secrets set "Jwt:Secret" "local-development-value"
```

When Docker Compose is used locally, a local `.env` file may be used for Compose variables.

Do not commit secret-bearing `.env` files.

---

## Rules

* Never commit production secrets.
* Keep `appsettings.json` environment independent and free of secrets.
* Keep environment-specific files free of real credentials.
* Use environment variables, .NET User Secrets or deployment secret stores for secrets.
* Keep Docker Compose environment-variable names consistent with ASP.NET Core configuration.
* Do not log resolved secret values at startup.
* Store local uploaded media outside the source tree and serve it only through the managed media endpoint.
