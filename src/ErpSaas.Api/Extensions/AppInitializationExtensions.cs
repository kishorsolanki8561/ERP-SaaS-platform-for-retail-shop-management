using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Messaging;
using ErpSaas.Infrastructure.Seeds;
using ErpSaas.Infrastructure.Sql;
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
            await sp.GetRequiredService<PlatformDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<TenantDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<AnalyticsDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<LogDbContext>().Database.MigrateAsync();
            await sp.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();

            await sp.GetRequiredService<ISqlObjectMigrator>().DeployAsync();

            await sp.GetRequiredService<DatabaseSeeder>().SeedAllAsync();

            // Skip Hangfire recurring jobs in test environments where the server is disabled
            var env = app.Services.GetRequiredService<IWebHostEnvironment>();
            if (!env.IsEnvironment("Testing"))
            {
                RecurringJob.AddOrUpdate<NotificationDrainJob>(
                    "notification-drain",
                    job => job.ExecuteAsync(CancellationToken.None),
                    Cron.Minutely);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup initialization failed — continuing in dev mode");
        }
    }
}
