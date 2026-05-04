using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Marketing;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Services;

public sealed class LeadService(
    PlatformDbContext db,
    IErrorLogger errorLogger,
    ILogger<LeadService> logger)
    : BaseService<PlatformDbContext>(db, errorLogger), ILeadService
{
    public async Task<Result<long>> SubmitAsync(SubmitLeadDto dto, CancellationToken ct = default)
        => await ExecuteAsync<long>("Lead.Submit", async () =>
        {
            if (!Enum.TryParse<LeadSource>(dto.Source, ignoreCase: true, out var source))
                source = LeadSource.Website;

            var lead = new Lead
            {
                Name             = dto.Name,
                Email            = dto.Email,
                Phone            = dto.Phone,
                BusinessName     = dto.BusinessName,
                Message          = dto.Message,
                CityCode         = dto.CityCode,
                StateCode        = dto.StateCode,
                VerticalCode     = dto.VerticalCode,
                ShopsCount       = dto.ShopsCount,
                Source           = source,
                Status           = LeadStatus.New,
                UtmSource        = dto.UtmSource,
                UtmCampaign      = dto.UtmCampaign,
                CreatedAtUtc     = DateTime.UtcNow,
            };

            _db.Leads.Add(lead);
            await _db.SaveChangesAsync(ct);

            logger.LogInformation("Lead submitted: {Email} from {Source}", dto.Email, source);

            return Result<long>.Success(lead.Id);
        }, ct);

    public async Task<(IReadOnlyList<LeadSummaryDto> Items, int TotalCount)> ListAsync(
        int page, int pageSize, LeadStatus? status, CancellationToken ct = default)
    {
        var query = _db.Leads.AsNoTracking();

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        var total = await query.CountAsync(ct);

        var leads = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id, l.Name, l.Email, l.Phone, l.BusinessName,
                l.VerticalCode, l.Source, l.Status,
                l.AssignedUserId, l.LastContactedAtUtc, l.CreatedAtUtc
            })
            .ToListAsync(ct);

        // Resolve assigned user names in one round-trip
        var userIds = leads.Where(l => l.AssignedUserId.HasValue)
                           .Select(l => l.AssignedUserId!.Value).Distinct().ToList();

        var userNames = userIds.Any()
            ? await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct)
            : new Dictionary<long, string>();

        var items = leads.Select(l => new LeadSummaryDto(
            l.Id, l.Name, l.Email, l.Phone, l.BusinessName,
            l.VerticalCode, l.Source.ToString(), l.Status.ToString(),
            l.AssignedUserId,
            l.AssignedUserId.HasValue ? userNames.GetValueOrDefault(l.AssignedUserId.Value) : null,
            l.LastContactedAtUtc, l.CreatedAtUtc)).ToList();

        return (items, total);
    }

    public async Task<LeadDetailDto?> GetAsync(long id, CancellationToken ct = default)
    {
        var lead = await _db.Leads.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        if (lead is null) return null;

        return MapDetail(lead);
    }

    public async Task<Result<bool>> AssignAsync(long leadId, long userId, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Lead.Assign", async () =>
        {
            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == leadId, ct);
            if (lead is null) return Result<bool>.NotFound(Errors.Lead.NotFound);

            var userExists = await _db.Users.AnyAsync(u => u.Id == userId && u.IsActive, ct);
            if (!userExists) return Result<bool>.NotFound(Errors.Lead.UserNotFound);

            lead.AssignedUserId = userId;
            await _db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct);

    public async Task<Result<bool>> UpdateStatusAsync(long leadId, UpdateLeadStatusDto dto, CancellationToken ct = default)
        => await ExecuteAsync<bool>("Lead.UpdateStatus", async () =>
        {
            if (!Enum.TryParse<LeadStatus>(dto.Status, ignoreCase: true, out var newStatus))
                return Result<bool>.Failure(Errors.Lead.InvalidStatus);

            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == leadId, ct);
            if (lead is null) return Result<bool>.NotFound(Errors.Lead.NotFound);

            if (newStatus == LeadStatus.Converted && !lead.ConvertedShopId.HasValue)
                return Result<bool>.Conflict(Errors.Lead.CannotMarkConvertedWithoutShop);

            lead.Status = newStatus;
            if (!string.IsNullOrWhiteSpace(dto.Notes))
                lead.Notes = dto.Notes;

            if (newStatus == LeadStatus.Contacted)
                lead.LastContactedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }, ct);

    public async Task<Result<long>> ConvertAsync(long leadId, CancellationToken ct = default)
        => await ExecuteAsync<long>("Lead.Convert", async () =>
        {
            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == leadId, ct);
            if (lead is null) return Result<long>.NotFound(Errors.Lead.NotFound);

            if (lead.Status == LeadStatus.Converted)
                return Result<long>.Conflict(Errors.Lead.AlreadyConverted);

            // Create a minimal Shop stub — full onboarding done separately
            var shopCode = $"SHOP-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var shop = new Shop
            {
                ShopCode     = shopCode,
                LegalName    = lead.BusinessName ?? lead.Name,
                CurrencyCode = "INR",
                TimeZone     = "Asia/Kolkata",
                IsActive     = false,
                CreatedAtUtc = DateTime.UtcNow,
            };

            _db.Shops.Add(shop);
            await _db.SaveChangesAsync(ct);

            lead.Status          = LeadStatus.Converted;
            lead.ConvertedShopId = shop.Id;
            await _db.SaveChangesAsync(ct);

            logger.LogInformation("Lead {LeadId} converted to shop {ShopId}", leadId, shop.Id);

            return Result<long>.Success(shop.Id);
        }, ct, useTransaction: true);

    // ── Private helpers ───────────────────────────────────────────────────────

    private static LeadDetailDto MapDetail(Lead l) => new(
        l.Id, l.Name, l.Email, l.Phone, l.BusinessName, l.Message, l.Notes,
        l.CityCode, l.StateCode, l.VerticalCode, l.ShopsCount,
        l.Source.ToString(), l.Status.ToString(),
        l.UtmSource, l.UtmCampaign, l.ConvertedShopId,
        l.AssignedUserId, l.LastContactedAtUtc, l.CreatedAtUtc);
}
