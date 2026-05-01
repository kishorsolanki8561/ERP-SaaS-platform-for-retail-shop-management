using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Modules.Warranty.Entities;
using ErpSaas.Modules.Warranty.Enums;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
namespace ErpSaas.Modules.Warranty.Services;

public sealed class WarrantyService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant)
    : BaseService<TenantDbContext>(db, errorLogger), IWarrantyService
{
    public async Task<Result<long>> RegisterWarrantyAsync(RegisterWarrantyDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Warranty.RegisterWarranty", async () =>
        {
            var exists = await _db.Set<WarrantyRegistration>()
                .AnyAsync(w => w.SerialNumber == dto.SerialNumber, ct);
            if (exists) return Result<long>.Conflict(Errors.Warranty.SerialNumberExists);

            var reg = new WarrantyRegistration
            {
                ShopId = tenant.ShopId,
                InvoiceId = dto.InvoiceId,
                InvoiceLineId = dto.InvoiceLineId,
                ProductId = dto.ProductId,
                ProductNameSnapshot = "—",
                CustomerId = dto.CustomerId,
                CustomerNameSnapshot = "—",
                SerialNumber = dto.SerialNumber,
                PurchaseDate = dto.PurchaseDate,
                WarrantyStartDate = dto.PurchaseDate,
                WarrantyEndDate = dto.PurchaseDate.AddMonths(dto.WarrantyMonths),
                WarrantyMonths = dto.WarrantyMonths,
                Type = dto.Type,
                StatusCode = "Active",
                TermsSnapshot = dto.Terms,
                BranchId = dto.BranchId,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<WarrantyRegistration>().Add(reg);
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(reg.Id);
        }, ct, useTransaction: true);
    }

    public async Task<WarrantyRegistrationDto?> GetBySerialAsync(string serial, CancellationToken ct = default)
    {
        var reg = await _db.Set<WarrantyRegistration>()
            .FirstOrDefaultAsync(w => w.SerialNumber == serial, ct);
        if (reg is null) return null;
        return Map(reg);
    }

    public async Task<IReadOnlyList<WarrantyRegistrationDto>> ListExpiringAsync(int days, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(days);
        return await _db.Set<WarrantyRegistration>()
            .Where(w => w.WarrantyEndDate <= cutoff && w.StatusCode == "Active")
            .Select(w => new WarrantyRegistrationDto(
                w.Id, w.SerialNumber, w.ProductNameSnapshot, w.CustomerNameSnapshot,
                w.PurchaseDate, w.WarrantyEndDate, w.StatusCode, w.Type))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WarrantyRegistrationDto>> ListByCustomerAsync(long customerId, CancellationToken ct = default)
    {
        return await _db.Set<WarrantyRegistration>()
            .Where(w => w.CustomerId == customerId)
            .Select(w => new WarrantyRegistrationDto(
                w.Id, w.SerialNumber, w.ProductNameSnapshot, w.CustomerNameSnapshot,
                w.PurchaseDate, w.WarrantyEndDate, w.StatusCode, w.Type))
            .ToListAsync(ct);
    }

    public async Task<Result<long>> CreateClaimAsync(CreateClaimDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Warranty.CreateClaim", async () =>
        {
            var reg = await _db.Set<WarrantyRegistration>()
                .FirstOrDefaultAsync(w => w.Id == dto.WarrantyRegistrationId, ct);
            if (reg is null) return Result<long>.NotFound(Errors.Warranty.RegistrationNotFound);
            if (reg.StatusCode != "Active") return Result<long>.Conflict(Errors.Warranty.WarrantyNotActive);
            if (reg.WarrantyEndDate < dto.ClaimDate.Date) return Result<long>.Conflict(Errors.Warranty.WarrantyExpired);

            var claimNumber = await sequence.NextAsync(Constants.SequenceCodes.WarrantyClaim, tenant.ShopId, ct);

            var claim = new WarrantyClaim
            {
                ShopId = tenant.ShopId,
                WarrantyRegistrationId = dto.WarrantyRegistrationId,
                ClaimNumber = claimNumber,
                ClaimDate = dto.ClaimDate,
                IssueDescription = dto.IssueDescription,
                Status = ClaimStatus.Open,
                AttachmentFileIds = dto.AttachmentFileIds,
                CreatedAtUtc = DateTime.UtcNow,
            };
            _db.Set<WarrantyClaim>().Add(claim);

            reg.StatusCode = "Claimed";
            reg.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<long>.Success(claim.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ResolveClaimAsync(long claimId, ResolveClaimDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync("Warranty.ResolveClaim", async () =>
        {
            var claim = await _db.Set<WarrantyClaim>()
                .Include(c => c.Registration)
                .FirstOrDefaultAsync(c => c.Id == claimId, ct);
            if (claim is null) return Result<bool>.NotFound(Errors.Warranty.ClaimNotFound);
            if (claim.Status == ClaimStatus.Resolved || claim.Status == ClaimStatus.Rejected)
                return Result<bool>.Conflict(Errors.Warranty.ClaimAlreadyClosed);

            claim.Status = dto.Status;
            claim.ResolutionNotes = dto.ResolutionNotes;
            claim.RepairCost = dto.RepairCost;
            claim.ResolvedDate = DateTime.UtcNow;
            claim.UpdatedAtUtc = DateTime.UtcNow;

            if (dto.Status == ClaimStatus.Resolved)
            {
                claim.Registration.StatusCode = "Active";
                claim.Registration.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<IReadOnlyList<WarrantyClaimDto>> ListClaimsAsync(CancellationToken ct = default)
    {
        return await _db.Set<WarrantyClaim>()
            .Select(c => new WarrantyClaimDto(
                c.Id, c.ClaimNumber, c.WarrantyRegistrationId,
                c.ClaimDate, c.IssueDescription, c.Status,
                c.ResolutionNotes, c.RepairCost, c.ResolvedDate))
            .ToListAsync(ct);
    }

    private static WarrantyRegistrationDto Map(WarrantyRegistration w) =>
        new(w.Id, w.SerialNumber, w.ProductNameSnapshot, w.CustomerNameSnapshot,
            w.PurchaseDate, w.WarrantyEndDate, w.StatusCode, w.Type);
}
