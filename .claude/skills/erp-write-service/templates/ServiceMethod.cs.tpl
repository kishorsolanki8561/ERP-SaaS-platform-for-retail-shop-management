//  ╭─────────────────────────────────────────────────────────────────────╮
//  │  Service method template — plan §5.1 BaseService pattern           │
//  │  EVERY service method MUST use this shape.                          │
//  │  Arch tests fail the build if SaveChangesAsync is called outside.   │
//  ╰─────────────────────────────────────────────────────────────────────╯

public Task<Result<{TReturn}>> {MethodName}Async({parameters}, CancellationToken ct) =>
    _baseService.ExecuteAsync(operationName: "{Module}.{Method}", async () =>
    {
        //  1. Load — lock rows if concurrent writes are possible
        //     var entity = await _db.Set<{Entity}>().FirstOrDefaultAsync(x => x.Id == id, ct);
        //     if (entity is null) return Result<{TReturn}>.NotFound("{msg}");

        //  2. Validate business rules beyond FluentValidation
        //     if (entity.StatusCode == "FINALIZED")
        //         return Result<{TReturn}>.Conflict("Already finalized");

        //  3. Allocate a document number — only if the method creates a document
        //     var seq = await _sequence.NextAsync(
        //         code: "{SEQUENCE_CODE}",
        //         targetEntityType: nameof({Entity}),
        //         targetEntityId: entity.Id,
        //         ct: ct);
        //     entity.{NumberField} = seq.RenderedText;

        //  4. Snapshot columns for any quantity-bearing row (§6.3.c)
        //     line.UnitCodeSnapshot = productUnit.UnitCode;
        //     line.ConversionFactorSnapshot = productUnit.ConversionFactorToBase;
        //     line.QuantityInBaseUnit = line.QuantityInBilledUnit * line.ConversionFactorSnapshot;

        //  5. Call out to dependent services via interfaces — never another DbContext
        //     await _inventory.DecrementAsync(productId, qtyBase, ct);
        //     await _accounting.PostVoucherAsync(voucherDto, ct);

        //  6. Persist
        //     await _db.SaveChangesAsync(ct);

        //  7. Fire-and-forget side-effects (notifications, analytics)
        //     _ = _notifications.EnqueueAsync("EVENT_CODE", ..., ct);

        //  8. Return — use the right Result<T> factory:
        //       Result<T>.Success(value)          — happy path
        //       Result<T>.NotFound("message")     — resource missing
        //       Result<T>.Conflict("message")     — business-rule violation
        //       Result<T>.Validation(errors)      — input invalid (usually caught in pipeline)
        //       Result<T>.Forbidden()             — should be caught by [RequirePermission]
        //     NEVER throw for expected failures — return a Result.

        return Result<{TReturn}>.Success({value});
    }, ct, useTransaction: {true | false});
