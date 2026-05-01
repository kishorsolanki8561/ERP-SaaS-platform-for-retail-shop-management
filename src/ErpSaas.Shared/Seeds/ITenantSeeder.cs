namespace ErpSaas.Shared.Seeds;

/// <summary>
/// Runs once per shop during onboarding to seed shop-scoped default data
/// (e.g. Chart of Accounts, default WalletConfig).
/// Contrast with <see cref="IDataSeeder"/> which runs on every deploy.
/// </summary>
public interface ITenantSeeder
{
    int Order { get; }
    Task SeedAsync(long shopId, CancellationToken ct = default);
}
