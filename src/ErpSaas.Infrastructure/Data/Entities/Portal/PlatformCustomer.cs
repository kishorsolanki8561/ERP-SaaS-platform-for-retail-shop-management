namespace ErpSaas.Infrastructure.Data.Entities.Portal;

public sealed class PlatformCustomer
{
    public long Id { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    public ICollection<CustomerLink> CustomerLinks { get; set; } = [];
    public ICollection<CustomerLoginSession> LoginSessions { get; set; } = [];
}
