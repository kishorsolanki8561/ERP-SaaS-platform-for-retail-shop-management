using ErpSaas.Shared.Seeds;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Infrastructure.Seeds;

public sealed class DatabaseSeeder(
    IEnumerable<IDataSeeder> seeders,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAllAsync(CancellationToken ct = default)
    {
        foreach (var seeder in seeders.OrderBy(s => s.Order))
        {
            logger.LogInformation("Running seeder {Seeder}", seeder.GetType().Name);
            await seeder.SeedAsync(ct);
        }
    }
}
