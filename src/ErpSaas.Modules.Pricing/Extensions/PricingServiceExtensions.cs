using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Pricing.Infrastructure;
using ErpSaas.Modules.Pricing.Seeds;
using ErpSaas.Modules.Pricing.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Pricing.Extensions;

public static class PricingServiceExtensions
{
    public static IServiceCollection AddPricingModule(this IServiceCollection services)
    {
        services.AddSingleton<IPricingEngine, PricingEngine>();
        services.AddScoped<IPricingManagementService, PricingManagementService>();
        services.AddScoped<IDataSeeder, PricingSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, PricingModelConfigurator>();
        services.AddSingleton(new ServiceDescriptorEntry("Pricing", "Discount rules, extra charges, and promotional offers engine", "1.0"));
        return services;
    }
}

internal sealed class PricingModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => PricingModelConfiguration.Configure(modelBuilder);
}
