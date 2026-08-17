# Production Checklist

## Before Deploy

* Set `ASPNETCORE_ENVIRONMENT=Production`.
* Configure explicit `AllowedHosts` for API and Web; do not use `*`.
* Configure HTTPS at the edge/reverse proxy and set trusted `ReverseProxy` entries when forwarded headers are enabled.
* Inject `ConnectionStrings__DefaultConnection` from a secret source.
* Inject `Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenMinutes` and `Jwt__RefreshTokenDays`.
* Configure `AdminSession__Provider=Redis` and inject `AdminSession__RedisConnectionString`.
* Configure persistent `DataProtection__ApplicationName` and absolute `DataProtection__KeyRingPath` for Web.
* Configure persistent local media storage with `ImageStorage__LocalRoot` and `ImageStorage__PublicBaseUrl`, or replace the provider in a future storage phase.
* Configure `AdminApi__BaseUrl`, `PublicApi__BaseUrl` and `PublicMenu__BaseUrl` as HTTPS URLs.
* Review `Database__Retry`, `HttpClients__DefaultTimeoutSeconds` and `RateLimiting` values.
* Build the EF Core migration bundle from the exact release commit.

## Deploy

* Run the migration bundle as a controlled deployment step.
* Stop deployment if the migration bundle fails.
* Start CafeMenu.Api and CafeMenu.Web.
* Confirm `/health/live` returns healthy for both processes.
* Confirm `/health/ready` returns healthy before routing traffic.

## After Deploy

* Smoke test a public menu at `/c/{slug}`.
* Smoke test admin login and authenticated admin navigation.
* Smoke test image upload and public media serving if local media storage is enabled.
* Smoke test logout and confirm server-side session/refresh-token cleanup works.
