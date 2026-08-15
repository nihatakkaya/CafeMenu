# Docker Guide

## Containers

The project uses Docker Compose.

Typical services:

* ASP.NET Core API
* ASP.NET Core Web
* PostgreSQL
* Redis
* pgAdmin (optional for development)

Suggested Compose service names:

```text
api
postgres
redis
pgadmin
```

If your own `docker-compose.yml` uses different service names, update the command examples in this document to match the actual file.

---

## Development

Start

```bash
docker compose up -d
```

Stop

```bash
docker compose down
```

Restart

```bash
docker compose restart
```

---

## Build

```bash
docker compose up --build -d
```

---

## Logs

All services

```bash
docker compose logs -f
```

API only

```bash
docker compose logs -f api
```

Database only

```bash
docker compose logs -f postgres
```

Redis only

```bash
docker compose logs -f redis
```

---

## Remove Containers

Remove containers and network while keeping named volumes:

```bash
docker compose down
```

Remove containers and volumes:

```bash
docker compose down -v
```

Use `-v` carefully because database volume data may be deleted.

---

## Images

```bash
docker image ls
```

---

## Volumes

```bash
docker volume ls
```

---

## Networks

```bash
docker network ls
```

---

## Environment Variables

Docker Compose may use a local:

```text
.env
```

Never commit production credentials.

Keep secret-bearing `.env` files in `.gitignore`.

Use deployment secrets/environment variables in production.

---

## ASP.NET Core Container Configuration

The API container should receive configuration through environment variables.

Examples

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_HTTP_PORTS=8080
ConnectionStrings__DefaultConnection=...
Database__Retry__Enabled=true
Database__Retry__MaxRetryCount=3
Database__Retry__MaxRetryDelaySeconds=5
Jwt__Secret=...
```

Do not bake secrets into the Docker image.

CafeMenu.Api and CafeMenu.Web images listen on internal container port `8080`. Host port mappings are deployment-specific and must be configured outside the image, such as through Docker Compose or the hosting platform.

Both runtime images use the Microsoft .NET image built-in non-root `APP_UID` user. Deployment volumes mounted for writable application data must allow this non-root user to read and write the mount path.

CafeMenu.Api uses `/var/cafemenu/media` as the documented local media mount point when `ImageStorage__Provider=Local`. The development Compose file mounts the `media_data` named volume there.

CafeMenu.Web production deployments must configure `DataProtection__KeyRingPath` to a persistent/shared mounted path, for example `/var/cafemenu/data-protection`. The image prepares this path for the non-root runtime user, but production ownership and permissions remain a deployment responsibility when external volumes are mounted.

The Web container uses `AdminSession__Provider`. Local development defaults to `Memory`. To exercise the distributed session store with Docker Compose, set:

```text
ADMIN_SESSION_PROVIDER=Redis
ADMIN_SESSION_REDIS_CONNECTION_STRING=redis:6379,abortConnect=false
```

Production-like deployments must not use the memory provider. Use a managed or otherwise secured Redis deployment, provide the connection string as a secret, and configure shared ASP.NET Core Data Protection keys across Web instances.

## Allowed Hosts

CafeMenu.Api and CafeMenu.Web use ASP.NET Core Host Filtering. Local Docker development runs in `Development`, where the project appsettings allow the local browser hosts and Compose service DNS names needed by the current topology:

* API: `localhost`, `127.0.0.1`, `[::1]`, `api`
* Web: `localhost`, `127.0.0.1`, `[::1]`, `web`

Production deployments must set `AllowedHosts` explicitly through environment/deployment configuration, for example:

```text
AllowedHosts=web.example.com
```

Do not bake real production hostnames into Docker images. Do not include URL schemes or ports in `AllowedHosts`; Docker/Kestrel port binding is separate from Host header allow-listing.

---

## Database Migrations

EF Core migrations must be version-controlled.

Production schema changes are applied through an EF Core Migration Bundle built from the same commit/version as the application release:

```powershell
.\scripts\database\build-migration-bundle.ps1 -OutputDirectory .artifacts\migrations
```

The bundle is a generated deployment artifact and must not be committed to Git. It should run as a controlled deployment or maintenance step before the new API/Web version receives traffic.

Do not run ad-hoc SQL schema changes inside production containers.

Do not add `dotnet ef database update`, migration bundle execution or other schema mutation commands to API/Web Docker image startup or ENTRYPOINT scripts. The application containers start only the application processes. Production provider selection will determine where the separate migration step runs.

The migration step must receive the production database connection string through environment variables or a deployment secret manager, using the normal ASP.NET Core configuration key:

```text
ConnectionStrings__DefaultConnection=
```

If a migration bundle fails, stop the deployment and investigate. Do not make automatic down-migration rollback part of normal container startup.

---

## PostgreSQL Transient Retry

CafeMenu.Api enables bounded EF Core/Npgsql transient retry by default for normal runtime database operations.

Default configuration:

```text
Database__Retry__Enabled=true
Database__Retry__MaxRetryCount=3
Database__Retry__MaxRetryDelaySeconds=5
```

This retry behavior handles only failures that the PostgreSQL provider classifies as transient. It does not run migrations, does not mask permanent configuration/schema errors and does not replace `/health/ready` dependency probes.

---

## Health Probes

CafeMenu.Api and CafeMenu.Web expose provider-independent probe endpoints for deployment platforms:

```text
GET /health/live
GET /health/ready
```

Use `/health/live` for liveness checks. It verifies that the process and HTTP pipeline are responding and does not fail when PostgreSQL or Redis is temporarily unavailable.

Use `/health/ready` for readiness checks before routing traffic:

* API readiness checks PostgreSQL connectivity.
* Web readiness checks Redis when `AdminSession__Provider=Redis`.
* Web readiness does not require Redis when the development-only memory session provider is active.

Healthy probes return HTTP `200`. Unhealthy readiness probes return HTTP `503` with a minimal status-only JSON response.

The application images intentionally do not install curl, wget or extra shell tooling only for image-level `HEALTHCHECK` instructions. Use orchestrator, reverse-proxy or load-balancer probes against `/health/live` and `/health/ready`. The PostgreSQL and Redis development Compose services keep their own native healthchecks.

---

## Docker Philosophy

Everything required to run the local project infrastructure should start with a single command whenever practical:

```bash
docker compose up -d
```

The repository's Dockerfiles and Compose files are the source of truth for actual image names, ports, volumes and service names.
