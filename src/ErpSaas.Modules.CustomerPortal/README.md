# CustomerPortal Module

**Plan reference:** §6.27 Customer Self-Service Portal  
**DB Schema:** `portal` (TenantDB for OnlineOrder/CustomerInquiry; PlatformDB for auth entities via Infrastructure)  
**DB Tier:** Tenant (business entities) + Platform (auth entities — pre-existing in Infrastructure)

---

## Entities

| Entity | DB | Description |
|--------|-----|-------------|
| `OnlineOrder` | TenantDB / `portal` | Customer-placed online order with status lifecycle |
| `OnlineOrderLine` | TenantDB / `portal` | Line item with unit + conversion factor per §6.3.c |
| `CustomerInquiry` | TenantDB / `portal` | Support/query ticket from a portal customer |
| `CustomerInquiryMessage` | TenantDB / `portal` | Thread message on an inquiry |
| `PlatformCustomer` | PlatformDB / `portal` | Cross-shop customer identity (Infrastructure) |
| `CustomerLink` | PlatformDB / `portal` | Link between PlatformCustomer and a shop's TenantCustomer (Infrastructure) |
| `CustomerLoginSession` | PlatformDB / `portal` | OTP challenge and refresh-token sessions (Infrastructure) |

---

## Endpoints

### Portal Auth — `/api/portal/auth` (anonymous)

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/signup-otp` | `[AllowAnonymous] + [RequireCaptcha]` | Request OTP for signup/login |
| `POST` | `/verify-otp` | `[AllowAnonymous] + [RequireCaptcha]` | Verify OTP → issue JWT (token_scope=customer) |
| `POST` | `/refresh` | `[AllowAnonymous]` | Rotate refresh token |
| `POST` | `/logout` | `[CustomerAuth]` | Revoke refresh token |

### Portal — `/api/portal` (customer JWT required)

| Method | Path | Permission | Description |
|--------|------|-----------|-------------|
| `GET` | `/me` | `[CustomerAuth]` | Get own profile |
| `PATCH` | `/me` | `[CustomerAuth]` | Update display name / email |
| `GET` | `/me/purchases` | `[CustomerAuth]` | Cross-shop purchase history (Dapper) |
| `GET` | `/me/purchases/{id}` | `[CustomerAuth]` | Purchase detail with line items |
| `GET` | `/me/shops` | `[CustomerAuth]` | List linked shops |
| `GET` | `/me/insights` | `[CustomerAuth]` | Spend analytics across shops |
| `GET` | `/me/inquiries` | `[CustomerAuth]` | Own inquiry list |
| `POST` | `/me/inquiries` | `[CustomerAuth]` | Create inquiry |
| `POST` | `/me/inquiries/{id}/reply` | `[CustomerAuth]` | Reply to own inquiry |
| `POST` | `/me/orders` | `[CustomerAuth]` | Place online order |
| `GET` | `/me/orders` | `[CustomerAuth]` | Own order list |
| `GET` | `/me/orders/{id}` | `[CustomerAuth]` | Order detail |

### Online Orders (staff) — `/api/online-orders`

| Method | Path | Permission | Description |
|--------|------|-----------|-------------|
| `GET` | `/` | `OnlineOrder.View` | List all shop orders |
| `GET` | `/{id}` | `OnlineOrder.View` | Order detail |
| `PATCH` | `/{id}/accept` | `OnlineOrder.Manage` | Accept pending order |
| `PATCH` | `/{id}/reject` | `OnlineOrder.Manage` | Reject with reason |
| `PATCH` | `/{id}/dispatch` | `OnlineOrder.Manage` | Mark dispatched |
| `PATCH` | `/{id}/deliver` | `OnlineOrder.Manage` | Mark delivered |
| `PATCH` | `/{id}/cancel` | `OnlineOrder.Manage` | Cancel order |

### Inquiries (staff) — `/api/portal/inquiries`

| Method | Path | Permission | Description |
|--------|------|-----------|-------------|
| `GET` | `/` | `Inquiry.View` | List all shop inquiries |
| `GET` | `/{id}` | `Inquiry.View` | Inquiry detail with messages |
| `POST` | `/{id}/reply` | `Inquiry.Manage` | Staff reply |
| `POST` | `/{id}/close` | `Inquiry.Manage` | Close inquiry |
| `PATCH` | `/{id}/assign` | `Inquiry.Manage` | Assign to staff user |

---

## DDL Catalog Keys Used

- `ONLINE_ORDER_STATUS` — Pending, Accepted, Rejected, Dispatched, Delivered, Cancelled, Refunded
- `INQUIRY_TYPE` — ProductAvailability, PriceQuery, Complaint, FeatureRequest
- `INQUIRY_STATUS` — Open, InProgress, Resolved, Closed

---

## Permissions

| Code | Description |
|------|-------------|
| `OnlineOrder.View` | View online orders in staff app |
| `OnlineOrder.Manage` | Accept/reject/dispatch/deliver/cancel orders |
| `Inquiry.View` | View customer inquiries in staff app |
| `Inquiry.Manage` | Reply/close/assign inquiries |
| `Portal.Config` | Configure portal settings |

---

## Features (Subscription Plan Flags)

| Code | Plans |
|------|-------|
| `customer.portal` | Growth, Enterprise |
| `customer.online_orders` | Enterprise |
| `customer.smart_shopping` | Enterprise |

---

## Sequences

| Code | Prefix | Description |
|------|--------|-------------|
| `ONLINE_ORDER` | `ORD` | Online order number |
| `CUSTOMER_INQUIRY` | `INQ` | Customer inquiry number |

---

## Integrations

- None — portal auth is internal OTP; SMS delivery via `INotificationService` (NotificationsDB, deferred)

---

## How to Run Module Tests

```bash
# Unit tests (8 tests)
dotnet test src/ErpSaas.Tests.Unit --filter "FullyQualifiedName~CustomerPortal"

# Arch tests (5 rules)
dotnet test src/ErpSaas.Tests.Arch --filter "FullyQualifiedName~CustomerPortal"

# Integration tests (all skipped — Testcontainers gate pending)
dotnet test src/ErpSaas.Tests.Integration --filter "FullyQualifiedName~CustomerPortal"
```
