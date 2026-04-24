using ErpSaas.Core.Results;
using ErpSaas.Modules.{Module}.Entities;
using ErpSaas.Shared.Auditing;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.SequenceManagement;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.{Module}.Services;

public interface I{Module}Service
{
    Task<Result<long>> CreateAsync({EntityName}Dto dto, CancellationToken ct);
    Task<Result<{EntityName}>> GetAsync(long id, CancellationToken ct);
    Task<Result<PagedList<{EntityName}>>> ListAsync({EntityName}ListFilter filter, CancellationToken ct);
    Task<Result> UpdateAsync(long id, {EntityName}Dto dto, CancellationToken ct);
    Task<Result> DeleteAsync(long id, CancellationToken ct);
}

public class {Module}Service : I{Module}Service
{
    private readonly TenantDbContext _db;
    private readonly IBaseService _baseService;
    private readonly ISequenceService _sequence;
    private readonly IAuditLogger _audit;

    public {Module}Service(
        TenantDbContext db,
        IBaseService baseService,
        ISequenceService sequence,
        IAuditLogger audit)
    {
        _db = db;
        _baseService = baseService;
        _sequence = sequence;
        _audit = audit;
    }

    public Task<Result<long>> CreateAsync({EntityName}Dto dto, CancellationToken ct) =>
        _baseService.ExecuteAsync("{Module}.Create", async () =>
        {
            // 1. Validation (FluentValidation already ran in pipeline; add business rules here)
            // 2. Allocate document number from the unified sequence service (§5.16)
            //    var seq = await _sequence.NextAsync("{MODULE_SEQUENCE_CODE}",
            //        targetEntityType: "{EntityName}", ct: ct);
            //
            // 3. Map DTO → entity, capture snapshot columns, set quantities in base unit
            // 4. Add to context + SaveChangesAsync
            // 5. Return Result<long>.Success(entity.Id)

            throw new NotImplementedException("Scaffold — implement per §{X.Y}");
        }, ct, useTransaction: true);

    public Task<Result<{EntityName}>> GetAsync(long id, CancellationToken ct) =>
        _baseService.ExecuteAsync("{Module}.Get", async () =>
        {
            var entity = await _db.Set<{EntityName}>()
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            return entity is null
                ? Result<{EntityName}>.NotFound($"{nameof({EntityName})} {id} not found")
                : Result<{EntityName}>.Success(entity);
        }, ct, useTransaction: false);

    public Task<Result<PagedList<{EntityName}>>> ListAsync({EntityName}ListFilter filter, CancellationToken ct) =>
        _baseService.ExecuteAsync("{Module}.List", async () =>
        {
            var q = _db.Set<{EntityName}>().AsNoTracking();
            // Apply filter.{Field} conditions here
            var total = await q.CountAsync(ct);
            var items = await q.Skip(filter.Skip).Take(filter.Take).ToListAsync(ct);
            return Result<PagedList<{EntityName}>>.Success(new PagedList<{EntityName}>(items, total, filter.Page, filter.Take));
        }, ct, useTransaction: false);

    public Task<Result> UpdateAsync(long id, {EntityName}Dto dto, CancellationToken ct) =>
        _baseService.ExecuteAsync("{Module}.Update", async () =>
        {
            var entity = await _db.Set<{EntityName}>().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return Result.NotFound($"{nameof({EntityName})} {id} not found");

            // Map dto → entity (only permitted-to-edit fields — do NOT touch Id, ShopId, audit columns)
            // Check business rules (e.g., cannot edit a finalized invoice)
            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }, ct, useTransaction: true);

    public Task<Result> DeleteAsync(long id, CancellationToken ct) =>
        _baseService.ExecuteAsync("{Module}.Delete", async () =>
        {
            var entity = await _db.Set<{EntityName}>().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return Result.NotFound($"{nameof({EntityName})} {id} not found");

            // Soft delete — NEVER hard-delete business entities
            entity.IsDeleted = true;
            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }, ct, useTransaction: true);
}
