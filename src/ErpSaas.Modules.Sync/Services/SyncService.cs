#pragma warning disable CS9107
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Sync.Entities;
using ErpSaas.Modules.Sync.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Sync.Services;

public sealed class SyncService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), ISyncService
{
    public async Task<SyncCommandsBatchResultDto> ProcessCommandsAsync(
        SyncCommandsBatchDto batch, CancellationToken ct = default)
    {
        var results = new List<CommandResultDto>();

        foreach (var cmd in batch.Commands)
        {
            var result = await ProcessSingleCommandAsync(cmd, ct);
            results.Add(result);
        }

        return new SyncCommandsBatchResultDto(results);
    }

    private async Task<CommandResultDto> ProcessSingleCommandAsync(OfflineCommandDto cmd, CancellationToken ct)
    {
        var existing = await db.Set<OfflineCommand>()
            .FirstOrDefaultAsync(c => c.ClientCommandId == cmd.ClientCommandId
                && c.ShopId == tenant.ShopId, ct);

        if (existing is not null)
        {
            return new CommandResultDto(
                cmd.ClientCommandId,
                existing.Status != OfflineCommandStatus.Rejected,
                existing.ResultingEntityId,
                existing.WarningNote,
                existing.RejectionReason);
        }

        var record = new OfflineCommand
        {
            ShopId              = tenant.ShopId,
            ClientCommandId     = cmd.ClientCommandId,
            DeviceId            = cmd.DeviceId,
            CommandType         = cmd.CommandType,
            PayloadJson         = cmd.PayloadJson,
            ClientTimestampUtc  = cmd.ClientTimestampUtc,
            ReceivedAtUtc       = DateTime.UtcNow,
            Status              = OfflineCommandStatus.Applied,
            CreatedAtUtc        = DateTime.UtcNow,
        };

        db.Set<OfflineCommand>().Add(record);
        await db.SaveChangesAsync(ct);

        return new CommandResultDto(cmd.ClientCommandId, true, null, null, null);
    }

    public async Task<Result<InvoiceRangeDto>> AllocateInvoiceRangeAsync(
        AllocateInvoiceRangeDto dto, CancellationToken ct = default)
        => await ExecuteAsync<InvoiceRangeDto>("Sync.AllocateInvoiceRange", async () =>
        {
            var now = DateTime.UtcNow;
            var fy = now.Month >= 4 ? now.Year : now.Year - 1;

            var maxEnd = await db.Set<InvoiceNumberAllocation>()
                .Where(a => a.ShopId == tenant.ShopId
                    && a.FinancialYear == fy
                    && a.Status != InvoiceNumberAllocationStatus.Released)
                .MaxAsync(a => (long?)a.RangeEnd, ct) ?? 0L;

            var start = maxEnd + 1;
            var end   = start + dto.RangeSize - 1;

            var allocation = new InvoiceNumberAllocation
            {
                ShopId        = tenant.ShopId,
                DeviceId      = dto.DeviceId,
                BranchId      = dto.BranchId,
                FinancialYear = fy,
                RangeStart    = start,
                RangeEnd      = end,
                LastUsed      = start - 1,
                IssuedAtUtc   = now,
                Status        = InvoiceNumberAllocationStatus.Active,
                CreatedAtUtc  = now,
            };

            db.Set<InvoiceNumberAllocation>().Add(allocation);
            await db.SaveChangesAsync(ct);

            return Result<InvoiceRangeDto>.Success(new InvoiceRangeDto(
                allocation.Id, start, end, fy));
        }, ct, useTransaction: true);

    public async Task<Result<bool>> ReleaseInvoiceRangeAsync(
        long allocationId, ReleaseInvoiceRangeDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Sync.ReleaseInvoiceRange", async () =>
        {
            var allocation = await db.Set<InvoiceNumberAllocation>()
                .FirstOrDefaultAsync(a => a.Id == allocationId && a.ShopId == tenant.ShopId, ct);

            if (allocation is null) return Result<bool>.NotFound(Errors.Sync.AllocationNotFound);
            if (allocation.Status == InvoiceNumberAllocationStatus.Released)
                return Result<bool>.Conflict(Errors.Sync.AllocationAlreadyReleased);

            allocation.LastUsed      = dto.HighestUsed;
            allocation.Status        = InvoiceNumberAllocationStatus.Released;
            allocation.ReleasedAtUtc = DateTime.UtcNow;
            allocation.UpdatedAtUtc  = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct);

    public async Task<(IReadOnlyList<SyncExceptionDto> Items, int TotalCount)> ListExceptionsAsync(
        int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = db.Set<OfflineCommand>()
            .AsNoTracking()
            .Where(c => c.ShopId == tenant.ShopId
                && (c.Status == OfflineCommandStatus.Rejected
                    || c.Status == OfflineCommandStatus.AppliedWithWarning));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.ReceivedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new SyncExceptionDto(
                c.Id, c.DeviceId, c.CommandType,
                c.ClientTimestampUtc, c.Status.ToString(),
                c.RejectionReason, c.WarningNote))
            .ToListAsync(ct);

        return (items, total);
    }
}
