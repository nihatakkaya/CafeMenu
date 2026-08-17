# Data Model

## Overview

The application is a multi-tenant SaaS platform. A cafe is the tenant. Cafe-owned administration data must have an explicit relationship to `CafeEntity`.

The initial domain should be implemented as a modular monolith using the repository architecture:

```text
Controller -> Service -> Repository -> DbContext / Database
```

The default identifier type is `long` mapped to PostgreSQL `BIGINT`, following `docs/DATABASE_CONVENTIONS.md`.

Physical table, column, constraint and index names must use `snake_case`. Table names must be singular.

## Proposed Entities

### AppUserEntity

Table: `app_user`

Purpose: Administration user account.

Important fields:

* `Id`
* `Email`
* `PasswordHash`
* `FullName`
* `IsActive`
* `LastLoginAt`
* `CreatedAt`
* `UpdatedAt`
* `IsDeleted`
* `DeletedAt`

Constraints and indexes:

* Primary key: `pk_app_user`
* Unique email: `uk_app_user_email`
* Index for active user lookup as needed

Notes:

* Never expose `PasswordHash` through API DTOs.
* Store passwords with BCrypt only.

### RoleEntity

Table: `role`

Purpose: Platform and cafe role definitions.

Initial role codes:

* `PLATFORM_ADMIN`
* `CAFE_OWNER`
* `CAFE_MANAGER`

Important fields:

* `Id`
* `Code`
* `Name`
* `Description`
* `CreatedAt`
* `UpdatedAt`

Constraints and indexes:

* Primary key: `pk_role`
* Unique code: `uk_role_code`

Enum-like role values should be persisted as stable readable strings where appropriate.

### CafeEntity

Table: `cafe`

Purpose: Tenant root.

Important fields:

* `Id`
* `Name`
* `Slug`
* `LogoImageUrl`
* `CoverImageUrl`
* `IsActive`
* `IsPublished`
* `CreatedAt`
* `UpdatedAt`
* `IsDeleted`
* `DeletedAt`

Constraints and indexes:

* Primary key: `pk_cafe`
* Globally unique slug: `uk_cafe_slug`
* Slug lookup index: `idx_cafe_slug`

Notes:

* Public menu lookup uses `Slug`.
* Inactive, unpublished or soft-deleted cafes must not appear publicly.

### CafeMembershipEntity

Table: `cafe_membership`

Purpose: User-to-cafe membership and cafe-scoped role assignment.

Important fields:

* `Id`
* `AppUserId`
* `CafeId`
* `RoleId`
* `IsActive`
* `CreatedAt`
* `UpdatedAt`
* `IsDeleted`
* `DeletedAt`

Relationships:

* Many memberships belong to one `AppUserEntity`.
* Many memberships belong to one `CafeEntity`.
* Many memberships reference one `RoleEntity`.

Constraints and indexes:

* Primary key: `pk_cafe_membership`
* Foreign keys:
  * `fk_cafe_membership_app_user`
  * `fk_cafe_membership_cafe`
  * `fk_cafe_membership_role`
* Unique active membership where practical: `uk_cafe_membership_app_user_cafe_role`
* User/cafe lookup index: `idx_cafe_membership_app_user_cafe`
* Cafe membership index: `idx_cafe_membership_cafe`

Notes:

* The model supports one user managing multiple cafes.
* Authorization checks must use this membership table, not client-supplied cafe IDs.

### CafeThemeEntity

Table: `cafe_theme`

Purpose: Controlled public branding settings for a cafe.

Important fields:

* `Id`
* `CafeId`
* `PrimaryColor`
* `SecondaryColor`
* `AccentColor`
* `BackgroundColor`
* `TextColor`
* `WelcomeTitle`
* `WelcomeDescription`
* `FontPreset`
* `ThemePreset`
* `IsPublished`
* `CreatedAt`
* `UpdatedAt`

Constraints and indexes:

* Primary key: `pk_cafe_theme`
* Foreign key: `fk_cafe_theme_cafe`
* Unique cafe theme relationship: `uk_cafe_theme_cafe`

Notes:

* Do not store arbitrary HTML, CSS or JavaScript.
* Validate colors and preset values.

### CategoryEntity

Table: `category`

Purpose: Cafe-owned menu category.

Important fields:

* `Id`
* `CafeId`
* `Name`
* `Description`
* `ImageUrl`
* `DisplayOrder`
* `IsVisible`
* `IsPublished`
* `CreatedAt`
* `UpdatedAt`
* `IsDeleted`
* `DeletedAt`

Constraints and indexes:

* Primary key: `pk_category`
* Foreign key: `fk_category_cafe`
* Tenant query index: `idx_category_cafe`
* Tenant ordering index: `idx_category_cafe_display_order`

Notes:

* Categories are not hard-coded.
* Public queries must filter by cafe, visibility, published state and soft-delete state.

### ProductEntity

Table: `product`

Purpose: Cafe-owned menu product.

Important fields:

* `Id`
* `CafeId`
* `CategoryId`
* `Name`
* `Description`
* `Price`
* `ImageUrl`
* `IsAvailable`
* `IsVisible`
* `IsPublished`
* `DisplayOrder`
* `CreatedAt`
* `UpdatedAt`
* `IsDeleted`
* `DeletedAt`

Constraints and indexes:

* Primary key: `pk_product`
* Foreign keys:
  * `fk_product_cafe`
  * `fk_product_category`
* Tenant query index: `idx_product_cafe`
* Tenant/category query index: `idx_product_cafe_category`
* Tenant ordering index: `idx_product_cafe_category_display_order`

Notes:

* `Price` must be fixed precision decimal. Database precision should be configured explicitly.
* Unavailable products may appear publicly only when visible, published and non-deleted, with a clear unavailable state.
* Products must never be returned across tenant boundaries.

### RefreshTokenEntity

Table: `refresh_token`

Purpose: Revocable refresh token persistence.

Important fields:

* `Id`
* `AppUserId`
* `TokenHash`
* `ExpiresAt`
* `RevokedAt`
* `ReplacedByTokenHash`
* `CreatedAt`
* `UpdatedAt`

Constraints and indexes:

* Primary key: `pk_refresh_token`
* Foreign key: `fk_refresh_token_app_user`
* User token lookup index: `idx_refresh_token_app_user`

Notes:

* Store token hashes where practical; never log token values.

### AuditLogEntity

Table: `audit_log`

Purpose: Meaningful administration event tracking.

Important fields:

* `Id`
* `CafeId`
* `AppUserId`
* `Action`
* `EntityName`
* `EntityId`
* `Summary`
* `CreatedAt`

Constraints and indexes:

* Primary key: `pk_audit_log`
* Foreign keys:
  * `fk_audit_log_cafe`
  * `fk_audit_log_app_user`
* Tenant audit lookup index: `idx_audit_log_cafe_created_at`
* User audit lookup index: `idx_audit_log_app_user_created_at`

Notes:

* Never store secrets, passwords or tokens in audit records.

## Relationships

```text
AppUserEntity
  -> CafeMembershipEntity
      -> CafeEntity
          -> CafeThemeEntity
          -> CategoryEntity
              -> ProductEntity
          -> ProductEntity
          -> AuditLogEntity

AppUserEntity
  -> RefreshTokenEntity

RoleEntity
  -> CafeMembershipEntity
```

`ProductEntity` carries both `CafeId` and `CategoryId`. The service layer must ensure the category belongs to the same cafe as the product.

## Cafe Ownership

Cafe ownership is represented through `CafeMembershipEntity` with the `CAFE_OWNER` role. Do not store a single owner ID on `CafeEntity` as the only ownership mechanism.

This supports:

* Multiple owners/managers per cafe
* One user managing multiple cafes
* Future permission expansion

## Soft-Delete Strategy

Soft-deletable entities include:

* `AppUserEntity`
* `CafeEntity`
* `CafeMembershipEntity`
* `CategoryEntity`
* `ProductEntity`

Soft-delete fields:

* `IsDeleted`
* `DeletedAt`

Normal administration queries and public menu queries must exclude soft-deleted rows unless a future recovery feature intentionally requests them.

## Public Filtering Rules

Public menu queries must require:

* Cafe is active
* Cafe is published
* Cafe is not deleted
* Category is visible
* Category is published
* Category is not deleted
* Product is visible
* Product is published
* Product is not deleted

Unavailable products may be included when they satisfy public visibility rules.

## Tenant-Scoped Query Rules

Administration queries for cafe-owned resources must be scoped by server-side cafe access checks.

Required checks:

* The authenticated user exists and is active.
* The user has active membership for the target cafe.
* The membership role or policy permits the requested action.
* The requested cafe-owned resource belongs to the same cafe.

Never trust a client-supplied `CafeId` alone.

