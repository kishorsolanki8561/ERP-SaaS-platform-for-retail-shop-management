# ErpSaas.Modules.Inventory

## Schema

All entities mapped to SQL schema **`inventory`** inside `TenantDB`.

## Entities

| Entity | Table | Purpose |
|---|---|---|
| `Product` | `inventory.Product` | Product master (code, HSN, GST rate, unit, pricing) |
| `ProductUnit` | `inventory.ProductUnit` | Units per product (PCS, BOX, DOZEN). ConversionFactor relative to base unit |
| `Warehouse` | `inventory.Warehouse` | Physical/logical storage location per shop |
| `StockMovement` | `inventory.StockMovement` | Immutable ledger row for every stock in/out event |

## Permissions

| Code | Purpose |
|---|---|
| `Inventory.View` | Read products, warehouses, stock levels |
| `Inventory.Manage` | Create/update products, warehouses, stock adjustments |

## DDL Catalogs

| Key | Items |
|---|---|
| `PRODUCT_CATEGORY` | Electrical, Electronics, PowerTools, Hardware, Accessories, Cables, Batteries, Lighting, Safety |

## Endpoints

| Method | Route | Permission |
|---|---|---|
| GET | `/api/inventory/products` | `Inventory.View` |
| GET | `/api/inventory/products/{id}` | `Inventory.View` |
| POST | `/api/inventory/products` | `Inventory.Manage` |
| PUT | `/api/inventory/products/{id}` | `Inventory.Manage` |
| DELETE | `/api/inventory/products/{id}` | `Inventory.Manage` |
| GET | `/api/inventory/warehouses` | `Inventory.View` |
| POST | `/api/inventory/warehouses` | `Inventory.Manage` |
| GET | `/api/inventory/stock/{productId}/{warehouseId}` | `Inventory.View` |
| POST | `/api/inventory/stock/adjust` | `Inventory.Manage` |

## Key rules

- Every stock movement stores `ProductUnitId`, `UnitCodeSnapshot`, `ConversionFactorSnapshot`, `QuantityInBilledUnit`, and `QuantityInBaseUnit` (CLAUDE.md §3.7).
- All DB writes go through `BaseService.ExecuteAsync` — never bare `SaveChangesAsync`.
- Category dropdown uses DDL catalog `PRODUCT_CATEGORY`; never hardcoded.

## DI registration

```csharp
services.AddInventoryModule();
```
