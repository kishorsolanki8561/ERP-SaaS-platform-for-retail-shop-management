using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hardware.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record ReceiptTemplateDto(
    long Id,
    string Name,
    string TemplateType,
    string HeaderJson,
    string FooterJson,
    bool IsDefault,
    bool IsActive);

public record CreateReceiptTemplateDto(
    string Name,
    string TemplateType,
    string HeaderJson,
    string FooterJson,
    bool IsDefault = false);

public record UpdateReceiptTemplateDto(
    string? Name,
    string? HeaderJson,
    string? FooterJson,
    bool? IsDefault,
    bool? IsActive);

public record ReceiptLineItem(
    string Name,
    decimal Qty,
    string Unit,
    decimal Rate,
    decimal Amount);

public record PrintReceiptRequest(
    string ShopName,
    string? ShopAddress,
    string? Gstin,
    string InvoiceNumber,
    DateTime InvoiceDate,
    string CustomerName,
    IReadOnlyList<ReceiptLineItem> Items,
    decimal SubTotal,
    decimal TaxAmount,
    decimal Total,
    string? PaymentMode,
    long? ReceiptTemplateId = null);

public record PrintReceiptResponse(
    string EscPosBase64,
    string TemplateType);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface IReceiptTemplateService
{
    Task<Result<long>> CreateAsync(CreateReceiptTemplateDto dto, CancellationToken ct = default);

    Task<Result<bool>> UpdateAsync(long id, UpdateReceiptTemplateDto dto, CancellationToken ct = default);

    Task<Result<bool>> DeleteAsync(long id, CancellationToken ct = default);

    Task<ReceiptTemplateDto?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<ReceiptTemplateDto>> ListAsync(CancellationToken ct = default);

    Task<Result<PrintReceiptResponse>> RenderAsync(PrintReceiptRequest request, CancellationToken ct = default);
}
