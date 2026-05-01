using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.SalesReturns.Infrastructure;
using ErpSaas.Modules.SalesReturns.Seeds;
using ErpSaas.Modules.SalesReturns.Services;
using ErpSaas.Shared.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.SalesReturns.Extensions;

public static class SalesReturnsServiceExtensions
{
    public static IServiceCollection AddSalesReturnsModule(this IServiceCollection services)
    {
        services.AddScoped<ISalesReturnsService, SalesReturnsService>();
        services.AddDataSeeder<SalesReturnsSystemSeeder>();
        services.AddSingleton(new ServiceDescriptorEntry(
            "SalesReturns",
            "Sales returns and credit note management",
            "1.0"));
        services.AddSingleton<IEntityModelConfigurator, SalesReturnsModelConfigurator>();
        return services;
    }
}

internal sealed class SalesReturnsModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder) => SalesReturnsModelConfiguration.Configure(modelBuilder);
}
