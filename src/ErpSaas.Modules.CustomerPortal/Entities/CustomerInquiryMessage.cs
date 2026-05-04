using ErpSaas.Shared.Data;

namespace ErpSaas.Modules.CustomerPortal.Entities;

public sealed class CustomerInquiryMessage : TenantEntity
{
    public long InquiryId { get; set; }
    public bool IsFromCustomer { get; set; }
    public long AuthorId { get; set; }
    public string Body { get; set; } = default!;
    public DateTime SentAtUtc { get; set; }
    public string? AttachmentFileIdsCsv { get; set; }

    public CustomerInquiry Inquiry { get; set; } = null!;
}
