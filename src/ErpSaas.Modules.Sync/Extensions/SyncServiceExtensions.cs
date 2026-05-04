using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Sync.Infrastructure;
using ErpSaas.Modules.Sync.Seeds;
using ErpSaas.Modules.Sync.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Sync.Extensions;

public static class SyncServiceExtensions
{
    public static IServiceCollection AddSyncModule(this IServiceCollection services)
    {
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<IDataSeeder, SyncSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, SyncConfigurator>();
        services.AddSingleton(new ServiceDescriptorEntry(
            "Sync",
            "Device registration, offline command queue, and invoice range allocation for §6.19",
            "1.0"));
        return services;
    }
}

internal sealed class SyncConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => SyncModelConfiguration.Configure(modelBuilder);
}
