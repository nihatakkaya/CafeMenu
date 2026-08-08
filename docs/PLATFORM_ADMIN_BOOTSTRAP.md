# Platform Admin Bootstrap

## Purpose

V1 does not support public self-registration.

Anonymous customers do not have accounts, and cafe owner or cafe manager accounts must be created or assigned by a trusted platform administration flow.

The first `PLATFORM_ADMIN` account must therefore be bootstrapped through a controlled operational process, not through a public API endpoint.

## Rules

Never commit:

* plaintext passwords
* password hashes for production accounts
* JWT signing keys
* bootstrap credentials
* production `.env` files

Never seed a production password from:

* source code
* EF Core migrations
* Docker Compose files
* plaintext environment variables
* CI logs or command history

## Recommended Production Bootstrap

Use a one-time operational command or maintenance job that runs inside the trusted production environment after migrations are applied.

The command should:

1. Require operator access to the production environment.
2. Read the admin email from an approved secret/configuration source.
3. Prompt for the initial password interactively with terminal echo disabled, or read a precomputed BCrypt password hash from a secret manager.
4. Hash plaintext input in memory using BCrypt if interactive input is used.
5. Create the `AppUserEntity` only if it does not already exist.
6. Assign only the seeded `PLATFORM_ADMIN` role.
7. Log only the account id/email and bootstrap result, never the password, token or hash.
8. Be disabled, removed from deployment execution, or guarded after the first successful bootstrap.

For fully automated production environments, prefer storing a precomputed BCrypt password hash in a secret manager instead of storing a plaintext password. Rotate the password immediately after first sign-in.

## Local Development

Local development may use developer-only secrets or an interactive local bootstrap command.

Development credentials must stay outside the repository and must not be copied into `.env.example`, `appsettings.json`, migrations or test fixtures intended for production.

## Current Implementation Status

The authentication foundation seeds only role metadata:

* `PLATFORM_ADMIN`
* `CAFE_OWNER`
* `CAFE_MANAGER`

No production user credentials are seeded.

`POST /Authentication/Register` is not part of the public V1 API surface.

## Integration Testing Strategy

`Microsoft.EntityFrameworkCore.InMemory` may be used only for tests that do not depend on relational database behavior.

Use real PostgreSQL integration tests with Testcontainers before implementing features that require PostgreSQL-specific guarantees, including:

* unique index behavior
* relational constraints
* transaction behavior
* migration validation
* tenant isolation
* cafe-scoped authorization

Do not rely on InMemory tests for future tenant-isolation or PostgreSQL-specific behavior.
