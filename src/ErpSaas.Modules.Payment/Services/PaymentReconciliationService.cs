using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Payment.Connectors;
using ErpSaas.Modules.Payment.Entities;
using ErpSaas.Modules.Payment.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Payment.Services;

public sealed class PaymentReconciliationService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant,
    IGatewayConnectorRegistry connectorRegistry)
    : BaseService<TenantDbContext>(db, errorLogger), IPaymentReconciliationService
{
    public async Task<Result<int>> RunReconciliationAsync(string gatewayCode, DateTime settlementDate, CancellationToken ct = default)
    {
        return await ExecuteAsync("Payment.Reconcile", async () =>
        {
            var connector = await connectorRegistry.ResolveAsync(gatewayCode, ct);
            var report = await connector.FetchSettlementReportAsync(settlementDate, ct);

            // Index our Success transactions by GatewayTxnId for O(1) lookup
            var ourTxns = await _db.Set<PaymentGatewayTransaction>()
                .Where(t => t.GatewayCode == gatewayCode
                         && t.Status == PaymentGatewayStatus.Success
                         && t.InitiatedAtUtc.Date <= settlementDate.Date)
                .ToListAsync(ct);

            var ourByGatewayId = ourTxns.ToDictionary(t => t.GatewayTxnId, StringComparer.OrdinalIgnoreCase);
            var reportGatewayIds = report.Lines.Select(l => l.GatewayTxnId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            int exceptionsCreated = 0;

            // 1. Match each line from the gateway settlement report against our transactions
            foreach (var line in report.Lines)
            {
                if (!ourByGatewayId.TryGetValue(line.GatewayTxnId, out var txn))
                {
                    // Gateway has a transaction we don't know about
                    if (await HasOpenException(line.GatewayTxnId, null, ct)) continue;
                    AddException(gatewayCode, line.GatewayTxnId, line.OurReference, null,
                        ReconciliationExceptionType.MissingInOurDb,
                        null, line.SettledAmount, null, line.Fee);
                    exceptionsCreated++;
                    continue;
                }

                var amountMatch = Math.Abs(txn.Amount - line.SettledAmount) < 0.01m;
                if (!amountMatch)
                {
                    if (await HasOpenException(line.GatewayTxnId, txn.Id, ct)) continue;
                    AddException(gatewayCode, line.GatewayTxnId, line.OurReference, txn.Id,
                        ReconciliationExceptionType.AmountMismatch,
                        txn.Amount, line.SettledAmount, txn.GatewayFee, line.Fee);
                    exceptionsCreated++;
                    continue;
                }

                // Stamp settlement fields — money confirmed received
                if (txn.SettledAtUtc is null)
                {
                    txn.SettledAtUtc = line.SettledAtUtc;
                    txn.SettlementReference = $"{gatewayCode}-{settlementDate:yyyyMMdd}";
                    txn.GatewayFee = line.Fee;
                    txn.GatewayGst = line.GstOnFee;
                    txn.NetSettled = line.NetSettled;
                    txn.UpdatedAtUtc = DateTime.UtcNow;
                }
            }

            // 2. Our Success transactions not present in the gateway report
            foreach (var txn in ourTxns.Where(t => t.SettledAtUtc == null && !reportGatewayIds.Contains(t.GatewayTxnId)))
            {
                if (await HasOpenException(txn.GatewayTxnId, txn.Id, ct)) continue;
                AddException(gatewayCode, txn.GatewayTxnId, txn.OurReferenceNumber, txn.Id,
                    ReconciliationExceptionType.MissingInGateway,
                    txn.Amount, null, txn.GatewayFee, null);
                exceptionsCreated++;
            }

            await _db.SaveChangesAsync(ct);
            return Result<int>.Success(exceptionsCreated);
        }, ct, useTransaction: true);
    }

    public async Task<IReadOnlyList<ReconciliationExceptionDto>> ListExceptionsAsync(
        ReconciliationExceptionStatus? status, CancellationToken ct = default)
    {
        var q = _db.Set<ReconciliationException>().AsQueryable();
        if (status.HasValue) q = q.Where(e => e.Status == status.Value);

        return await q
            .OrderByDescending(e => e.DetectedAtUtc)
            .Select(e => new ReconciliationExceptionDto(
                e.Id, e.GatewayCode, e.GatewayTxnId, e.OurReferenceNumber,
                e.PaymentGatewayTransactionId, e.ExceptionType, e.Status,
                e.OurAmount, e.GatewayAmount, e.OurFee, e.GatewayFee,
                e.Notes, e.DetectedAtUtc, e.ResolvedAtUtc, e.ResolutionNotes))
            .ToListAsync(ct);
    }

    public async Task<Result<bool>> ResolveExceptionAsync(long exceptionId, ResolveExceptionDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Payment.ResolveException", async () =>
        {
            var ex = await _db.Set<ReconciliationException>()
                .FirstOrDefaultAsync(e => e.Id == exceptionId, ct);
            if (ex is null) return Result<bool>.NotFound(Errors.Payment.ExceptionNotFound);
            if (ex.Status == ReconciliationExceptionStatus.Resolved)
                return Result<bool>.Conflict(Errors.Payment.ExceptionAlreadyResolved);

            ex.Status = ReconciliationExceptionStatus.Resolved;
            ex.ResolvedAtUtc = DateTime.UtcNow;
            ex.ResolvedByUserId = tenant.CurrentUserId;
            ex.ResolutionNotes = dto.ResolutionNotes;
            ex.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    private async Task<bool> HasOpenException(string gatewayTxnId, long? txnId, CancellationToken ct)
        => await _db.Set<ReconciliationException>()
            .AnyAsync(e => (txnId.HasValue ? e.PaymentGatewayTransactionId == txnId : e.GatewayTxnId == gatewayTxnId)
                        && e.Status == ReconciliationExceptionStatus.Open, ct);

    private void AddException(
        string gatewayCode, string gatewayTxnId, string ourRef, long? txnId,
        ReconciliationExceptionType type,
        decimal? ourAmount, decimal? gatewayAmount, decimal? ourFee, decimal? gatewayFee)
    {
        _db.Set<ReconciliationException>().Add(new ReconciliationException
        {
            ShopId = tenant.ShopId,
            GatewayCode = gatewayCode,
            GatewayTxnId = gatewayTxnId,
            OurReferenceNumber = ourRef,
            PaymentGatewayTransactionId = txnId,
            ExceptionType = type,
            Status = ReconciliationExceptionStatus.Open,
            OurAmount = ourAmount,
            GatewayAmount = gatewayAmount,
            OurFee = ourFee,
            GatewayFee = gatewayFee,
            DetectedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
        });
    }
}
