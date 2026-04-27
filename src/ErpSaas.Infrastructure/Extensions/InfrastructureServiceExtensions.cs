using ErpSaas.Infrastructure.Catalog;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Ddl;
using ErpSaas.Infrastructure.Dapper;
using ErpSaas.Infrastructure.Files;
using ErpSaas.Infrastructure.MultiTenant;
using ErpSaas.Infrastructure.Messaging;
using ErpSaas.Infrastructure.Metering;
using ErpSaas.Infrastructure.Seeds;
using ErpSaas.Infrastructure.Sequence;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Infrastructure.Sql;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── DbContexts ─────────────────────────────────────────────────────────
        // Interceptors are resolved by EF Core from the DI container as DbContext
        // constructor parameters — do NOT also add them via AddInterceptors() here.
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<TenantSaveChangesInterceptor>();

        services.AddDbContext<PlatformDbContext>(opts =>
            opts.UseSqlServer(
                configuration.GetConnectionString("PlatformDb"),
                sql => sql.MigrationsAssembly(typeof(PlatformDbContext).Assembly.FullName)));

        services.AddDbContext<TenantDbContext>(opts =>
            opts.UseSqlServer(
                configuration.GetConnectionString("TenantDb"),
                sql => sql.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName)));

        services.AddDbContext<AnalyticsDbContext>(opts =>
            opts.UseSqlServer(
                configuration.GetConnectionString("AnalyticsDb"),
                sql => sql.MigrationsAssembly(typeof(AnalyticsDbContext).Assembly.FullName)));

        services.AddDbContext<LogDbContext>(opts =>
            opts.UseSqlServer(
                configuration.GetConnectionString("LogDb"),
                sql => sql.MigrationsAssembly(typeof(LogDbContext).Assembly.FullName)));

        services.AddDbContext<NotificationsDbContext>(opts =>
            opts.UseSqlServer(
                configuration.GetConnectionString("NotificationsDb"),
                sql => sql.MigrationsAssembly(typeof(NotificationsDbContext).Assembly.FullName)));

        services.AddDbContext<MarketplaceEventsDbContext>(opts =>
            opts.UseSqlServer(
                configuration.GetConnectionString("MarketplaceEventsDb"),
                sql => sql.MigrationsAssembly(typeof(MarketplaceEventsDbContext).Assembly.FullName)));

        services.AddDbContext<SyncDbContext>(opts =>
            opts.UseSqlServer(
                configuration.GetConnectionString("SyncDb"),
                sql => sql.MigrationsAssembly(typeof(SyncDbContext).Assembly.FullName)));

        // ── File storage ───────────────────────────────────────────────────────
        var useAzure = !string.IsNullOrEmpty(configuration.GetConnectionString("AzureStorage"));
        if (useAzure)
            services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
        else
            services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IFileUploadService, FileUploadService>();

        // ── HTTP clients ───────────────────────────────────────────────────────
        services.AddHttpClient("turnstile", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        // ── Cross-cutting services ─────────────────────────────────────────────
        services.AddMemoryCache();
        services.AddSingleton<IErrorLogger, ErrorLogger>();
        services.AddHostedService(sp => (ErrorLogger)sp.GetRequiredService<IErrorLogger>());
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IDdlService, DdlService>();
        services.AddScoped<ISequenceService, SequenceService>();
        services.AddScoped<ISqlObjectMigrator, SqlObjectMigrator>();

        // ── Service catalog ────────────────────────────────────────────────────
        // Entries are registered as singletons so ServiceCatalog picks them up
        // via IEnumerable<ServiceDescriptorEntry> injection. Each AddXxx() call
        // co-locates its catalog entry with the service it describes.
        services.AddSingleton<IServiceCatalog, ServiceCatalog>();
        services.AddSingleton(new ServiceDescriptorEntry("DDL", "Dropdown catalog service", "1.0"));
        services.AddSingleton(new ServiceDescriptorEntry("Sequence", "Document number sequencing", "1.0"));
        services.AddSingleton(new ServiceDescriptorEntry("ServiceCatalog", "Registered service registry", "1.0"));
        services.AddSingleton(new ServiceDescriptorEntry("ErrorLogger", "Async error logging to LogDb", "1.0"));
        services.AddSingleton(new ServiceDescriptorEntry("AuditLogger", "Mutation audit trail to LogDb", "1.0"));

        // ── Multi-tenant connection resolution ─────────────────────────────────
        services.AddScoped<IShopConnectionResolver, ShopConnectionResolver>();

        // ── Dapper context + slow-query logging ────────────────────────────────
        services.AddScoped<IDapperContext, TenantDapperContext>();
        services.AddScoped<DapperLoggingInterceptor>();

        // ── Messaging ──────────────────────────────────────────────────────────
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<NotificationDrainJob>();

        // ── Metering ───────────────────────────────────────────────────────────
        services.AddSingleton<IEntityModelConfigurator, MeteringModelConfigurator>();
        services.AddScoped<IUsageMeterService, UsageMeterService>();

        // ── Seeders ────────────────────────────────────────────────────────────
        services.AddScoped<DatabaseSeeder>();
        services.AddDataSeeder<DdlDataSeeder>();
        services.AddDataSeeder<NotificationTemplateSeeder>();

        return services;
    }

    /// <summary>
    /// Registers a data seeder so DatabaseSeeder picks it up automatically.
    /// Call this from each module's DI extension, not from Program.cs.
    /// </summary>
    public static IServiceCollection AddDataSeeder<TSeeder>(this IServiceCollection services)
        where TSeeder : class, IDataSeeder
    {
        services.AddScoped<IDataSeeder, TSeeder>();
        return services;
    }
}
