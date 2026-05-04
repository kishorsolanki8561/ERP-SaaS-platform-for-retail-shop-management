# ApiAccess Module

Shop API key management and outbound webhook dispatch for third-party integrations (§6.20).

**DB schema:** `integration` | **DB tier:** TenantDB

## Entities

| Entity | Description |
|--------|-------------|
| `ShopApiKey` | Hashed API key with scope, expiry, rate-limit, last-used tracking |
| `WebhookEndpoint` | Registered HTTPS endpoint with HMAC signing secret and event subscription |
| `WebhookDelivery` | Per-event delivery attempt with status, HTTP response, and retry counter |

## Services

| Service | Responsibility |
|---------|----------------|
| `IShopApiKeyService` | Create, list, revoke API keys; generate raw key + SHA-256 hash |
| `IWebhookDispatchService` | Register/update endpoints, dispatch events, retry delivery, test endpoint |
| `WebhookSignatureGenerator` | Pure HMAC-SHA256 signature utility |

## Hangfire Jobs

| Job | Trigger | Description |
|-----|---------|-------------|
| `WebhookDeliveryJob` | Enqueued on dispatch | HTTP POST to endpoint with exponential backoff (1 s → 5 min); dead-letters after 5 attempts |

## Endpoints (12)

```
GET    /api/shop-api-keys                             List active API keys
POST   /api/shop-api-keys                             Create key (returns raw key once)
DELETE /api/shop-api-keys/{id}                        Revoke key

GET    /api/webhooks/events                           Event catalog
POST   /api/webhooks/endpoints                        Register endpoint
GET    /api/webhooks/endpoints                        List endpoints
PATCH  /api/webhooks/endpoints/{id}                   Update endpoint
POST   /api/webhooks/endpoints/{id}/rotate-secret     Rotate signing secret
POST   /api/webhooks/endpoints/{id}/test              Fire test event
GET    /api/webhooks/endpoints/{id}/deliveries        List delivery log
POST   /api/webhooks/deliveries/{deliveryId}/retry    Retry dead-lettered delivery
```

## Permissions

| Code | Description |
|------|-------------|
| `Integration.ManageApiKeys` | Create and revoke API keys |
| `Integration.ManageWebhooks` | Register and configure webhook endpoints |
| `Integration.ViewDeliveries` | View webhook delivery log |

## Feature Flags

| Code | Plans |
|------|-------|
| `integration.api_access` | Growth, Enterprise |
| `integration.webhooks` | Growth, Enterprise |

## Webhook Signature

Each delivery includes `X-ShopSphere-Signature: sha256=<hex>` computed with HMAC-SHA256 over the raw JSON body using the endpoint's `SigningSecret`.

## Running Tests

```bash
# Unit + arch tests only (no containers)
dotnet test src/ErpSaas.sln --filter "FullyQualifiedName~ApiAccess"

# Unit only
dotnet test src/ErpSaas.Tests.Unit --filter "FullyQualifiedName~ApiAccess"

# Arch only
dotnet test src/ErpSaas.Tests.Arch --filter "FullyQualifiedName~ApiAccess"
```
