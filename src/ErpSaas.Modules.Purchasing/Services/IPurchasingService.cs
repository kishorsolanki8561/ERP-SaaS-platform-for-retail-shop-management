using ErpSaas.Modules.Purchasing.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Purchasing.Services;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record SupplierDto(long Id, string Name, string? Code, string? GstNumber, string? Phone, string? Email, bool IsActive);

public record CreateSupplierDto(
    string Name, string? Code, string? GstNumber, string? PanNumber,
    string? Phone, string? Email, string? Address, string? City, string? State, string? Pincode,
    decimal OpeningBalance, string? Notes);

public record UpdateSupplierDto(
    string Name, string? GstNumber, string? PanNumber,
    string? Phone, string? Email, string? Address, string? City, string? State, string? Pincode,
    bool IsActive, string? Notes);

public record PurchaseOrderLineDto(
    long ProductId, long ProductUnitId, decimal QuantityInBilledUnit,
    decimal UnitPrice, decimal DiscountPercent, decimal GstRate);

public record CreatePurchaseOrderDto(
    long SupplierId, DateTime OrderDate, DateTime? ExpectedDeliveryDate,
    string? Notes, long? BranchId,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public record ReceivePoDto(long PurchaseOrderId, IReadOnlyList<PoReceiveLine> Lines);
public record PoReceiveLine(long LineId, decimal QuantityReceived);

public record CreateBillDto(
    long SupplierId, string? SupplierBillNumber, long? PurchaseOrderId,
    DateTime BillDate, DateTime? DueDate, string? Notes,
    decimal SubTotal, decimal TotalTaxAmount, decimal GrandTotal);

public record PayBillDto(
    long BillId, DateTime PaymentDate, decimal Amount,
    string PaymentModeCode, string? ReferenceNumber, string? Notes);

public record PurchaseReturnLineDto(
    long ProductId, long ProductUnitId, decimal QuantityInBilledUnit,
    decimal UnitPrice, decimal GstRate);

public record CreatePurchaseReturnDto(
    long SupplierId, long? PurchaseOrderId, DateTime ReturnDate,
    string? Reason, string? Notes, long? BranchId,
    IReadOnlyList<PurchaseReturnLineDto> Lines);

public record IssueDebitNoteDto(
    long PurchaseReturnId, DateTime IssueDate, DateTime? ExpiryDate, string? Notes);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IPurchasingService
{
    // Suppliers
    Task<IReadOnlyList<SupplierDto>> ListSuppliersAsync(CancellationToken ct = default);
    Task<Result<long>> CreateSupplierAsync(CreateSupplierDto dto, CancellationToken ct = default);
    Task<Result<bool>> UpdateSupplierAsync(long id, UpdateSupplierDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteSupplierAsync(long id, CancellationToken ct = default);

    // Purchase Orders
    Task<Result<long>> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, CancellationToken ct = default);
    Task<Result<bool>> SendPurchaseOrderAsync(long id, CancellationToken ct = default);
    Task<Result<bool>> ReceivePurchaseOrderAsync(ReceivePoDto dto, CancellationToken ct = default);
    Task<Result<bool>> CancelPurchaseOrderAsync(long id, CancellationToken ct = default);

    // Bills
    Task<Result<long>> CreateBillAsync(CreateBillDto dto, CancellationToken ct = default);
    Task<Result<bool>> ApproveBillAsync(long id, CancellationToken ct = default);
    Task<Result<bool>> PayBillAsync(PayBillDto dto, CancellationToken ct = default);
    Task<Result<bool>> CancelBillAsync(long id, CancellationToken ct = default);

    // Purchase Returns
    Task<Result<long>> CreatePurchaseReturnAsync(CreatePurchaseReturnDto dto, CancellationToken ct = default);
    Task<Result<bool>> ApprovePurchaseReturnAsync(long id, CancellationToken ct = default);
    Task<Result<bool>> CancelPurchaseReturnAsync(long id, CancellationToken ct = default);
    Task<Result<long>> IssueDebitNoteAsync(IssueDebitNoteDto dto, CancellationToken ct = default);
}
