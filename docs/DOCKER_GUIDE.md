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
ConnectionStrings__DefaultConnection=...
Jwt__Secret=...
```

Do not bake secrets into the Docker image.

The Web container uses `AdminSession__Provider`. Local development defaults to `Memory`. To exercise the distributed session store with Docker Compose, set:

```text
ADMIN_SESSION_PROVIDER=Redis
ADMIN_SESSION_REDIS_CONNECTION_STRING=redis:6379,abortConnect=false
```

Production-like deployments must not use the memory provider. Use a managed or otherwise secured Redis deployment, provide the connection string as a secret, and configure shared ASP.NET Core Data Protection keys across Web instances.

---

## Database Migrations

EF Core migrations must be version-controlled.

The deployment strategy must define where `dotnet ef database update` or an equivalent migration step runs.

Do not run ad-hoc SQL schema changes inside production containers.

---

## Docker Philosophy

Everything required to run the local project infrastructure should start with a single command whenever practical:

```bash
docker compose up -d
```

The repository's Dockerfiles and Compose files are the source of truth for actual image names, ports, volumes and service names.
