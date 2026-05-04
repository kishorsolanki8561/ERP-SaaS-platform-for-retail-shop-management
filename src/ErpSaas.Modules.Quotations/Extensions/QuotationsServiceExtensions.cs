using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Quotations.Infrastructure;
using ErpSaas.Modules.Quotations.Seeds;
using ErpSaas.Modules.Quotations.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Quotations.Extensions;

public static class QuotationsServiceExtensions
{
    public static IServiceCollection AddQuotationsModule(this IServiceCollection services)
    {
        services.AddScoped<IQuotationsService, QuotationsService>();
        services.AddScoped<IDataSeeder, QuotationsSystemSeeder>();
        services.AddSingleton<IEntityModelConfigurator, QuotationsModelConfigurator>();
        services.AddSingleton(new ServiceDescriptorEntry("Quotations", "Quotation → Sales Order → Delivery Challan workflow", "1.0"));
        return services;
    }
}

internal sealed class QuotationsModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => QuotationsModelConfiguration.Configure(modelBuilder);
}
