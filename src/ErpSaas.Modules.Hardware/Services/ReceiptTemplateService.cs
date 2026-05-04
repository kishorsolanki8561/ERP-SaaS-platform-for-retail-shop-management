using System.Text;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Hardware.Entities;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Hardware.Services;

public sealed class ReceiptTemplateService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IReceiptTemplateService
{
    public async Task<Result<long>> CreateAsync(CreateReceiptTemplateDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Hardware.ReceiptTemplate.Create", async () =>
        {
            if (dto.IsDefault)
                await ClearDefaultAsync(dto.TemplateType, ct);

            var entity = new ReceiptTemplate
            {
                ShopId = tenant.ShopId,
                Name = dto.Name,
                TemplateType = dto.TemplateType,
                HeaderJson = dto.HeaderJson,
                FooterJson = dto.FooterJson,
                IsDefault = dto.IsDefault,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<ReceiptTemplate>().Add(entity);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(entity.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> UpdateAsync(long id, UpdateReceiptTemplateDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Hardware.ReceiptTemplate.Update", async () =>
        {
            var entity = await _db.Set<ReceiptTemplate>().FirstOrDefaultAsync(t => t.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Hardware.ReceiptTemplateNotFound);

            if (dto.IsDefault == true)
                await ClearDefaultAsync(entity.TemplateType, ct);

            if (dto.Name is not null) entity.Name = dto.Name;
            if (dto.HeaderJson is not null) entity.HeaderJson = dto.HeaderJson;
            if (dto.FooterJson is not null) entity.FooterJson = dto.FooterJson;
            if (dto.IsDefault.HasValue) entity.IsDefault = dto.IsDefault.Value;
            if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    public async Task<Result<bool>> DeleteAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Hardware.ReceiptTemplate.Delete", async () =>
        {
            var entity = await _db.Set<ReceiptTemplate>().FirstOrDefaultAsync(t => t.Id == id, ct);
            if (entity is null) return Result<bool>.NotFound(Errors.Hardware.ReceiptTemplateNotFound);

            _db.Set<ReceiptTemplate>().Remove(entity);
            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: false);
    }

    public async Task<ReceiptTemplateDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var e = await _db.Set<ReceiptTemplate>().FirstOrDefaultAsync(t => t.Id == id, ct);
        return e is null ? null : Map(e);
    }

    public async Task<IReadOnlyList<ReceiptTemplateDto>> ListAsync(CancellationToken ct = default)
        => await _db.Set<ReceiptTemplate>()
            .OrderBy(t => t.Name)
            .Select(t => Map(t))
            .ToListAsync(ct);

    public async Task<Result<PrintReceiptResponse>> RenderAsync(PrintReceiptRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync("Hardware.ReceiptTemplate.Render", async () =>
        {
            ReceiptTemplate? template = null;
            if (request.ReceiptTemplateId.HasValue)
            {
                template = await _db.Set<ReceiptTemplate>()
                    .FirstOrDefaultAsync(t => t.Id == request.ReceiptTemplateId.Value, ct);
                if (template is null)
                    return Result<PrintReceiptResponse>.NotFound(Errors.Hardware.ReceiptTemplateNotFound);
            }

            var bytes = EscPosRenderer.Render(request);
            var base64 = Convert.ToBase64String(bytes);
            var templateType = template?.TemplateType ?? "Retail80mm";

            return Result<PrintReceiptResponse>.Success(new PrintReceiptResponse(base64, templateType));
        }, ct, useTransaction: false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task ClearDefaultAsync(string templateType, CancellationToken ct)
    {
        var existing = await _db.Set<ReceiptTemplate>()
            .Where(t => t.TemplateType == templateType && t.IsDefault)
            .ToListAsync(ct);
        foreach (var t in existing) t.IsDefault = false;
    }

    private static ReceiptTemplateDto Map(ReceiptTemplate e) => new(
        e.Id, e.Name, e.TemplateType, e.HeaderJson, e.FooterJson, e.IsDefault, e.IsActive);
}

/// <summary>
/// Produces ESC/POS byte stream for 80mm thermal printers.
/// Returns bytes ready to be base64-encoded and sent to the client.
/// </summary>
internal static class EscPosRenderer
{
    // ESC/POS command constants
    private const byte ESC = 0x1B;
    private const byte GS  = 0x1D;
    private const byte LF  = 0x0A;

    public static byte[] Render(PrintReceiptRequest r)
    {
        var buf = new List<byte>();

        // Initialize printer
        buf.AddRange([ESC, 0x40]);

        // Center align
        buf.AddRange([ESC, 0x61, 0x01]);

        // Bold shop name
        buf.AddRange([ESC, 0x45, 0x01]);
        buf.AddRange(Line(r.ShopName));
        buf.AddRange([ESC, 0x45, 0x00]);

        if (!string.IsNullOrWhiteSpace(r.ShopAddress))
            buf.AddRange(Line(r.ShopAddress));

        if (!string.IsNullOrWhiteSpace(r.Gstin))
            buf.AddRange(Line($"GSTIN: {r.Gstin}"));

        buf.AddRange(Line(Dashes()));
        buf.AddRange(Line($"Invoice: {r.InvoiceNumber}"));
        buf.AddRange(Line($"Date   : {r.InvoiceDate:dd-MM-yyyy HH:mm}"));
        buf.AddRange(Line($"Customer: {r.CustomerName}"));
        buf.AddRange(Line(Dashes()));

        // Left align for item list
        buf.AddRange([ESC, 0x61, 0x00]);

        foreach (var item in r.Items)
        {
            buf.AddRange(Line(item.Name));
            var detail = $"  {item.Qty} {item.Unit} x {item.Rate:F2} = {item.Amount:F2}";
            buf.AddRange(Line(detail));
        }

        buf.AddRange(Line(Dashes()));

        // Right-align totals
        buf.AddRange([ESC, 0x61, 0x02]);
        buf.AddRange(Line($"Sub-total : {r.SubTotal,10:F2}"));
        buf.AddRange(Line($"Tax       : {r.TaxAmount,10:F2}"));

        buf.AddRange([ESC, 0x45, 0x01]);
        buf.AddRange(Line($"TOTAL     : {r.Total,10:F2}"));
        buf.AddRange([ESC, 0x45, 0x00]);

        if (!string.IsNullOrWhiteSpace(r.PaymentMode))
            buf.AddRange(Line($"Payment   : {r.PaymentMode}"));

        buf.AddRange(Line(Dashes()));

        // Center footer
        buf.AddRange([ESC, 0x61, 0x01]);
        buf.AddRange(Line("Thank you! Visit again."));

        // Feed and cut
        buf.AddRange([LF, LF, LF]);
        buf.AddRange([GS, 0x56, 0x00]);

        return [.. buf];
    }

    private static byte[] Line(string text)
    {
        var encoded = Encoding.ASCII.GetBytes(text.Length > 48 ? text[..48] : text);
        return [.. encoded, LF];
    }

    private static string Dashes() => new('-', 32);
}
