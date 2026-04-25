using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Billing.Infrastructure;
using ErpSaas.Modules.Billing.Seeds;
using ErpSaas.Modules.Billing.Services;
using ErpSaas.Shared.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace ErpSaas.Modules.Billing.Extensions;

public static class BillingServiceExtensions
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services)
    {
        // QuestPDF community license (free for open-source and commercial use under 1M USD revenue)
        QuestPDF.Settings.License = LicenseType.Community;

        services.AddScoped<IBillingService, BillingService>();
        services.AddSingleton<IInvoicePdfGenerator, InvoicePdfGenerator>();
        services.AddDataSeeder<BillingSystemSeeder>();
        services.AddSingleton(new ServiceDescriptorEntry(
            "Billing",
            "Invoicing and sales billing",
            "1.0"));
        services.AddSingleton<IEntityModelConfigurator, BillingModelConfigurator>();
        return services;
    }
}

internal sealed class BillingModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder) => BillingModelConfiguration.Configure(modelBuilder);
}
