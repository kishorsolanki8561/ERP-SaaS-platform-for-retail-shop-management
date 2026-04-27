# ErpSaas.Modules.Identity

Handles authentication, authorisation, shop management, branch management, role management,
and user administration for the ERP platform.

## Domain model

| Entity | Table | Schema / DB |
|---|---|---|
| `Shop` | `Shop` | `identity` / PlatformDB |
| `User` | `User` | `identity` / PlatformDB |
| `UserShop` | `UserShop` | `identity` / PlatformDB |
| `UserSecurityToken` | `UserSecurityToken` | `identity` / PlatformDB |
| `Role` | `Role` | `identity` / PlatformDB |
| `RolePermission` | `RolePermission` | `identity` / PlatformDB |
| `Branch` | `Branch` | `identity` / PlatformDB |
| `SubscriptionPlan` | `SubscriptionPlan` | `identity` / PlatformDB |
| `ShopSubscription` | `ShopSubscription` | `identity` / PlatformDB |

## Endpoints

| Method | Route | Permission |
|---|---|---|
| `POST` | `/api/auth/login` | Public (CAPTCHA-gated) |
| `POST` | `/api/auth/refresh` | Public |
| `POST` | `/api/auth/logout` | Authenticated |
| `POST` | `/api/auth/forgot-password` | Public (CAPTCHA-gated) |
| `POST` | `/api/auth/reset-password` | Public (CAPTCHA-gated) |
| `GET` | `/api/admin/users` | `User.View` |
| `POST` | `/api/admin/users/invite` | `User.Invite` |
| `PUT` | `/api/admin/users/{id}/roles/{roleId}` | `User.ManageRoles` |
| `GET` | `/api/admin/roles` | `Role.View` |
| `POST` | `/api/admin/roles` | `Role.Create` |
| `PUT` | `/api/admin/roles/{id}/permissions` | `Role.ManagePermissions` |
| `GET` | `/api/admin/branches` | `ShopProfile.View` |
| `POST` | `/api/admin/branches` | `ShopProfile.Edit` |
| `PUT` | `/api/admin/branches/{id}` | `ShopProfile.Edit` |
| `DELETE` | `/api/admin/branches/{id}` | `ShopProfile.Edit` |
| `POST` | `/api/bootstrap/register-product-owner` | One-time only (no owner exists) |
| `GET` | `/api/bootstrap/status` | Public |
| `GET` | `/api/menu/tree` | Authenticated |

## Permissions

| Code | Purpose |
|---|---|
| `User.View` | List users |
| `User.Invite` | Invite new users |
| `User.Deactivate` | Deactivate a user |
| `User.ManageRoles` | Assign / remove roles |
| `Role.View` | List roles |
| `Role.Create` | Create custom roles |
| `Role.ManagePermissions` | Assign permissions to a role |
| `ShopProfile.View` | View shop + branch settings |
| `ShopProfile.Edit` | Edit shop profile and branches |

## JWT tokens

- **Staff token**: 15-min access + 30-day rotating refresh. Carries `shopId`, `userId`, `branchId`, `permissions[]`.
- **Customer portal token**: carries `token_scope=customer`; rejected by every staff API endpoint.

## Branch selector

The Angular `BranchStore` (signal store) loads all active branches via `GET /api/admin/branches`.
The active branch is persisted in `localStorage` under key `active-branch-id`.
Every API request carries the `X-Branch-Id` header set by `tenantInterceptor`.

## Wiring

1. Call `services.AddIdentityModule()` in `Program.cs` after `AddInfrastructure()`.
2. `IdentityDataSeeder` + `MenuDataSeeder` are registered automatically via `AddDataSeeder<T>`.
3. `ShopOnboardingService` is called by `BootstrapController` and creates the initial shop,
   admin user, trial subscription, and tenant seeds in one `ExecuteAsync` transaction.

## TODOs (deferred to subsequent phases)

- TOTP 2FA enforcement per-shop policy (Phase 5).
- SSO / OAuth2 provider login (Phase 5).
- Customer portal `PlatformCustomer` cross-link to staff `Customer` entity (Phase 1 CRM done; link in Phase 3).
