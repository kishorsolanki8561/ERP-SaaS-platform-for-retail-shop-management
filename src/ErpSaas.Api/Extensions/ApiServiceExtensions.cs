using ErpSaas.Infrastructure.Authorization;
using ErpSaas.Modules.Billing.Extensions;
using ErpSaas.Modules.Crm.Extensions;
using ErpSaas.Modules.Identity.Extensions;
using ErpSaas.Modules.Inventory.Extensions;
using ErpSaas.Modules.Masters.Extensions;
using ErpSaas.Modules.Wallet.Extensions;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Api.Extensions;

public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<CaptchaValidationFilter>();

        services.AddControllers(opts => opts.Filters.AddService<CaptchaValidationFilter>())
            .AddApplicationPart(typeof(Modules.Masters.Controllers.DdlController).Assembly)
            .AddApplicationPart(typeof(Modules.Identity.Controllers.AuthController).Assembly)
            .AddApplicationPart(typeof(Modules.Crm.Controllers.CrmController).Assembly)
            .AddApplicationPart(typeof(Modules.Inventory.Controllers.InventoryController).Assembly)
            .AddApplicationPart(typeof(Modules.Billing.Controllers.BillingController).Assembly)
            .AddApplicationPart(typeof(Modules.Wallet.Controllers.WalletController).Assembly);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
            c.SwaggerDoc("v1", new() { Title = "ShopEarth ERP API", Version = "v1" }));

        services.AddIdentityModule(configuration);
        services.AddMastersModule();
        services.AddCrmModule();
        services.AddInventoryModule();
        services.AddBillingModule();
        services.AddWalletModule();

        services.AddHangfire(cfg => cfg
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("LogDb")));
        services.AddHangfireServer();

        return services;
    }
}
