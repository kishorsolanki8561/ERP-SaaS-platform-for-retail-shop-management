using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.CustomerPortal.Infrastructure;
using ErpSaas.Modules.CustomerPortal.Seeds;
using ErpSaas.Modules.CustomerPortal.Services;
using ErpSaas.Shared.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.CustomerPortal.Extensions;

public static class CustomerPortalServiceExtensions
{
    public static IServiceCollection AddCustomerPortalModule(this IServiceCollection services)
    {
        services.AddScoped<ICustomerPortalAuthService, CustomerPortalAuthService>();
        services.AddScoped<ICustomerPortalService, CustomerPortalService>();
        services.AddScoped<IOnlineOrderService, OnlineOrderService>();
        services.AddScoped<ICustomerInquiryService, CustomerInquiryService>();
        services.AddDataSeeder<CustomerPortalSystemSeeder>();
        services.AddSingleton(new ServiceDescriptorEntry(
            "CustomerPortal",
            "Customer self-service portal — cross-shop history, online orders, inquiries",
            "1.0"));
        services.AddSingleton<IEntityModelConfigurator, CustomerPortalModelConfigurator>();
        return services;
    }
}

internal sealed class CustomerPortalModelConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder) =>
        CustomerPortalModelConfiguration.Configure(modelBuilder);
}
