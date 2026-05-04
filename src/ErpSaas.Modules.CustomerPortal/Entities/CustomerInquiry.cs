using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.CustomerPortal.Entities;

[Auditable("CustomerPortal.CustomerInquiry")]
public sealed class CustomerInquiry : TenantEntity
{
    public string InquiryNumber { get; set; } = default!;
    public long PlatformCustomerId { get; set; }
    public long? TenantCustomerId { get; set; }
    public CustomerInquiryType Type { get; set; }
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
    public InquiryStatus Status { get; set; } = InquiryStatus.Open;
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public long? AssignedToUserId { get; set; }

    public ICollection<CustomerInquiryMessage> Messages { get; set; } = [];
}
