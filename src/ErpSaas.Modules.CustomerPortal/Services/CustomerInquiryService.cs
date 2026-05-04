using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.CustomerPortal.Entities;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Messages;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.CustomerPortal.Services;

#pragma warning disable CS9107, CS9113
public sealed class CustomerInquiryService(
    TenantDbContext db,
    IErrorLogger errorLogger,
    ISequenceService sequence,
    ITenantContext tenant,
    ILogger<CustomerInquiryService> logger)
    : BaseService<TenantDbContext>(db, errorLogger), ICustomerInquiryService
{
    public async Task<PagedResult<InquirySummaryDto>> ListInquiriesAsync(int page, int pageSize, InquiryStatus? status, CancellationToken ct = default)
    {
        var query = db.Set<CustomerInquiry>().Where(i => i.ShopId == tenant.ShopId);
        if (status.HasValue) query = query.Where(i => i.Status == status.Value);

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(i => i.OpenedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new InquirySummaryDto(i.Id, i.InquiryNumber, i.Subject, i.Status, i.Type, i.OpenedAtUtc))
            .ToListAsync(ct);

        return new PagedResult<InquirySummaryDto>(rows, total, page, pageSize);
    }

    public async Task<Result<InquiryDetailDto?>> GetInquiryAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Inquiry.Get", async () =>
        {
            var inquiry = await db.Set<CustomerInquiry>()
                .Include(i => i.Messages)
                .FirstOrDefaultAsync(i => i.Id == id, ct);

            if (inquiry is null) return Result<InquiryDetailDto?>.Success(null);

            var messages = inquiry.Messages
                .OrderBy(m => m.SentAtUtc)
                .Select(m => new InquiryMessageDto(m.Id, m.IsFromCustomer, m.Body, m.SentAtUtc))
                .ToList();

            return Result<InquiryDetailDto?>.Success(new InquiryDetailDto(
                inquiry.Id, inquiry.InquiryNumber, inquiry.Subject, inquiry.Body,
                inquiry.Type, inquiry.Status, inquiry.OpenedAtUtc, inquiry.ClosedAtUtc, messages));
        }, ct);
    }

    public async Task<Result<long>> CreateInquiryAsync(CreateInquiryDto dto, long platformCustomerId, CancellationToken ct = default)
    {
        return await ExecuteAsync("Inquiry.Create", async () =>
        {
            var inquiryNumber = await sequence.NextAsync(Constants.SequenceCodes.CustomerInquiry, tenant.ShopId, ct);

            var inquiry = new CustomerInquiry
            {
                InquiryNumber = inquiryNumber,
                PlatformCustomerId = platformCustomerId,
                TenantCustomerId = dto.TenantCustomerId,
                Type = dto.Type,
                Subject = dto.Subject,
                Body = dto.Body,
                Status = InquiryStatus.Open,
                OpenedAtUtc = DateTime.UtcNow,
                ShopId = tenant.ShopId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            db.Set<CustomerInquiry>().Add(inquiry);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(inquiry.Id);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> ReplyAsync(long id, ReplyInquiryDto dto, long authorId, bool isFromCustomer, CancellationToken ct = default)
    {
        return await ExecuteAsync("Inquiry.Reply", async () =>
        {
            var inquiry = await db.Set<CustomerInquiry>().FindAsync([id], ct);
            if (inquiry is null) return Result<bool>.NotFound(Errors.CustomerPortal.InquiryNotFound);
            if (inquiry.Status == InquiryStatus.Closed) return Result<bool>.Conflict(Errors.CustomerPortal.InquiryAlreadyClosed);

            if (inquiry.Status == InquiryStatus.Open && !isFromCustomer)
                inquiry.Status = InquiryStatus.InProgress;

            db.Set<CustomerInquiryMessage>().Add(new CustomerInquiryMessage
            {
                InquiryId = id,
                IsFromCustomer = isFromCustomer,
                AuthorId = authorId,
                Body = dto.Body,
                AttachmentFileIdsCsv = dto.AttachmentFileIdsCsv,
                SentAtUtc = DateTime.UtcNow,
                ShopId = tenant.ShopId,
                CreatedAtUtc = DateTime.UtcNow,
            });

            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> CloseInquiryAsync(long id, CancellationToken ct = default)
    {
        return await ExecuteAsync("Inquiry.Close", async () =>
        {
            var inquiry = await db.Set<CustomerInquiry>().FindAsync([id], ct);
            if (inquiry is null) return Result<bool>.NotFound(Errors.CustomerPortal.InquiryNotFound);
            if (inquiry.Status == InquiryStatus.Closed) return Result<bool>.Conflict(Errors.CustomerPortal.InquiryAlreadyClosed);

            inquiry.Status = InquiryStatus.Closed;
            inquiry.ClosedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }

    public async Task<Result<bool>> AssignInquiryAsync(long id, long staffUserId, CancellationToken ct = default)
    {
        return await ExecuteAsync("Inquiry.Assign", async () =>
        {
            var inquiry = await db.Set<CustomerInquiry>().FindAsync([id], ct);
            if (inquiry is null) return Result<bool>.NotFound(Errors.CustomerPortal.InquiryNotFound);

            inquiry.AssignedToUserId = staffUserId;
            await db.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }, ct, useTransaction: true);
    }
}
