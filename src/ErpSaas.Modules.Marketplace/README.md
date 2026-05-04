# Marketplace Module

Online marketplace integration for Amazon, Flipkart, Meesho, Shopify, and WooCommerce.

## Entities (schema: `marketplace`)

| Entity | Description |
|--------|-------------|
| `MarketplaceAccount` | Credentials and sync flags for each connected marketplace |
| `MarketplaceProductMapping` | Maps internal products to marketplace SKUs/listings |
| `MarketplaceOrder` | Orders fetched from marketplaces, pending invoice conversion |

## Services

| Service | Responsibility |
|---------|----------------|
| `IMarketplaceAccountService` | CRUD for marketplace accounts, test-connection |
| `IMarketplaceOrderService` | List and ingest orders, convert to invoice |
| `IMarketplaceSyncService` | Trigger inventory/price/order sync, manage product links |

## Connectors (extend `ThirdPartyApiClientBase`)

- `AmazonSpApiConnector` — SP-API OAuth; every call logged to `ThirdPartyApiLog`
- `FlipkartConnector` — Flipkart Seller API; every call logged to `ThirdPartyApiLog`

## Hangfire Jobs

| Job | Schedule | Description |
|-----|----------|-------------|
| `MarketplaceOrderPollingJob` | Every 5 min | Polls all active accounts for new orders |
| `MarketplaceInventorySyncJob` | Daily | Pushes stock changes to marketplaces |
| `MarketplacePriceSyncJob` | Daily | Pushes price changes to marketplaces |

## Endpoints (11)

```
GET    /api/marketplace/accounts
POST   /api/marketplace/accounts
PATCH  /api/marketplace/accounts/{id}
POST   /api/marketplace/accounts/{id}/test-connection
GET    /api/marketplace/products
POST   /api/marketplace/products/link
POST   /api/marketplace/sync/inventory
POST   /api/marketplace/sync/prices
POST   /api/marketplace/sync/orders
GET    /api/marketplace/orders
POST   /api/marketplace/orders/{id}/convert-to-invoice
```

## Permissions

| Code | Description |
|------|-------------|
| `Marketplace.View` | View accounts, orders, mappings |
| `Marketplace.Manage` | Add/edit accounts and product links |
| `Marketplace.Sync` | Trigger sync jobs |
| `Marketplace.ConvertOrder` | Convert marketplace orders to invoices |

## Feature Flags

- `Marketplace.Amazon` — Growth/Enterprise
- `Marketplace.Flipkart` — Growth/Enterprise
- `Marketplace.Shopify` — Growth/Enterprise
- `Marketplace.WooCommerce` — Growth/Enterprise
- `Marketplace.OwnEcommerce` — Growth/Enterprise
