using ErpSaas.Modules.Verticals.Medical.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Verticals.Medical.Services;

public record CreateDrugBatchDto(
    long ProductId,
    string BatchNumber,
    string? GenericName,
    string? Manufacturer,
    DrugSchedule Schedule,
    DateTime ManufactureDate,
    DateTime ExpiryDate,
    decimal InitialQuantity,
    decimal PurchasePrice,
    decimal SellingPrice,
    long? PurchaseBillId);

public record RecordPrescriptionDto(
    long DrugBatchId,
    long InvoiceId,
    long CustomerId,
    string DoctorName,
    string? DoctorRegistrationNumber,
    DateTime PrescriptionDate,
    decimal QuantityDispensed,
    string? Notes);

public record DrugBatchDto(
    long Id,
    string BatchNumber,
    long ProductId,
    string ProductNameSnapshot,
    string? GenericName,
    string? Manufacturer,
    DrugSchedule Schedule,
    DateTime ManufactureDate,
    DateTime ExpiryDate,
    decimal InitialQuantity,
    decimal CurrentQuantity,
    decimal PurchasePrice,
    decimal SellingPrice,
    bool IsActive,
    int DaysToExpiry);

public interface IMedicalInventoryService
{
    Task<Result<long>> CreateBatchAsync(CreateDrugBatchDto dto, CancellationToken ct = default);
    Task<DrugBatchDto?> GetBatchAsync(long batchId, CancellationToken ct = default);
    Task<IReadOnlyList<DrugBatchDto>> ListBatchesAsync(long? productId, bool? expiringWithin30Days, CancellationToken ct = default);
    Task<IReadOnlyList<DrugBatchDto>> ListBatchesByProductAsync(long productId, CancellationToken ct = default);
    Task<Result<bool>> RecordPrescriptionAsync(RecordPrescriptionDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<DrugBatchDto>> ListExpiringAsync(int days, CancellationToken ct = default);
}
