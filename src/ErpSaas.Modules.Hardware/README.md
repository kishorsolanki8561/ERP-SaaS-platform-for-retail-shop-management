# Hardware Integration Module (§7.7)

Provides server-side support for peripheral devices: device registration, label template management (ZPL/TSPL), receipt template management (ESC/POS), and print rendering endpoints.

## Entities (TenantDB — `hardware` schema)

| Entity | Purpose |
|---|---|
| `DeviceProfile` | Registry of physical devices per shop (printer, cash drawer, scanner, scale) |
| `LabelTemplate` | ZPL/TSPL label layout with `{{placeholder}}` substitution |
| `ReceiptTemplate` | ESC/POS receipt configuration (header/footer JSON) |

## Services

| Service | Responsibility |
|---|---|
| `IDeviceProfileService` | CRUD for device profiles |
| `ILabelTemplateService` | CRUD + ZPL/TSPL rendering via `ZplRenderer` |
| `IReceiptTemplateService` | CRUD + ESC/POS byte stream rendering via `EscPosRenderer` |

## Endpoints

| Method | Route | Permission | Feature |
|---|---|---|---|
| GET | `/api/device-profiles` | `Device.Configure` | — |
| POST | `/api/device-profiles` | `Device.Configure` | — |
| PATCH | `/api/device-profiles/{id}` | `Device.Configure` | — |
| DELETE | `/api/device-profiles/{id}` | `Device.Configure` | — |
| GET | `/api/label-templates` | `Template.Label.Manage` | — |
| POST | `/api/label-templates` | `Template.Label.Manage` | — |
| PATCH | `/api/label-templates/{id}` | `Template.Label.Manage` | — |
| DELETE | `/api/label-templates/{id}` | `Template.Label.Manage` | — |
| GET | `/api/receipt-templates` | `Template.Receipt.Manage` | — |
| POST | `/api/receipt-templates` | `Template.Receipt.Manage` | — |
| PATCH | `/api/receipt-templates/{id}` | `Template.Receipt.Manage` | — |
| DELETE | `/api/receipt-templates/{id}` | `Template.Receipt.Manage` | — |
| POST | `/api/receipt-templates/{id}/preview` | `Template.Receipt.Manage` | — |
| POST | `/api/print/label` | `Template.Label.Manage` | `hardware.label_printer` |
| POST | `/api/print/receipt` | `Template.Receipt.Manage` | `hardware.thermal_receipt` |

## ZPL Label Rendering

`LabelTemplateService.RenderAsync` substitutes `{{productName}}`, `{{barcode}}`, `{{price}}`, `{{date}}`, `{{productId}}` in the stored template. If no `LabelTemplateId` is provided, the shop's default `ProductTag` template is used, or a built-in fallback is applied.

## ESC/POS Receipt Rendering

`ReceiptTemplateService.RenderAsync` builds a standard 80mm ESC/POS byte stream and returns it base64-encoded. The Angular client (Electron/Capacitor) sends the raw bytes to the thermal printer.

## Feature Flags

- `hardware.label_printer` — Enabled on Growth and Enterprise plans
- `hardware.thermal_receipt` — Enabled on Growth and Enterprise plans
