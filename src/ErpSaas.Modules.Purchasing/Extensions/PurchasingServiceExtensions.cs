using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Purchasing.Infrastructure;
using ErpSaas.Modules.Purchasing.Seeds;
using ErpSaas.Modules.Purchasing.Services;
using ErpSaas.Shared.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Purchasing.Extensions;

public static class PurchasingServiceExtensions
{
    public static IServiceCollection AddPurchasingModule(this IServiceCollection services)
    {
        services.AddScoped<IPurchasingService, PurchasingService>();
        services.AddDataSeeder<PurchasingSystemSeeder>();
        services.AddSingleton(new ServiceDescriptorEntry(
            "Purchasing",
            "Supplier management, purchase orders and vendor bills",
            "1.0"));
        services.AddSingleton<IEntityModelConfigurator, PurchasingModelConfigurator>();
        return services;
    }
}

internal sealed class PurchasingModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder) => PurchasingModelConfiguration.Configure(modelBuilder);
}
