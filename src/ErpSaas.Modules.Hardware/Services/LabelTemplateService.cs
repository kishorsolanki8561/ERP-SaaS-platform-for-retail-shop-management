using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Hardware.Entities;
using ErpSaas.Modules.Hardware.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Hardware.Services;

public sealed class LabelTemplateService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), ILabelTemplateService
{
    public async Task<Result<long>> CreateAsync(CreateLabelTemplateDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Hardware.LabelTemplate.Create", async () =>
        {
            if (dto.IsDefault)
                await ClearDefaultAsync(dto.LabelType, ct);

            var entity = new LabelTemplate
            {
                ShopId = tenant.ShopId,
                Name = dto.Name,
                LabelType = dto.LabelType,
                PageWidthMm = dto.PageWidthMm,
                PageHeightMm = dto.PageHeightMm,
                ZplTemplate = dto.ZplTemplate,
                TsplTemplate = dto.TsplTemplate,
                IsDefault = dto.IsDefault,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<LabelTemplate>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> UpdateAsync(long id, UpdateLabelTemplateDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Hardware.LabelTemplate.Update", async () =>
        {
            var entity = await _db.Set<LabelTemplate>().FirstOrDefaultAsync(t => t.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Hardware.LabelTemplateNotFound);

            if (dto.IsDefault == true)
                await ClearDefaultAsync(entity.LabelType, ct);

            if (dto.Name is not null) entity.Name = dto.Name;
            if (dto.ZplTemplate is not null) entity.ZplTemplate = dto.ZplTemplate;
            if (dto.TsplTemplate is not null) entity.TsplTemplate = dto.TsplTemplate;
            if (dto.IsDefault.HasValue) entity.IsDefault = dto.IsDefault.Value;
            if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> DeleteAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Hardware.LabelTemplate.Delete", async () =>
        {
            var entity = await _db.Set<LabelTemplate>().FirstOrDefaultAsync(t => t.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Hardware.LabelTemplateNotFound);

            _db.Set<LabelTemplate>().Remove(entity);
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    public async Task<LabelTemplateDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var e = await _db.Set<LabelTemplate>().FirstOrDefaultAsync(t => t.Id == id, ct);
        return e is null ? null : Map(e);
    }

    public async Task<IReadOnlyList<LabelTemplateDto>> ListAsync(LabelType? labelType = null, CancellationToken ct = default)
    {
        var q = _db.Set<LabelTemplate>().AsQueryable();
        if (labelType.HasValue) q = q.Where(t => t.LabelType == labelType.Value);
        return await q.OrderBy(t => t.Name).Select(t => Map(t)).ToListAsync(ct);
    }

    public async Task<Result<PrintLabelResponse>> RenderAsync(PrintLabelRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync("Hardware.LabelTemplate.Render", async () =>
        {
            LabelTemplate? template;
            if (request.LabelTemplateId.HasValue)
            {
                template = await _db.Set<LabelTemplate>()
                    .FirstOrDefaultAsync(t => t.Id == request.LabelTemplateId.Value, ct);
                if (template is null)
                    return Result<PrintLabelResponse>.NotFound(Errors.Hardware.LabelTemplateNotFound);
            }
            else
            {
                template = await _db.Set<LabelTemplate>()
                    .Where(t => t.LabelType == LabelType.ProductTag && t.IsDefault && t.IsActive)
                    .FirstOrDefaultAsync(ct)
                    ?? BuildDefaultTemplate();
            }

            var values = new Dictionary<string, string>
            {
                ["productName"] = request.ProductName,
                ["barcode"]     = request.Barcode,
                ["price"]       = request.Price.ToString("F2"),
                ["date"]        = DateTime.UtcNow.ToString("dd-MM-yyyy"),
                ["productId"]   = request.ProductId.ToString(),
            };

            var zpl  = ZplRenderer.Render(template.ZplTemplate, values);
            var tspl = template.TsplTemplate is not null
                ? ZplRenderer.Render(template.TsplTemplate, values)
                : null;

            return Result<PrintLabelResponse>.Success(new PrintLabelResponse(
                zpl, tspl, request.Copies, template.PageWidthMm, template.PageHeightMm));
        }, ct, useTransaction: false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task ClearDefaultAsync(LabelType labelType, CancellationToken ct)
    {
        var existing = await _db.Set<LabelTemplate>()
            .Where(t => t.LabelType == labelType && t.IsDefault)
            .ToListAsync(ct);
        foreach (var t in existing) t.IsDefault = false;
    }

    private static LabelTemplate BuildDefaultTemplate() => new()
    {
        LabelType = LabelType.ProductTag,
        PageWidthMm = 40,
        PageHeightMm = 25,
        ZplTemplate =
            "^XA\n" +
            "^CF0,22\n" +
            "^FO20,10^FD{{productName}}^FS\n" +
            "^FO20,38^BY2^BCN,55,Y,N,N^FD{{barcode}}^FS\n" +
            "^FO20,110^CF0,20^FDRs {{price}}^FS\n" +
            "^FO20,135^CF0,16^FD{{date}}^FS\n" +
            "^XZ",
    };

    private static LabelTemplateDto Map(LabelTemplate e) => new(
        e.Id, e.Name, e.LabelType, e.PageWidthMm, e.PageHeightMm,
        e.ZplTemplate, e.TsplTemplate, e.IsDefault, e.IsActive);
}

/// <summary>
/// Substitutes {{key}} placeholders in ZPL / TSPL templates.
/// </summary>
internal static class ZplRenderer
{
    public static string Render(string template, Dictionary<string, string> values)
        => values.Aggregate(template, (current, kv) =>
            current.Replace($"{{{{{kv.Key}}}}}", kv.Value));
}
