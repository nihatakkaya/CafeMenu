# Git & GitHub Conventions

## Purpose

This document defines the Git and GitHub workflow and commit standards for the project.

All developers should follow these conventions.

---

# Repository

Use the project's own GitHub repository.

Do not hard-code another person's repository URL into documentation or scripts.

The configured Git remote should point to the repository owned or managed by this project/team.

Check it with:

```bash
git remote -v
```

---

# Branch Strategy

Main branches

```text
main
develop
```

Feature branches

```text
feature/authentication
feature/user-management
feature/category-crud
feature/product-crud
```

Bug fixes

```text
bugfix/login-error
bugfix/token-validation
```

Hotfixes

```text
hotfix/security-patch
hotfix/database-fix
```

Release branches

```text
release/v1.0.0
```

For a small project, the team may explicitly simplify this workflow, but the chosen strategy must be used consistently.

---

# Commit Message Format

Use the following format:

```text
type: short description
```

Examples

```text
feat: add jwt authentication
feat: create user crud
fix: resolve login validation bug
refactor: simplify user service
docs: update api conventions
style: format project
test: add user service tests
chore: update dependencies
build: update docker image
ci: add github actions build
```

---

# Commit Types

| Type     | Description                                |
| -------- | ------------------------------------------ |
| feat     | New feature                                |
| fix      | Bug fix                                    |
| refactor | Code improvement without changing behavior |
| docs     | Documentation changes                      |
| style    | Formatting and code style                  |
| test     | Tests                                      |
| chore    | Maintenance tasks                          |
| build    | Build configuration                        |
| ci       | CI/CD changes                              |

---

# Pull Request Rules

Before creating a Pull Request:

* `dotnet build` succeeds
* All tests pass with `dotnet test`
* Code follows conventions
* No secrets are included
* Documentation is updated if necessary
* EF Core migrations are included when the database schema changed

Pull Request title example

```text
feat: implement jwt authentication
```

---

# Merge Strategy

Use:

```text
Squash and Merge
```

whenever possible to keep history clean.

The repository owner may choose another GitHub merge policy, but the repository configuration and this document must agree.

---

# Code Review Checklist

Before approving a Pull Request:

* Code is readable
* No duplicate code
* Business logic is inside services
* Controllers do not access repositories or DbContext directly
* Naming conventions are followed
* DTOs are used
* Validation exists
* Exceptions are handled correctly
* No commented-out code remains without a valid reason
* No debug code remains
* No secrets are present
* Database migrations are reviewed when included

---

# Ignore Rules

Never commit:

* IDE user settings
* Temporary files
* Logs
* Build output
* Secrets
* Local secret-bearing environment files
* Local database files unless explicitly required

Recommended `.gitignore` entries include:

```text
.vs/
.idea/
.vscode/*
!.vscode/extensions.json
!.vscode/settings.json

bin/
obj/
TestResults/

*.user
*.suo
*.log

.env
.env.*
!.env.example

appsettings.Local.json
appsettings.*.local.json
```

Do not ignore source-controlled EF Core migration files.

---

# GitHub Actions

GitHub Actions workflows belong in:

```text
.github/workflows/
```

A basic CI pipeline should normally run:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
```

CI/CD secrets must be stored in GitHub Actions Secrets or the deployment platform's secret store.

Never write secret values directly into workflow YAML files.

---

# Version Tags

Use Semantic Versioning.

Examples

```text
v1.0.0
v1.1.0
v1.1.1
v2.0.0
```

---

# Git Workflow

```text
develop
    ↓
feature/new-feature
    ↓
Commit
    ↓
Push to own GitHub repository
    ↓
Pull Request
    ↓
GitHub Actions / CI
    ↓
Code Review
    ↓
Merge into develop
    ↓
Release
    ↓
Merge into main
```

---

# General Rules

* Commit often with meaningful messages.
* Keep commits focused on a single purpose.
* Do not mix unrelated changes in one commit.
* Pull/fetch before starting work on a shared branch.
* Resolve conflicts carefully.
* Never commit secrets or credentials.
* Keep the Git history clean and understandable.
* Keep repository documentation consistent with the actual GitHub branch and merge settings.
