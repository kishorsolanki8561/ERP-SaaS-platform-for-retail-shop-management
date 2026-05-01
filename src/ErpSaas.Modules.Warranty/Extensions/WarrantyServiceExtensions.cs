using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Warranty.Infrastructure;
using ErpSaas.Modules.Warranty.Seeds;
using ErpSaas.Modules.Warranty.Services;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Warranty.Extensions;

public static class WarrantyServiceExtensions
{
    public static IServiceCollection AddWarrantyModule(this IServiceCollection services)
    {
        services.AddScoped<IWarrantyService, WarrantyService>();
        services.AddScoped<IDataSeeder, WarrantySystemSeeder>();

        services.AddSingleton<IEntityModelConfigurator, WarrantyConfigurator>();
        return services;
    }
}

internal sealed class WarrantyConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => WarrantyModelConfiguration.Configure(modelBuilder);
}
