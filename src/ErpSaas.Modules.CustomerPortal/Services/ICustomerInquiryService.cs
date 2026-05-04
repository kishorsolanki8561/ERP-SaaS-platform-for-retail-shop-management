using ErpSaas.Modules.CustomerPortal.Entities;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.CustomerPortal.Services;

public interface ICustomerInquiryService
{
    Task<PagedResult<InquirySummaryDto>> ListInquiriesAsync(int page, int pageSize, InquiryStatus? status, CancellationToken ct = default);
    Task<Result<InquiryDetailDto?>> GetInquiryAsync(long id, CancellationToken ct = default);
    Task<Result<long>> CreateInquiryAsync(CreateInquiryDto dto, long platformCustomerId, CancellationToken ct = default);
    Task<Result<bool>> ReplyAsync(long id, ReplyInquiryDto dto, long authorId, bool isFromCustomer, CancellationToken ct = default);
    Task<Result<bool>> CloseInquiryAsync(long id, CancellationToken ct = default);
    Task<Result<bool>> AssignInquiryAsync(long id, long staffUserId, CancellationToken ct = default);
}

public record InquirySummaryDto(long Id, string InquiryNumber, string Subject, InquiryStatus Status, CustomerInquiryType Type, DateTime OpenedAtUtc);
public record InquiryDetailDto(long Id, string InquiryNumber, string Subject, string Body, CustomerInquiryType Type, InquiryStatus Status, DateTime OpenedAtUtc, DateTime? ClosedAtUtc, IReadOnlyList<InquiryMessageDto> Messages);
public record InquiryMessageDto(long Id, bool IsFromCustomer, string Body, DateTime SentAtUtc);
public record CreateInquiryDto(CustomerInquiryType Type, string Subject, string Body, long? TenantCustomerId);
public record ReplyInquiryDto(string Body, string? AttachmentFileIdsCsv);
