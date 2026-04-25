using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Messaging;
using ErpSaas.Infrastructure.Seeds;
using ErpSaas.Infrastructure.Sql;
using Hangfire;
using Microsoft.EntityFrameworkCore;
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

            await sp.GetRequiredService<PlatformDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<TenantDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<AnalyticsDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<LogDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();

            await sp.GetRequiredService<DatabaseSeeder>().SeedAllAsync();

            RecurringJob.AddOrUpdate<NotificationDrainJob>(
                "notification-drain",
                job => job.ExecuteAsync(CancellationToken.None),
                Cron.Minutely);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup initialization failed — continuing in dev mode");
        }
    }
}
