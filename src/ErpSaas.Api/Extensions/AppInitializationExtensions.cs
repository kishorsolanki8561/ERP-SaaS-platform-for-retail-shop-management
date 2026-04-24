using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Seeds;
using ErpSaas.Infrastructure.Sql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Api.Extensions;

public static class AppInitializationExtensions
{
    public static async Task InitializeAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<WebApplication>>();

        try
        {
            await sp.GetRequiredService<ISqlObjectMigrator>().DeployAsync();

            await sp.GetRequiredService<PlatformDbContext>().Database.EnsureCreatedAsync();
            await sp.GetRequiredService<TenantDbContext>().Database.EnsureCreatedAsync();
            await sp.GetRequiredService<LogDbContext>().Database.EnsureCreatedAsync();

            await sp.GetRequiredService<DatabaseSeeder>().SeedAllAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup initialization failed — continuing in dev mode");
        }
    }
}
