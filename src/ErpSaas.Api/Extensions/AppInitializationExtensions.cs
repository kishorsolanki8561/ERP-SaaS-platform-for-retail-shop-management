using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Messaging;
using ErpSaas.Infrastructure.Metering;
using ErpSaas.Infrastructure.Seeds;
using ErpSaas.Infrastructure.Sql;
using ErpSaas.Modules.Accounting.Jobs;
using ErpSaas.Modules.Marketplace.Jobs;
using ErpSaas.Modules.Payment.Jobs;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            // Migrations must run BEFORE SqlObjectMigrator so the databases exist
            // when DeployAsync() tries to open a connection to them.
            await MigrateContextAsync(sp.GetRequiredService<PlatformDbContext>(),      "PlatformDb",       logger);
            await MigrateContextAsync(sp.GetRequiredService<TenantDbContext>(),        "TenantDb",         logger);
            await MigrateContextAsync(sp.GetRequiredService<AnalyticsDbContext>(),     "AnalyticsDb",      logger);
            await MigrateContextAsync(sp.GetRequiredService<LogDbContext>(),           "LogDb",            logger);
            await MigrateContextAsync(sp.GetRequiredService<NotificationsDbContext>(),     "NotificationsDb",      logger);
            await MigrateContextAsync(sp.GetRequiredService<MarketplaceEventsDbContext>(), "MarketplaceEventsDb",  logger);
            await MigrateContextAsync(sp.GetRequiredService<SyncDbContext>(),              "SyncDb",               logger);

            await sp.GetRequiredService<ISqlObjectMigrator>().DeployAsync();

            await sp.GetRequiredService<DatabaseSeeder>().SeedAllAsync();

            // Skip Hangfire recurring jobs in test environments where the server is disabled
            var env = app.Services.GetRequiredService<IWebHostEnvironment>();
            if (!env.IsEnvironment("Testing"))
            {
                var jobManager = sp.GetRequiredService<IRecurringJobManager>();
                jobManager.AddOrUpdate<NotificationDrainJob>(
                    "notification-drain",
                    job => job.ExecuteAsync(CancellationToken.None),
                    Cron.Minutely);

                jobManager.AddOrUpdate<StaleChequeJob>(
                    "stale-cheque-check",
                    job => job.ExecuteAsync(CancellationToken.None),
                    Cron.Daily);

                jobManager.AddOrUpdate<DepreciationJob>(
                    "monthly-depreciation",
                    job => job.ExecuteAsync(CancellationToken.None),
                    Cron.Monthly);

                jobManager.AddOrUpdate<DailyReconciliationJob>(
                    DailyReconciliationJob.JobId,
                    job => job.ExecuteAsync(CancellationToken.None),
                    Cron.Daily(hour: 2));

                jobManager.AddOrUpdate<MarketplaceOrderPollingJob>(
                    MarketplaceOrderPollingJob.JobId,
                    job => job.ExecuteAsync(CancellationToken.None),
                    "*/5 * * * *");

                jobManager.AddOrUpdate<MarketplaceInventorySyncJob>(
                    MarketplaceInventorySyncJob.JobId,
                    job => job.ExecuteAsync(CancellationToken.None),
                    Cron.Daily(hour: 3));

                jobManager.AddOrUpdate<MarketplacePriceSyncJob>(
                    MarketplacePriceSyncJob.JobId,
                    job => job.ExecuteAsync(CancellationToken.None),
                    Cron.Daily(hour: 4));

                jobManager.AddOrUpdate<UsageRolloverJob>(
                    "usage-monthly-rollover",
                    job => job.ExecuteAsync(CancellationToken.None),
                    "5 0 1 * *"); // 00:05 UTC on the 1st of every month
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup initialization failed — continuing in dev mode");
        }
    }

    private static async Task MigrateContextAsync(DbContext db, string dbName, ILogger logger)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("{DbName}: schema is up to date — no migrations to apply", dbName);
        }
        else
        {
            logger.LogInformation("{DbName}: applying {Count} pending migration(s): {Migrations}",
                dbName, pending.Count, string.Join(", ", pending));
        }

        await db.Database.MigrateAsync();

        if (pending.Count > 0)
            logger.LogInformation("{DbName}: all migrations applied successfully", dbName);
    }
}
