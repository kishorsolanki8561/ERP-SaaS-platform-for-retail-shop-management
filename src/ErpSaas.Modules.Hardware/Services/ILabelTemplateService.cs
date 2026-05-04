using ErpSaas.Modules.Hardware.Enums;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Hardware.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record LabelTemplateDto(
    long Id,
    string Name,
    LabelType LabelType,
    int PageWidthMm,
    int PageHeightMm,
    string ZplTemplate,
    string? TsplTemplate,
    bool IsDefault,
    bool IsActive);

public record CreateLabelTemplateDto(
    string Name,
    LabelType LabelType,
    int PageWidthMm,
    int PageHeightMm,
    string ZplTemplate,
    string? TsplTemplate,
    bool IsDefault = false);

public record UpdateLabelTemplateDto(
    string? Name,
    string? ZplTemplate,
    string? TsplTemplate,
    bool? IsDefault,
    bool? IsActive);

public record PrintLabelRequest(
    long ProductId,
    string ProductName,
    string Barcode,
    decimal Price,
    int Copies = 1,
    long? LabelTemplateId = null);

public record PrintLabelResponse(
    string ZplContent,
    string? TsplContent,
    int Copies,
    int PageWidthMm,
    int PageHeightMm);

// ── Interface ─────────────────────────────────────────────────────────────────

public interface ILabelTemplateService
{
    Task<Result<long>> CreateAsync(CreateLabelTemplateDto dto, CancellationToken ct = default);

    Task<Result<bool>> UpdateAsync(long id, UpdateLabelTemplateDto dto, CancellationToken ct = default);

    Task<Result<bool>> DeleteAsync(long id, CancellationToken ct = default);

    Task<LabelTemplateDto?> GetByIdAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<LabelTemplateDto>> ListAsync(LabelType? labelType = null, CancellationToken ct = default);

    Task<Result<PrintLabelResponse>> RenderAsync(PrintLabelRequest request, CancellationToken ct = default);
}
