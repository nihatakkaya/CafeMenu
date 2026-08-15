# Database Conventions

## Purpose

This document defines the database standards for the project.

All database objects must follow these conventions.

---

# Database

Database Engine

```text
PostgreSQL
```

ORM / Data Access

```text
Entity Framework Core 10
Npgsql PostgreSQL provider
```

---

# Naming Convention

All physical database object names must use **snake_case**.

Good

```text
app_user
user_role
refresh_token
audit_log
```

Bad

```text
AppUser
UserRole
USER_ROLE
userRole
```

C# property names remain PascalCase; database names remain snake_case.

---

# Table Naming

Use singular names.

Good

```text
app_user
role
permission
category
product
refresh_token
```

Bad

```text
users
roles
categories
```

Avoid PostgreSQL/SQL reserved words. Use `app_user` instead of `user` when necessary.

---

# Column Naming

Use snake_case.

Good

```text
first_name
last_name
email
created_at
updated_at
```

Bad

```text
FirstName
firstName
FIRST_NAME
```

---

# Primary Key

Every table must have a primary key named:

```text
id
```

Default type:

```text
BIGINT
```

Use PostgreSQL identity generation unless a business requirement specifies another strategy.

---

# Foreign Keys

Always use:

```text
table_name_id
```

Examples

```text
user_id
role_id
category_id
product_id
```

If the referenced table uses a disambiguated name such as `app_user`, keep foreign-key naming clear and consistent with the project model.

---

# Boolean Columns

Boolean columns should start with:

```text
is_
```

Examples

```text
is_active
is_deleted
is_locked
is_verified
```

---

# Date Columns

Use timestamp fields appropriate for UTC application timestamps.

Required audit fields

```text
created_at
updated_at
```

Optional

```text
deleted_at
last_login_at
```

Application code should handle timestamps consistently in UTC unless a documented business requirement states otherwise.

---

# Audit Fields

Every table should contain:

```text
created_at
updated_at
```

Optional

```text
created_by
updated_by
```

---

# Soft Delete

Never physically delete records unless absolutely necessary or explicitly required by the business domain.

Use:

```text
is_deleted
```

Optional:

```text
deleted_at
```

If global query filters are used in EF Core, they must be configured consistently and documented.

---

# Unique Constraints

Use unique constraints whenever applicable.

Examples

```text
email
username
code
```

---

# Index Naming

Format

```text
idx_table_column
```

Examples

```text
idx_app_user_email
idx_product_name
idx_category_code
```

---

# Unique Index / Constraint Naming

Format

```text
uk_table_column
```

Examples

```text
uk_app_user_email
uk_role_name
```

---

# Foreign Key Naming

Format

```text
fk_child_parent
```

Examples

```text
fk_user_role
fk_product_category
```

---

# Primary Key Naming

Format

```text
pk_table
```

Examples

```text
pk_app_user
pk_product
```

---

# Migration Management

All schema changes must be managed using Entity Framework Core Migrations.

Default migration folder

```text
Migrations/
```

Create migrations with descriptive PascalCase names.

Examples

```bash
dotnet ef migrations add InitialSchema

dotnet ef migrations add CreateUserTable

dotnet ef migrations add InsertDefaultRoles
```

Apply locally when appropriate:

```bash
dotnet ef database update
```

Never change a migration already applied to a shared or production database.

Create a new migration instead.

Migration source files must be committed to Git.

---

# PostgreSQL Transient Retry

Application runtime database access may use EF Core's Npgsql execution strategy with `EnableRetryOnFailure` for provider-detected transient PostgreSQL errors.

Retry configuration must remain bounded and configurable. CafeMenu V1 defaults to enabled retry with a maximum of `3` retries and a maximum delay of `5` seconds. Retry must not be implemented with custom catch-all loops around repositories or services.

Permanent database errors, validation errors, authorization failures and business-rule conflicts must not be treated as transient retry cases.

When service-layer operations create explicit EF Core transactions, they must run inside the provider execution strategy's `ExecuteAsync` pattern. Automatic migrations at application startup remain prohibited.

---

# Enum Storage

Store business enums as readable string values when persistence stability and database readability are important.

Example

```text
ADMIN
USER
MANAGER
```

Avoid persisting enum ordinal/integer values when changing enum order could alter meaning.

Configure EF Core value conversions explicitly when string storage is required.

---

# Junction Tables

Use singular names.

Examples

```text
user_role
role_permission
user_permission
```

---

# Reserved Words

Avoid reserved SQL/PostgreSQL keywords.

Bad

```text
user
order
group
```

Prefer

```text
app_user
customer_order
user_group
```

---

# Default Values

Define database defaults when appropriate.

Examples

```text
is_active = true
is_deleted = false
created_at = CURRENT_TIMESTAMP
```

Defaults must also be understood by the EF Core model so application and database behavior do not conflict.

---

# Nullable Columns

Columns should be `NOT NULL` by default.

Allow `NULL` only when it has a valid business meaning.

C# nullable reference/value types must reflect database nullability.

---

# UUID / Guid

Use `BIGINT` IDs by default unless there is a business requirement for UUID/Guid identifiers.

UUID/Guid may be used for:

* Public identifiers
* API keys
* Tokens
* External integrations
* Distributed identifier requirements

The choice must be consistent across Entity, migration and API models.

---

# Cascade Rules

Avoid broad cascade-delete behavior unless absolutely necessary.

Explicitly define relationship delete behavior in EF Core.

Use `DeleteBehavior` configuration deliberately rather than relying on accidental defaults for critical relationships.

---

# Transactions

Business transaction boundaries must be handled by the service layer.

Repositories may participate in the current EF Core unit of work/transaction but must not define business transaction boundaries.

Database transactions must never be managed inside controllers.

---

# Seed Data

Initial/required data must be managed through a version-controlled EF Core migration or a documented EF Core seeding strategy.

Examples

* Default Roles
* Default Admin User when securely provisioned
* System Settings

Never rely on manual production database edits for required seed data.

Never place real production passwords or secrets in seed code or migrations.

---

# Database Checklist

Before creating a new table:

* Table name uses snake_case
* Table name is singular
* Reserved words are avoided
* Primary key is named `id`
* Foreign keys follow the project naming convention
* Audit fields exist
* Required indexes are created
* Unique constraints are defined
* Default values are configured
* Nullability is intentional
* EF Core migration is created
* Entity configuration matches the migration
* Naming conventions are followed
