# UI Branding

## Application UI Model

Use Blazor Web App for the web interface unless a future repository decision explicitly changes the UI technology.

The public menu experience and authenticated administration experience must remain clearly separated in routing and UI structure.

Suggested route groups:

```text
/c/{slug}          Public QR menu
/admin             Authenticated cafe administration
/platform          Platform administration
/account           Account/authentication pages
```

API routes must still follow `docs/API_CONVENTIONS.md` and use action-based endpoint names.

## Public Menu Structure

The public menu is mobile-first and optimized for QR entry.

Core sections:

* Cover area
* Cafe logo and cafe name
* Welcome title
* Optional welcome description
* Category navigation
* Product search
* Product list grouped or filtered by category
* Product detail display
* Unavailable product state

Only active, published, visible and non-deleted content may appear publicly.

## Mobile-First Requirements

The public menu should:

* Load quickly on mobile networks
* Avoid unnecessary large assets
* Use responsive images where practical
* Keep navigation reachable with one hand
* Make category switching fast
* Make prices and availability clear
* Avoid requiring authentication or account creation

## Configurable Branding Values

Cafe administrators can configure:

* Cafe name
* Logo image
* Cover image
* Primary color
* Secondary color
* Accent color
* Background color
* Text color
* Welcome title
* Welcome description
* Font preset
* Theme/layout preset

Do not allow arbitrary HTML, CSS or JavaScript.

## Validation Rules

Branding values must be validated before persistence.

Recommended validation:

* Color values must match an approved hex color format.
* Font preset must be one of the supported values.
* Theme preset must be one of the supported values.
* Welcome title and description must respect length limits.
* Image references must come from the approved file storage flow.

## Theme Presets

Initial controlled presets:

* `CLASSIC`
* `MODERN`
* `COMPACT`

These are stable identifiers, not user-provided CSS.

## Font Presets

Initial controlled presets:

* `SYSTEM`
* `SANS`
* `SERIF`

These are stable identifiers mapped by application code to safe font stacks.

## CSS Variable Strategy

The public UI may use CSS variables populated from validated theme settings.

Example variable names:

```text
--cafe-primary-color
--cafe-secondary-color
--cafe-accent-color
--cafe-background-color
--cafe-text-color
```

Only validated values from `CafeThemeEntity` should populate these variables.

## Administration Areas

Initial cafe admin areas:

* Dashboard
* Menu Management
* Categories
* Products
* Appearance
* Branding
* Theme
* Preview
* QR Code
* Cafe Settings
* Account

Dashboard Version 1 includes:

* Total product count
* Total category count
* Available product count
* Unavailable product count

Advanced analytics are out of scope.

## Preview Behavior

Cafe administrators should be able to preview public branding changes where practical before publishing.

Preview behavior must:

* Use the same controlled theme settings as the public page
* Avoid executing user-provided HTML/CSS/JavaScript
* Clearly separate draft preview state from published public state when draft support is implemented

If draft publishing is not implemented in the first increment, branding updates may apply immediately after validation, and draft support can be deferred.

## Image Usage

Images are stored through file storage and referenced by URL or storage key in the database.

Do not store normal image binaries in PostgreSQL.

Image upload controls must validate file type, size, MIME type where appropriate and generated storage paths.

