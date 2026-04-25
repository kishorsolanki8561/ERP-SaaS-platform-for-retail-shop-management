#pragma warning disable CS9107
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Messaging.Enums;
using ErpSaas.Infrastructure.Messaging;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Shift.Entities;
using ErpSaas.Modules.Shift.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Shift.Services;

public sealed class ShiftService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant,
    INotificationService? notifications = null,
    ILogger<ShiftService>? logger = null)
    : BaseService<TenantDbContext>(db, errorLogger), IShiftService, IShiftLookup
{
    public async Task<Result<long>> OpenShiftAsync(OpenShiftDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Shift.Open", async () =>
        {
            var existing = await db.Set<Entities.Shift>()
                .AnyAsync(s => s.ShopId == tenant.ShopId
                    && s.CashierUserId == tenant.CurrentUserId
                    && s.BranchId == dto.BranchId
                    && s.Status == ShiftStatus.Open, ct);

            if (existing)
                return Result<long>.Conflict(Errors.Shift.AlreadyOpen);

            var shift = new Entities.Shift
            {
                ShopId = tenant.ShopId,
                BranchId = dto.BranchId,
                CashierUserId = tenant.CurrentUserId,
                CashierNameSnapshot = dto.CashierName,
                CashierPhoneSnapshot = dto.CashierPhone,
                OpenedAtUtc = DateTime.UtcNow,
                OpeningCash = dto.OpeningCash,
                Status = ShiftStatus.Open,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.Set<Entities.Shift>().Add(shift);
            await db.SaveChangesAsync(ct);

            if (dto.Denominations?.Count > 0)
                await SaveDenominationsAsync(shift.Id, ShiftDenominationPhase.Opening, dto.Denominations, ct);

            return Result<long>.Success(shift.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> CloseShiftAsync(
        long shiftId, CloseShiftDto dto, CancellationToken ct = default)
    {
        var result = await ExecuteAsync<bool>("Shift.Close", async () =>
        {
            var shift = await db.Set<Entities.Shift>()
                .Include(s => s.CashMovements)
                .FirstOrDefaultAsync(s => s.Id == shiftId && s.ShopId == tenant.ShopId && !s.IsDeleted, ct);

            if (shift is null) return Result<bool>.NotFound(Errors.Shift.NotFound);
            if (shift.Status != ShiftStatus.Open) return Result<bool>.Conflict(Errors.Shift.NotOpen);

            var totalCashIn = shift.CashMovements
                .Where(m => m.Type == ShiftCashMovementType.CashIn)
                .Sum(m => m.Amount);
            var totalCashOut = shift.CashMovements
                .Where(m => m.Type is ShiftCashMovementType.CashOut or ShiftCashMovementType.PettyExpense)
                .Sum(m => m.Amount);

            var systemCash = shift.OpeningCash + totalCashIn + shift.TotalCashSales
                - totalCashOut - shift.TotalCashRefunds;

            shift.Status = ShiftStatus.Closed;
            shift.ClosedAtUtc = DateTime.UtcNow;
            shift.ClosingCashCounted = dto.ClosingCashCounted;
            shift.SystemComputedCash = systemCash;
            shift.CashVariance = dto.ClosingCashCounted - systemCash;
            shift.ClosingNotes = dto.Notes;

            await db.SaveChangesAsync(ct);

            if (dto.Denominations?.Count > 0)
                await SaveDenominationsAsync(shiftId, ShiftDenominationPhase.Closing, dto.Denominations, ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

        if (result.IsSuccess)
            await TrySendCloseNotificationAsync(shiftId, ct);

        return result;
    }

    private async Task TrySendCloseNotificationAsync(long shiftId, CancellationToken ct)
    {
        if (notifications is null) return;
        try
        {
            var shift = await db.Set<Entities.Shift>()
                .FirstOrDefaultAsync(s => s.Id == shiftId && !s.IsDeleted, ct);

            if (shift?.CashierPhoneSnapshot is null) return;

            await notifications.EnqueueAsync(
                shift.ShopId,
                NotificationChannel.Sms,
                shift.CashierPhoneSnapshot,
                Constants.NotificationCodes.ShiftClosed,
                new Dictionary<string, string>
                {
                    { "CashierName", shift.CashierNameSnapshot },
                    { "TotalSales",  shift.TotalSales.ToString("F2") },
                    { "CashVariance", (shift.CashVariance ?? 0m).ToString("F2") },
                },
                correlationId: $"Shift:{shiftId}",
                ct);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Non-critical: failed to enqueue SMS for shift {Id}", shiftId);
        }
    }

    public async Task<Result<bool>> ForceCloseAsync(
        long shiftId, string reason, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Shift.ForceClose", async () =>
        {
            var shift = await db.Set<Entities.Shift>()
                .FirstOrDefaultAsync(s => s.Id == shiftId && s.ShopId == tenant.ShopId && !s.IsDeleted, ct);

            if (shift is null) return Result<bool>.NotFound(Errors.Shift.NotFound);
            if (shift.Status != ShiftStatus.Open) return Result<bool>.Conflict(Errors.Shift.NotOpen);

            shift.Status = ShiftStatus.ForcedClosed;
            shift.ClosedAtUtc = DateTime.UtcNow;
            shift.ForcedClosedByUserId = tenant.CurrentUserId;
            shift.ClosingNotes = reason;

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public Task<Result<bool>> RecordCashInAsync(long shiftId, CashMovementDto dto, CancellationToken ct = default)
        => RecordMovementAsync(shiftId, ShiftCashMovementType.CashIn, dto, ct);

    public Task<Result<bool>> RecordCashOutAsync(long shiftId, CashMovementDto dto, CancellationToken ct = default)
        => RecordMovementAsync(shiftId, ShiftCashMovementType.CashOut, dto, ct);

    public Task<ShiftSummaryDto?> GetOpenShiftForCashierAsync(
        long userId, long branchId, CancellationToken ct = default)
        => db.Set<Entities.Shift>()
            .Where(s => s.ShopId == tenant.ShopId
                && s.CashierUserId == userId
                && s.BranchId == branchId
                && s.Status == ShiftStatus.Open
                && !s.IsDeleted)
            .Select(s => (ShiftSummaryDto?)MapToSummary(s))
            .FirstOrDefaultAsync(ct);

    public Task<ShiftSummaryDto?> GetShiftSummaryAsync(long shiftId, CancellationToken ct = default)
        => db.Set<Entities.Shift>()
            .Where(s => s.Id == shiftId && s.ShopId == tenant.ShopId && !s.IsDeleted)
            .Select(s => (ShiftSummaryDto?)MapToSummary(s))
            .FirstOrDefaultAsync(ct);

    public async Task<PagedResult<ShiftListItemDto>> ListShiftsAsync(
        int page, int pageSize, long? branchId, CancellationToken ct = default)
    {
        var query = db.Set<Entities.Shift>()
            .Where(s => s.ShopId == tenant.ShopId && !s.IsDeleted);

        if (branchId.HasValue)
            query = query.Where(s => s.BranchId == branchId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.OpenedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ShiftListItemDto(
                s.Id, s.CashierNameSnapshot, s.OpenedAtUtc, s.Status,
                s.TotalSales, s.TransactionCount, s.CashVariance))
            .ToListAsync(ct);

        return new PagedResult<ShiftListItemDto>(items, total, page, pageSize);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<Result<bool>> RecordMovementAsync(
        long shiftId, ShiftCashMovementType type, CashMovementDto dto, CancellationToken ct)
        => await ExecuteAsync<bool>($"Shift.Record{type}", async () =>
        {
            var shift = await db.Set<Entities.Shift>()
                .FirstOrDefaultAsync(s => s.Id == shiftId && s.ShopId == tenant.ShopId && !s.IsDeleted, ct);

            if (shift is null) return Result<bool>.NotFound(Errors.Shift.NotFound);
            if (shift.Status != ShiftStatus.Open) return Result<bool>.Conflict(Errors.Shift.NotOpen);

            db.Set<ShiftCashMovement>().Add(new ShiftCashMovement
            {
                ShopId = tenant.ShopId,
                ShiftId = shiftId,
                MovementAtUtc = DateTime.UtcNow,
                Type = type,
                Amount = dto.Amount,
                ReasonCode = dto.ReasonCode,
                Notes = dto.Notes,
                AuthorizedByUserId = dto.AuthorizedByUserId,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    private async Task SaveDenominationsAsync(
        long shiftId,
        ShiftDenominationPhase phase,
        IReadOnlyList<DenominationDto> denominations,
        CancellationToken ct)
    {
        foreach (var d in denominations.Where(x => x.Count > 0))
        {
            db.Set<ShiftDenominationCount>().Add(new ShiftDenominationCount
            {
                ShopId = tenant.ShopId,
                ShiftId = shiftId,
                Phase = phase,
                Denomination = d.Denomination,
                Count = d.Count,
                Subtotal = d.Denomination * d.Count,
                CreatedAtUtc = DateTime.UtcNow,
            });
        }
        await db.SaveChangesAsync(ct);
    }

    // ── IShiftLookup (cross-module contract used by BillingService) ───────────

    public Task<bool> IsShiftOpenAsync(long shiftId, long shopId, CancellationToken ct = default)
        => db.Set<Entities.Shift>()
            .AnyAsync(s => s.Id == shiftId
                && s.ShopId == shopId
                && s.Status == ShiftStatus.Open
                && !s.IsDeleted, ct);

    private static ShiftSummaryDto MapToSummary(Entities.Shift s)
        => new(s.Id, s.BranchId, s.CashierNameSnapshot, s.OpenedAtUtc, s.OpeningCash,
            s.Status, s.TransactionCount, s.TotalSales, s.TotalCashSales,
            s.TotalCardSales, s.TotalUpiSales, s.TotalWalletDebits, s.TotalCashRefunds,
            s.ClosedAtUtc, s.ClosingCashCounted, s.SystemComputedCash, s.CashVariance, s.ClosingNotes);
}
