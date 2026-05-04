using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.ApiAccess.Infrastructure;
using ErpSaas.Modules.ApiAccess.Jobs;
using ErpSaas.Modules.ApiAccess.Seeds;
using ErpSaas.Modules.ApiAccess.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.ApiAccess.Extensions;

public static class ApiAccessServiceExtensions
{
    public static IServiceCollection AddApiAccessModule(this IServiceCollection services)
    {
        services.AddScoped<IShopApiKeyService, ShopApiKeyService>();
        services.AddScoped<IWebhookDispatchService, WebhookDispatchService>();
        services.AddSingleton<IWebhookSignatureGenerator, WebhookSignatureGenerator>();
        services.AddScoped<WebhookDeliveryJob>();
        services.AddScoped<IDataSeeder, ApiAccessSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, ApiAccessConfigurator>();
        services.AddHttpClient("WebhookClient");
        services.AddSingleton(new ServiceDescriptorEntry("ApiAccess", "API keys and outbound webhooks for shop integrations", "1.0"));
        return services;
    }
}

internal sealed class ApiAccessConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => ApiAccessModelConfiguration.Configure(modelBuilder);
}
