---
name: erp-write-service
description: Write a new service method in the ERP + SaaS platform following the mandatory BaseService.ExecuteAsync pattern. Triggers whenever the user asks to "add a method", "implement X service", "write a service method", creates any new C# method in any `*Service.cs` file under `src/ErpSaas.Modules.*`, or edits an existing service method. Also triggers whenever code calls `_db.SaveChangesAsync()` directly or when the user is about to write business logic that mutates data. Using this skill keeps every write path wrapped in try/catch + transaction + CancellationToken + error logging + audit — skipping it produces arch-test failures and inconsistent error handling across 28+ modules.
---

# Write an ERP Service Method

The `BaseService.ExecuteAsync` pattern (plan §5.1) is non-negotiable. Every service method must flow through it or arch tests fail.

## The pattern (memorize this)

```csharp
public Task<Result<TReturn>> {MethodName}Async({params}, CancellationToken ct) =>
    _baseService.ExecuteAsync("{Module}.{Method}", async () =>
    {
        // 1. Validate business rules (FluentValidation already ran in pipeline)
        // 2. Load referenced entities — lock rows if concurrent writes are possible
        // 3. Mutate / insert / delete — snapshot columns filled in here
        // 4. Call dependent services (sequence, pricing, notification) — NEVER other DbContexts
        // 5. await _db.SaveChangesAsync(ct);
        // 6. Return Result<TReturn>.Success(...) or Result.Failure(...)
    }, ct, useTransaction: {true for writes, false for reads});
```

## The six things `ExecuteAsync` handles for you

Do not reimplement any of these — it already does them:

1. **try/catch** — every `OperationCanceledException`, `DbUpdateConcurrencyException`, `ValidationException`, and generic `Exception` is caught and mapped to the right `Result` shape.
2. **Transaction** — opens `BeginTransactionAsync` if `useTransaction: true`, commits on Success, rolls back on Failure.
3. **CancellationToken propagation** — the `ct` must be passed into every awaitable call you make inside. If you don't, the method becomes un-cancellable.
4. **Error logging** — thrown exceptions auto-log to `ErrorLog` with operation name, correlation id, stack trace, request path, severity.
5. **Audit writes** — entity mutations are captured by the `AuditSaveChangesInterceptor` automatically. Only call `_audit.LogAsync(...)` explicitly for semantic events (login, role change, data export, permission grant) — not for every entity mutation.
6. **Result wrapping** — returns `Result<T>` with Success / Failure / Validation / Conflict / NotFound variants. Controllers call `.ToActionResult()` to produce the right HTTP status.

## Choose `useTransaction` correctly

- `useTransaction: true` — **any write path** (Create, Update, Delete, Finalize, Cancel, Approve, Process), any multi-step write that must be atomic (invoice + lines + stock movement + wallet debit — all in one).
- `useTransaction: false` — **pure reads** (Get, List, Search, Stats). Opening a transaction for reads costs performance and holds locks unnecessarily.

## The decision matrix you'll run every time

Before writing a single line of a new method, answer these:

| Question | If yes, do this |
|---|---|
| Does this method write to the DB? | `useTransaction: true` |
| Does this method call an external API? | Route through `ThirdPartyApiClientBase` — never raw `HttpClient` |
| Does this method allocate a document number (invoice, PO, voucher, etc.)? | `await _sequence.NextAsync("{CODE}", targetEntityType: "{Entity}", targetEntityId: x.Id, ct: ct)` |
| Does this method record a quantity? | Snapshot `ProductUnitId`, `UnitCodeSnapshot`, `ConversionFactorSnapshot`, `QuantityInBaseUnit` — §6.3.c |
| Does this method change a state (InvoiceStatus, PaymentStatus)? | Audit it. If the entity has `[Auditable]`, interceptor handles it; otherwise `await _audit.LogAsync(...)` |
| Does this method need a semantic event audited? (login, data export, permission change) | `await _audit.LogAsync(...)` explicitly |
| Does this method hit a hot path? (POS checkout, barcode scan, dashboard) | Consider Dapper for the specific query — but the outer `ExecuteAsync` stays EF-first |
| Does this method need a report-shaped result? (pivots, aggregates, joins) | Dapper via `_dapper.Tenant()` inside the `ExecuteAsync` block (§5.15) |
| Does this method run on a background job? | Same pattern — inject `IBaseService` into the job class and wrap the same way; explicitly pass `shopId` from `TenantScope` since there's no HTTP context |
| Does this method cross tenants? (platform admin impersonation, cross-shop customer portal) | It shouldn't. If it must, inject the specific cross-tenant service from Platform and document why in a code comment |

## Worked examples

### Example 1: Simple create with sequence + audit

```csharp
public Task<Result<long>> CreateAsync(CustomerDto dto, CancellationToken ct) =>
    _baseService.ExecuteAsync("Customer.Create", async () =>
    {
        // Uniqueness check (in addition to DB unique index — gives a friendly error)
        var exists = await _db.Customers
            .AnyAsync(x => x.Phone == dto.Phone, ct);
        if (exists) return Result<long>.Conflict($"Customer with phone {dto.Phone} already exists");

        var customer = new Customer
        {
            Name = dto.Name,
            CustomerTypeCode = dto.CustomerTypeCode,
            Phone = dto.Phone,
            CreditLimit = dto.CreditLimit,
            IsActive = true
        };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);

        // [Auditable] on Customer handles the audit row automatically.
        return Result<long>.Success(customer.Id);
    }, ct, useTransaction: true);
```

### Example 2: Multi-step write with sequence + stock + accounting

```csharp
public Task<Result<Invoice>> FinalizeAsync(long invoiceId, CancellationToken ct) =>
    _baseService.ExecuteAsync("Billing.FinalizeInvoice", async () =>
    {
        // 1. Lock the invoice row
        var invoice = await _db.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice is null) return Result<Invoice>.NotFound($"Invoice {invoiceId} not found");
        if (invoice.StatusCode == "FINALIZED") return Result<Invoice>.Conflict("Already finalized");

        // 2. Allocate invoice number from the shared sequence service (§5.16)
        var seq = await _sequence.NextAsync(
            code: invoice.Type == InvoiceType.Retail ? "INVOICE_RETAIL" : "INVOICE_WHOLESALE",
            targetEntityType: nameof(Invoice),
            targetEntityId: invoice.Id,
            ct: ct);
        invoice.InvoiceNumber = seq.RenderedText;

        // 3. Decrement stock — go through inventory service, not direct DbSet<Product> access
        foreach (var line in invoice.Lines)
            await _inventory.DecrementAsync(line.ProductId, line.QuantityInBaseUnit, ct);

        // 4. Post accounting voucher — route through accounting service
        await _accounting.PostSaleVoucherAsync(invoice, ct);

        // 5. Flip status + save
        invoice.StatusCode = "FINALIZED";
        await _db.SaveChangesAsync(ct);

        // 6. Queue notification — fire-and-forget; don't block checkout
        _ = _notifications.EnqueueAsync("INVOICE_CREATED", invoice.CustomerId, invoice.Id, ct);

        return Result<Invoice>.Success(invoice);
    }, ct, useTransaction: true);
```

### Example 3: Read-only with Dapper

```csharp
public Task<Result<DailySalesRow[]>> GetDailySalesAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
    _baseService.ExecuteAsync("Billing.GetDailySales", async () =>
    {
        const string sql = @"
            SELECT CAST(i.InvoiceDate AS DATE) AS [Date],
                   COUNT(*)                    AS InvoiceCount,
                   SUM(i.GrandTotal)           AS GrossSales,
                   SUM(i.TotalDiscount)        AS Discounts,
                   SUM(i.AmountPaid)           AS Collections
            FROM sales.Invoice i
            WHERE i.ShopId = @ShopId
              AND i.InvoiceDate BETWEEN @From AND @To
              AND i.IsDeleted = 0
            GROUP BY CAST(i.InvoiceDate AS DATE)
            ORDER BY [Date]";

        var rows = await _dapper.Tenant().QueryAsync<DailySalesRow>(
            sql, new { ShopId = _tenant.ShopId, From = from, To = to });

        return Result<DailySalesRow[]>.Success(rows.ToArray());
    }, ct, useTransaction: false);
```

### Example 4: Cross-module coordination via interfaces

Never inject another module's `DbContext`. Always go through an interface:

```csharp
// Wrong — Billing reaching into Inventory's DbContext
var product = await _db.Set<Product>().FirstAsync(...);

// Right — Billing going through the Inventory service
var product = await _inventory.GetByIdAsync(productId, ct);
```

Arch test `NoCrossModuleDbContextAccess` fails the build for the wrong pattern.

## Testing contract

For every method this skill helps you write, you must also add (or update) tests in the module's test files (see `erp-scaffold-module`):

1. **Happy path** in `{Module}ServiceTests`
2. **Each validation failure branch** in `{Module}ServiceTests`
3. **Each error branch** (NotFound, Conflict, concurrency) in `{Module}ServiceTests`
4. **Cancellation** test — pass a pre-cancelled `CancellationToken`, assert `Result.Cancelled`
5. **Tenant isolation** — if the method reads or writes, add a case to `{Module}TenantIsolationTests`
6. **Audit** — if the method mutates, assert `AuditLog` row exists in `{Module}AuditTrailTests`
7. **Permission** — if exposed via controller, add a case to `{Module}ControllerTests` asserting 403 without permission

## Anti-patterns — reject on sight

- Bare `await _db.SaveChangesAsync(ct)` outside `ExecuteAsync`
- Catching `Exception` inside the method — let `ExecuteAsync` handle it
- Returning naked `T` instead of `Result<T>`
- Using `async void` — always `Task<T>` or `Task`
- Forgetting to pass `ct` through to downstream awaits
- `Task.Wait()` / `.Result` — always `await`
- Raw `HttpClient` for external APIs
- Private sequence tables — always `_sequence.NextAsync(...)`
- Hard-coding permission strings as `if (user.Perms.Contains("..."))` — the `[RequirePermission]` attribute on the controller is the only check needed
- Writing cross-tenant queries in a business service
- Skipping `useTransaction: true` on write paths because "it's simple"

## Template file

See `templates/ServiceMethod.cs.tpl` for the minimal boilerplate.
