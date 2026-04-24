namespace ErpSaas.Shared.Seeds;

public interface IDataSeeder
{
    int Order { get; }
    Task SeedAsync(CancellationToken ct = default);
}
