using ErpSaas.Shared.Data;

namespace ErpSaas.Infrastructure.Data.Entities.Identity;

public class ShopRegistrationRequest : BaseEntity
{
    public string ShopCode              { get; set; } = "";
    public string LegalName             { get; set; } = "";
    public string? TradeName            { get; set; }
    public string? GstNumber            { get; set; }
    public string AdminEmail            { get; set; } = "";
    public string AdminDisplayName      { get; set; } = "";
    public string PasswordHashSnapshot  { get; set; } = "";
    public string? ContactPhone         { get; set; }
    public string? Notes                { get; set; }
    public RegistrationStatus Status    { get; set; } = RegistrationStatus.Pending;
    public long?  ReviewedByUserId      { get; set; }
    public DateTime? ReviewedAtUtc      { get; set; }
    public string? RejectionReason      { get; set; }
}
