using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Verticals.Grocery.Infrastructure;
using ErpSaas.Modules.Verticals.Grocery.Seeds;
using ErpSaas.Modules.Verticals.Grocery.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Verticals.Grocery.Extensions;

public static class GroceryServiceExtensions
{
    public static IServiceCollection AddGroceryModule(this IServiceCollection services)
    {
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<IDataSeeder, GrocerySystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, GroceryConfigurator>();
        services.AddSingleton(new ServiceDescriptorEntry("Grocery", "Grocery vertical — loyalty points, FIFO costing", "1.0"));
        return services;
    }
}

internal sealed class GroceryConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => GroceryModelConfiguration.Configure(modelBuilder);
}
