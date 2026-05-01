using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Accounting.Entities;
using ErpSaas.Modules.Accounting.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Accounting.Services;

public sealed class FixedAssetService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant,
    ILogger<FixedAssetService> logger)
    : BaseService<TenantDbContext>(db, errorLogger), IFixedAssetService
{
    // COA codes seeded in AccountingTenantSeeder
    private const string FixedAssetGrossCode        = "1500";
    private const string AccumDepreciationCode       = "1510";
    private const string DepreciationExpenseCode     = "5500";
    private const string GainOnDisposalCode          = "4900";
    private const string LossOnDisposalCode          = "5900";

    public async Task<PagedResult<FixedAssetListDto>> ListAsync(
        FixedAssetStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Set<FixedAsset>()
            .Where(f => !f.IsDeleted && (status == null || f.Status == status));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(f => f.AssetCode)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(f => new FixedAssetListDto(
                f.Id, f.AssetCode, f.Name, f.CategoryCode, f.PurchaseDate,
                f.PurchaseCost, f.AccumulatedDepreciation, f.NetBookValue, f.Status))
            .ToListAsync(ct);

        return new PagedResult<FixedAssetListDto>(items, total, page, pageSize);
    }

    public async Task<Result<long>> RegisterAsync(RegisterFixedAssetDto dto, CancellationToken ct = default)
        => await ExecuteAsync("FixedAsset.Register", async () =>
        {
            var assetCode = await sequence.NextAsync(
                Constants.SequenceCodes.FixedAsset, tenant.ShopId, ct);

            var rate = dto.Method == DepreciationMethod.StraightLine
                ? (dto.UsefulLifeYears > 0 ? 100m / dto.UsefulLifeYears : 0m)
                : dto.SalvageValue > 0
                    ? Math.Round((1m - (decimal)Math.Pow((double)(dto.SalvageValue / dto.PurchaseCost), 1d / (double)dto.UsefulLifeYears)) * 100m, 4)
                    : 0m;

            var asset = new FixedAsset
            {
                ShopId = tenant.ShopId,
                AssetCode = assetCode,
                Name = dto.Name,
                CategoryCode = dto.CategoryCode,
                PurchaseDate = dto.PurchaseDate,
                PurchaseCost = dto.PurchaseCost,
                Method = dto.Method,
                UsefulLifeYears = dto.UsefulLifeYears,
                SalvageValue = dto.SalvageValue,
                RateOfDepreciation = rate,
                AccumulatedDepreciation = 0,
                NetBookValue = dto.PurchaseCost,
                Status = FixedAssetStatus.InUse,
                SupplierId = dto.SupplierId,
                LocationNotes = dto.LocationNotes,
                AssignedToEmployeeId = dto.AssignedToEmployeeId,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<FixedAsset>().Add(asset);
            await _db.SaveChangesAsync(ct);

            return Result<long>.Success(asset.Id);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> RetireAsync(long id, CancellationToken ct = default)
        => await ExecuteAsync("FixedAsset.Retire", async () =>
        {
            var asset = await _db.Set<FixedAsset>()
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, ct);
            if (asset is null) return Result<bool>.NotFound("Fixed asset not found");

            if (asset.Status != FixedAssetStatus.InUse && asset.Status != FixedAssetStatus.UnderMaintenance)
                return Result<bool>.Conflict("Asset must be In Use or Under Maintenance to retire");

            asset.Status = FixedAssetStatus.Retired;
            asset.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<Result<bool>> DisposeAsync(long id, DisposeFixedAssetDto dto, CancellationToken ct = default)
        => await ExecuteAsync("FixedAsset.Dispose", async () =>
        {
            var asset = await _db.Set<FixedAsset>()
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, ct);
            if (asset is null) return Result<bool>.NotFound("Fixed asset not found");

            if (asset.Status is FixedAssetStatus.Sold or FixedAssetStatus.Disposed)
                return Result<bool>.Conflict("Asset already disposed");

            var grossAccount = await FindAccountByCodeAsync(FixedAssetGrossCode, ct);
            var accumDepAccount = await FindAccountByCodeAsync(AccumDepreciationCode, ct);
            var gainAccount = await FindAccountByCodeAsync(GainOnDisposalCode, ct);
            var lossAccount = await FindAccountByCodeAsync(LossOnDisposalCode, ct);

            if (grossAccount is null || accumDepAccount is null)
                return Result<bool>.Failure("Fixed asset COA accounts not found. Ensure 1500 and 1510 are seeded.");

            var gainOrLoss = dto.DisposalValue - asset.NetBookValue;
            var voucherNumber = await sequence.NextAsync(
                Constants.SequenceCodes.VoucherJournal, tenant.ShopId, ct);

            var disposeVoucher = new Voucher
            {
                ShopId = tenant.ShopId,
                VoucherNumber = voucherNumber,
                VoucherDate = dto.DisposalDate,
                VoucherType = VoucherType.Journal,
                Status = VoucherStatus.Posted,
                IsPosted = true,
                PostedAtUtc = DateTime.UtcNow,
                Narration = $"Disposal of fixed asset {asset.AssetCode} — {asset.Name}",
                TotalDebit = asset.PurchaseCost + (gainOrLoss < 0 ? Math.Abs(gainOrLoss) : 0),
                TotalCredit = asset.PurchaseCost + (gainOrLoss < 0 ? Math.Abs(gainOrLoss) : 0),
                SourceDocumentType = "FixedAsset",
                SourceDocumentId = asset.Id,
                CreatedAtUtc = DateTime.UtcNow,
            };

            // Dr Accumulated Depreciation
            disposeVoucher.Entries.Add(new VoucherEntry { ShopId = tenant.ShopId, AccountId = accumDepAccount.Id, Type = DebitCredit.Debit, Amount = asset.AccumulatedDepreciation, CreatedAtUtc = DateTime.UtcNow });
            // Dr Proceeds account
            if (dto.DisposalValue > 0)
                disposeVoucher.Entries.Add(new VoucherEntry { ShopId = tenant.ShopId, AccountId = dto.ProceedsAccountId, Type = DebitCredit.Debit, Amount = dto.DisposalValue, CreatedAtUtc = DateTime.UtcNow });
            // Dr Loss on Disposal (if loss)
            if (gainOrLoss < 0 && lossAccount is not null)
                disposeVoucher.Entries.Add(new VoucherEntry { ShopId = tenant.ShopId, AccountId = lossAccount.Id, Type = DebitCredit.Debit, Amount = Math.Abs(gainOrLoss), CreatedAtUtc = DateTime.UtcNow });
            // Cr Fixed Asset Gross Cost
            disposeVoucher.Entries.Add(new VoucherEntry { ShopId = tenant.ShopId, AccountId = grossAccount.Id, Type = DebitCredit.Credit, Amount = asset.PurchaseCost, CreatedAtUtc = DateTime.UtcNow });
            // Cr Gain on Disposal (if gain)
            if (gainOrLoss > 0 && gainAccount is not null)
                disposeVoucher.Entries.Add(new VoucherEntry { ShopId = tenant.ShopId, AccountId = gainAccount.Id, Type = DebitCredit.Credit, Amount = gainOrLoss, CreatedAtUtc = DateTime.UtcNow });

            _db.Set<Voucher>().Add(disposeVoucher);
            await _db.SaveChangesAsync(ct);

            asset.Status = FixedAssetStatus.Sold;
            asset.DisposalDate = dto.DisposalDate;
            asset.DisposalValue = dto.DisposalValue;
            asset.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct, useTransaction: true);

    public async Task<IReadOnlyList<DepreciationScheduleEntryDto>> GetDepreciationScheduleAsync(
        long id, CancellationToken ct = default)
        => await _db.Set<DepreciationEntry>()
            .Where(d => d.FixedAssetId == id && !d.IsDeleted)
            .OrderBy(d => d.PeriodDate)
            .Select(d => new DepreciationScheduleEntryDto(
                d.PeriodDate, d.Amount, d.AccumulatedAfter, d.NetBookValueAfter, d.VoucherId))
            .ToListAsync(ct);

    public async Task<Result<int>> RunDepreciationAsync(DateTime periodDate, CancellationToken ct = default)
        => await ExecuteAsync("FixedAsset.RunDepreciation", async () =>
        {
            var assets = await _db.Set<FixedAsset>()
                .Where(f => !f.IsDeleted && f.Status == FixedAssetStatus.InUse)
                .ToListAsync(ct);

            var depExpenseAccount = await FindAccountByCodeAsync(DepreciationExpenseCode, ct);
            var accumDepAccount   = await FindAccountByCodeAsync(AccumDepreciationCode, ct);

            if (depExpenseAccount is null || accumDepAccount is null)
            {
                logger.LogWarning("Depreciation accounts not found; skipping depreciation run");
                return Result<int>.Failure("Depreciation COA accounts not found");
            }

            int processed = 0;
            foreach (var asset in assets)
            {
                // Idempotent — skip if entry already posted for this period
                var alreadyRun = await _db.Set<DepreciationEntry>()
                    .AnyAsync(d => d.FixedAssetId == asset.Id && d.PeriodDate == periodDate.Date, ct);
                if (alreadyRun) continue;

                var depAmount = ComputeMonthlyDepreciation(asset);
                if (depAmount <= 0) continue;

                // Don't depreciate below salvage value
                var remaining = asset.NetBookValue - asset.SalvageValue;
                if (remaining <= 0) continue;
                depAmount = Math.Min(depAmount, remaining);

                var voucherNumber = await sequence.NextAsync(
                    Constants.SequenceCodes.VoucherJournal, tenant.ShopId, ct);

                var v = new Voucher
                {
                    ShopId = tenant.ShopId,
                    VoucherNumber = voucherNumber,
                    VoucherDate = periodDate,
                    VoucherType = VoucherType.Journal,
                    Status = VoucherStatus.Posted,
                    IsPosted = true,
                    PostedAtUtc = DateTime.UtcNow,
                    Narration = $"Monthly depreciation — {asset.AssetCode} {asset.Name} ({periodDate:MMM yyyy})",
                    TotalDebit = depAmount,
                    TotalCredit = depAmount,
                    SourceDocumentType = "DepreciationEntry",
                    SourceDocumentId = asset.Id,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                v.Entries.Add(new VoucherEntry { ShopId = tenant.ShopId, AccountId = depExpenseAccount.Id, Type = DebitCredit.Debit,  Amount = depAmount, CreatedAtUtc = DateTime.UtcNow });
                v.Entries.Add(new VoucherEntry { ShopId = tenant.ShopId, AccountId = accumDepAccount.Id,   Type = DebitCredit.Credit, Amount = depAmount, CreatedAtUtc = DateTime.UtcNow });
                _db.Set<Voucher>().Add(v);
                await _db.SaveChangesAsync(ct);

                var newAccumulated = asset.AccumulatedDepreciation + depAmount;
                var newNbv = asset.PurchaseCost - newAccumulated;

                _db.Set<DepreciationEntry>().Add(new DepreciationEntry
                {
                    ShopId = tenant.ShopId,
                    FixedAssetId = asset.Id,
                    PeriodDate = periodDate.Date,
                    Amount = depAmount,
                    AccumulatedAfter = newAccumulated,
                    NetBookValueAfter = newNbv,
                    VoucherId = v.Id,
                    CreatedAtUtc = DateTime.UtcNow,
                });

                asset.AccumulatedDepreciation = newAccumulated;
                asset.NetBookValue = newNbv;
                asset.UpdatedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                processed++;
            }

            logger.LogInformation("Depreciation run for {Period}: {Count} assets processed", periodDate, processed);
            return Result<int>.Success(processed);
        }, ct, useTransaction: false); // each asset in its own mini-transaction via SaveChangesAsync

    private static decimal ComputeMonthlyDepreciation(FixedAsset asset)
    {
        if (asset.Method == DepreciationMethod.StraightLine)
        {
            var annualDep = (asset.PurchaseCost - asset.SalvageValue) /
                            (asset.UsefulLifeYears > 0 ? asset.UsefulLifeYears : 1);
            return Math.Round(annualDep / 12, 2);
        }
        else // WDV
        {
            var monthlyRate = asset.RateOfDepreciation / 100m / 12m;
            return Math.Round(asset.NetBookValue * monthlyRate, 2);
        }
    }

    private Task<Account?> FindAccountByCodeAsync(string code, CancellationToken ct)
        => _db.Set<Account>()
            .FirstOrDefaultAsync(a => a.ShopId == tenant.ShopId && a.Code == code && !a.IsDeleted, ct);
}
