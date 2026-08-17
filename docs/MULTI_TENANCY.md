# Multi-Tenancy

## Tenant Definition

A cafe is the tenant.

Cafe-owned administration data includes:

* Cafe branding
* Categories
* Products
* Product images/references
* Category images/references
* QR menu settings
* Cafe audit records

Tenant isolation is a critical security requirement. A user authorized for Cafe A must never read, update, delete or manipulate private Cafe B resources.

## Membership Model

Tenant access is modeled through `CafeMembershipEntity`.

```text
AppUserEntity
  -> CafeMembershipEntity
      -> CafeEntity
      -> RoleEntity
```

This avoids the incorrect assumption that one user belongs to exactly one cafe.

Initial roles:

* `PLATFORM_ADMIN`
* `CAFE_OWNER`
* `CAFE_MANAGER`

`PLATFORM_ADMIN` is a platform-level role and may manage cafes according to platform policies. `CAFE_OWNER` and `CAFE_MANAGER` are cafe-scoped roles through cafe membership.

## Authenticated Tenant Authorization

Administration requests must use authenticated identity and server-side authorization logic.

The server must determine cafe access from:

* Authenticated user ID from the token/claims
* Active user state
* Active cafe membership
* Role/policy requirements
* Resource ownership checks

Client-supplied `CafeId` is only an identifier for the requested operation. It is not proof of authorization.

## Query Scoping

Repository methods may query by cafe IDs, but services are responsible for business authorization before returning or mutating private cafe data.

For every cafe-owned resource command:

1. Resolve authenticated user.
2. Verify required membership or platform role.
3. Load the target resource scoped by `CafeId`.
4. Verify the resource belongs to the same cafe.
5. Perform the operation.

For list/read operations, queries must include tenant scope. Do not fetch unscoped data and filter only in memory.

## Cross-Tenant Protection

The implementation must protect against:

* Reading Cafe B categories/products with Cafe A credentials
* Updating Cafe B resources by manually submitting Cafe B identifiers
* Soft-deleting Cafe B resources by manually submitting Cafe B identifiers
* Assigning a Cafe A product to a Cafe B category
* Viewing Cafe B audit logs from Cafe A membership

Service-layer checks must return standard `ApiResponse<T>` errors through the global API conventions and exception handling strategy.

## Public Cafe Resolution

Public menu browsing is anonymous.

Public resolution uses:

```text
/c/{slug}
```

The slug must be globally unique and URL-safe.

Public queries do not use cafe membership. They must still filter to active, published, visible and non-deleted public content.

Public menu resolution must not expose private administration data.

## Authorization Policies

Initial policies should distinguish:

* Platform administration
* Cafe ownership
* Cafe menu management
* Cafe branding management
* Cafe QR management

Policies should be implemented on the backend using ASP.NET Core authorization. The Blazor UI may hide inaccessible actions, but UI hiding is not security.

## Tenant Isolation Testing Strategy

Tenant isolation tests are mandatory.

At minimum include tests proving:

* A user authorized for Cafe A cannot read Cafe B private administration data.
* A user authorized for Cafe A cannot update Cafe B resources by submitting Cafe B identifiers.
* A user authorized for Cafe A cannot soft-delete Cafe B resources by submitting Cafe B identifiers.
* A Cafe A product cannot be assigned to a Cafe B category.
* Public menu queries expose only active, published, visible and non-deleted content.

Recommended test setup:

* Create User A, Cafe A and Cafe A membership.
* Create User B, Cafe B and Cafe B membership.
* Create categories/products for both cafes.
* Authenticate as User A.
* Attempt reads and mutations against Cafe B resources.
* Assert forbidden/not-found behavior according to the chosen service/API error convention.

Tenant isolation tests should run as integration tests for critical flows because repository, service and authorization behavior must work together.

## Audit Events

Record meaningful administration events where useful:

* Cafe created
* Cafe activated/deactivated
* Cafe branding changed
* Category created
* Category deleted
* Product created
* Product deleted
* Product price changed
* Role/membership changed

Audit records must not include secrets, passwords, token values or sensitive credentials.

