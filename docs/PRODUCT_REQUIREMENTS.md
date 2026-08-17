# Product Requirements

## Product Vision

The product is a production-oriented multi-tenant QR menu SaaS platform for cafes and restaurants in Turkey.

The application is not designed for a single cafe. A cafe is a tenant, and multiple independent cafes must use the same application while administration data remains isolated per cafe.

Cafe administrators must be able to configure menu and branding data without source-code changes.

Version 1 is Turkey-focused. Product prices use Turkish Lira only. Do not introduce configurable currencies, currency conversion, USD/EUR support, exchange-rate infrastructure or multi-currency abstractions in Version 1. Monetary values must use fixed-precision decimal types.

## Actors

### PLATFORM_ADMIN

The platform administrator manages the SaaS platform.

Initial capabilities:

* Create cafes
* View cafes
* Activate or deactivate cafes
* Create or assign cafe owners
* Inspect basic cafe information

Subscription billing is out of scope for Version 1.

### CAFE_OWNER

The cafe owner can fully administer cafes where the account has ownership or membership.

The data model must support a user managing more than one cafe. Never assume one user equals one cafe.

### CAFE_MANAGER

Cafe managers can manage permitted menu-related resources for cafes where they are assigned.

Authorization must allow additional permissions to be introduced later without redesigning the membership model.

### Anonymous Public Customer

Public customers browse QR menus without authentication. Customer accounts, customer login and customer registration are out of scope for Version 1.

## Version 1 Scope

Version 1 includes:

* Authentication for administration users
* Platform administration for cafes and cafe owners
* Cafe membership and tenant authorization
* Cafe branding and controlled appearance settings
* Category management
* Product management
* Public QR menu by cafe slug
* QR code generation for the cafe public menu URL
* Local-development image storage through a replaceable storage abstraction
* Basic cafe dashboard statistics
* Docker-based PostgreSQL development environment
* Tests for security-critical behavior, especially tenant isolation

## Functional Requirements

### Cafe Management

Each cafe must support:

* Name
* Globally unique URL-safe slug
* Logo image URL/reference
* Cover image URL/reference
* Active/inactive state
* Audit fields
* Soft-delete fields where applicable

Public cafe resolution uses the globally unique slug.

### Branding and Appearance

Each cafe must be able to configure:

* Cafe name
* Logo
* Cover image
* Primary color
* Secondary color
* Accent color
* Background color
* Text color
* Welcome title
* Welcome description
* Supported font preset
* Supported theme/layout preset

Cafe administrators must not upload or execute arbitrary HTML, CSS or JavaScript. Theme settings must be validated and controlled.

### Category Management

Categories are data-driven. Do not hard-code categories such as coffee, dessert or food in application code.

Cafe administrators must be able to:

* Create categories
* Update categories
* Soft-delete categories
* Hide or show categories
* Reorder categories
* Optionally upload or change category images

Category order is controlled by a display-order field.

### Product Management

Cafe administrators must be able to:

* Create products
* Update products
* Soft-delete products
* Upload or change product images
* Change product names
* Change product descriptions
* Change prices
* Assign products to categories
* Reorder products
* Hide or show products
* Mark products available or unavailable

A product must not be deleted only because it is temporarily unavailable.

### Public QR Menu

The public menu URL format is:

```text
/c/{slug}
```

The page must:

* Require no authentication
* Be mobile-first and responsive
* Load quickly
* Reflect the cafe branding
* Show only active, published, visible and non-deleted public content
* Allow product search
* Support product detail display
* Display unavailable products with a clear unavailable/sold-out state

The public menu should show:

* Cafe logo
* Cafe name
* Optional cover image
* Welcome title
* Optional welcome description
* Category navigation
* Product images
* Product names
* Product descriptions
* Prices in Turkish Lira
* Availability state

### QR Management

Each cafe has one general menu QR code in Version 1.

The QR management area should generate a QR code for the public menu URL and support PNG and SVG export where practical.

Table-specific QR codes are out of scope for Version 1, but the design must not make future table-specific QR ordering difficult.

### File and Image Storage

Do not store normal product or cafe image binaries in PostgreSQL.

Store references or URLs in the database and introduce an abstraction such as `IFileStorageService`.

For local development, a local filesystem implementation may be used. Business logic must not depend directly on one storage provider.

Uploads must validate:

* Permitted file type
* MIME type where appropriate
* Maximum file size
* Generated storage filename
* Unsafe paths
* Malformed files where reasonably possible

Never trust or directly use the original uploaded filename as a server storage path.

### Dashboard

The first cafe dashboard includes basic statistics:

* Total product count
* Total category count
* Available product count
* Unavailable product count

Advanced analytics are out of scope for Version 1.

## API Capability Areas

Follow `docs/API_CONVENTIONS.md`; use action-based endpoints and `ApiResponse<T>`.

Required API groups:

* Authentication
* Platform Administration
* Cafe Administration
* Cafe Branding
* Category Management
* Product Management
* Public Menu
* QR Management
* File/Image Upload

Entities must never be returned directly from controllers.

## Non-Functional Requirements

* Tenant isolation is a critical security requirement.
* Server-side authorization is mandatory.
* Do not trust client-supplied `CafeId` as proof of access.
* Use PostgreSQL through Entity Framework Core.
* All schema changes require EF Core migrations.
* Use Docker Compose for local PostgreSQL development.
* Use structured logging with `ILogger<T>`.
* Never log passwords, password hashes, access tokens, refresh tokens or secrets.
* Public menu pages should be fast and mobile-first.
* Administration and public menu UI routes must remain clearly separated.

## Explicitly Excluded Features

Do not implement these in Version 1:

* Customer accounts
* Customer login
* Customer registration
* Table ordering
* Normal ordering
* Payments
* Kitchen display system
* Reservations
* Loyalty points
* Delivery
* Advanced inventory
* Subscription billing
* Payment plans
* Advanced analytics
* Real-time order communication
* Microservices
* Arbitrary drag-and-drop page builders
* Custom user-provided JavaScript
* Multi-currency support

Do not create unused abstractions for these features merely because they may be added later.

## Future Features

A future ordering version may introduce:

```text
Cafe
  -> Table
  -> TableSession
  -> Order
  -> OrderItem
```

A table-specific QR code may identify both the cafe and table. Customers may eventually place orders from phones without customer accounts.

These future concepts should influence only important irreversible decisions. Do not implement ordering infrastructure in Version 1.

