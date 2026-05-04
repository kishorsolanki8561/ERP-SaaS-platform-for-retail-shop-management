using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Marketplace.Connectors;
using ErpSaas.Modules.Marketplace.Infrastructure;
using ErpSaas.Modules.Marketplace.Jobs;
using ErpSaas.Modules.Marketplace.Seeds;
using ErpSaas.Modules.Marketplace.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Marketplace.Extensions;

public static class MarketplaceServiceExtensions
{
    public static IServiceCollection AddMarketplaceModule(this IServiceCollection services)
    {
        services.AddScoped<IMarketplaceAccountService, MarketplaceAccountService>();
        services.AddScoped<IMarketplaceOrderService, MarketplaceOrderService>();
        services.AddScoped<IMarketplaceSyncService, MarketplaceSyncService>();

        services.AddScoped<MarketplaceOrderPollingJob>();
        services.AddScoped<MarketplaceInventorySyncJob>();
        services.AddScoped<MarketplacePriceSyncJob>();

        services.AddHttpClient<AmazonSpApiConnector>();
        services.AddHttpClient<FlipkartConnector>();
        // Register each connector as both its concrete type (via AddHttpClient) and the interface.
        // HttpClient is resolved from the typed client registered above.
        services.AddScoped<IMarketplaceConnector>(sp => sp.GetRequiredService<AmazonSpApiConnector>());
        services.AddScoped<IMarketplaceConnector>(sp => sp.GetRequiredService<FlipkartConnector>());

        services.AddScoped<IDataSeeder, MarketplaceSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, MarketplaceConfigurator>();
        services.AddSingleton(new ServiceDescriptorEntry("Marketplace", "Online marketplace integrations (Amazon, Flipkart, Shopify)", "1.0"));
        return services;
    }
}

internal sealed class MarketplaceConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => MarketplaceModelConfiguration.Configure(modelBuilder);
}
