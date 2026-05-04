using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Verticals.Medical.Entities;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Verticals.Medical.Services;

public sealed class MedicalInventoryService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IMedicalInventoryService
{
    public async Task<Result<long>> CreateBatchAsync(CreateDrugBatchDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Medical.CreateBatch", async () =>
        {
            var exists = await _db.Set<DrugBatch>()
                .AnyAsync(b => b.ProductId == dto.ProductId && b.BatchNumber == dto.BatchNumber, ct);
            if (exists) return Result<long>.Conflict(Errors.Medical.BatchNumberExists);

            var batch = new DrugBatch
            {
                ShopId = tenant.ShopId,
                ProductId = dto.ProductId,
                ProductNameSnapshot = "—",
                BatchNumber = dto.BatchNumber,
                GenericName = dto.GenericName,
                Manufacturer = dto.Manufacturer,
                Schedule = dto.Schedule,
                ManufactureDate = dto.ManufactureDate,
                ExpiryDate = dto.ExpiryDate,
                InitialQuantity = dto.InitialQuantity,
                CurrentQuantity = dto.InitialQuantity,
                PurchasePrice = dto.PurchasePrice,
                SellingPrice = dto.SellingPrice,
                PurchaseBillId = dto.PurchaseBillId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<DrugBatch>().Add(batch);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(batch.Id);
        }, ct, useTransaction: true);
    }

    public async Task<DrugBatchDto?> GetBatchAsync(long batchId, CancellationToken ct = default)
    {
        var b = await _db.Set<DrugBatch>().FirstOrDefaultAsync(x => x.Id == batchId, ct);
        return b is null ? null : Map(b);
    }

    public async Task<IReadOnlyList<DrugBatchDto>> ListBatchesAsync(long? productId, bool? expiringWithin30Days, CancellationToken ct = default)
    {
        var query = _db.Set<DrugBatch>().Where(b => b.IsActive);
        if (productId.HasValue) query = query.Where(b => b.ProductId == productId.Value);
        if (expiringWithin30Days == true)
        {
            var cutoff = DateTime.UtcNow.AddDays(30);
            query = query.Where(b => b.ExpiryDate <= cutoff);
        }
        var list = await query.OrderBy(b => b.ExpiryDate).ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<DrugBatchDto>> ListBatchesByProductAsync(long productId, CancellationToken ct = default)
    {
        var list = await _db.Set<DrugBatch>()
            .Where(b => b.ProductId == productId && b.IsActive)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<Result<bool>> RecordPrescriptionAsync(RecordPrescriptionDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Medical.RecordPrescription", async () =>
        {
            var batch = await _db.Set<DrugBatch>()
                .FirstOrDefaultAsync(b => b.Id == dto.DrugBatchId, ct);
            if (batch is null) return Result<bool>.NotFound(Errors.Medical.BatchNotFound);
            if (batch.ExpiryDate < DateTime.UtcNow) return Result<bool>.Conflict(Errors.Medical.BatchExpired);

            var record = new PrescriptionRecord
            {
                ShopId = tenant.ShopId,
                DrugBatchId = dto.DrugBatchId,
                InvoiceId = dto.InvoiceId,
                CustomerId = dto.CustomerId,
                DoctorName = dto.DoctorName,
                DoctorRegistrationNumber = dto.DoctorRegistrationNumber,
                PrescriptionDate = dto.PrescriptionDate,
                QuantityDispensed = dto.QuantityDispensed,
                Notes = dto.Notes,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<PrescriptionRecord>().Add(record);
            batch.CurrentQuantity -= dto.QuantityDispensed;
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<IReadOnlyList<DrugBatchDto>> ListExpiringAsync(int days, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(days);
        var list = await _db.Set<DrugBatch>()
            .Where(b => b.ExpiryDate <= cutoff && b.IsActive && b.CurrentQuantity > 0)
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    private static DrugBatchDto Map(DrugBatch b) => new(
        b.Id, b.BatchNumber, b.ProductId, b.ProductNameSnapshot,
        b.GenericName, b.Manufacturer, b.Schedule,
        b.ManufactureDate, b.ExpiryDate,
        b.InitialQuantity, b.CurrentQuantity,
        b.PurchasePrice, b.SellingPrice, b.IsActive,
        (int)(b.ExpiryDate - DateTime.UtcNow).TotalDays);
}
