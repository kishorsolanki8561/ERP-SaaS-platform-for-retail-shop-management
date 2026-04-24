using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Infrastructure.Seeds;
using ErpSaas.Shared.Seeds;
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
        // Interceptors are resolved by EF Core from the DI container as DbContext
        // constructor parameters — do NOT also add them via AddInterceptors() here,
        // or they fire twice.
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

        // Seeder infrastructure — modules register their seeders via AddDataSeeder<T>()
        services.AddScoped<DatabaseSeeder>();
        services.AddDataSeeder<DdlDataSeeder>();

        return services;
    }

    public static IServiceCollection AddDataSeeder<TSeeder>(this IServiceCollection services)
        where TSeeder : class, IDataSeeder
    {
        services.AddScoped<IDataSeeder, TSeeder>();
        return services;
    }
}
