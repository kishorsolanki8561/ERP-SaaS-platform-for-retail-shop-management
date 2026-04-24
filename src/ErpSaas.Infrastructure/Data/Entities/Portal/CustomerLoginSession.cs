namespace ErpSaas.Infrastructure.Data.Entities.Portal;

public sealed class CustomerLoginSession
{
    public long Id { get; set; }
    public long PlatformCustomerId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string Purpose { get; set; } = "OtpChallenge";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public PlatformCustomer PlatformCustomer { get; set; } = null!;
}
