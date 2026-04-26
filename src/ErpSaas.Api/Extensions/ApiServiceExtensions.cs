using ErpSaas.Infrastructure.Authorization;
using ErpSaas.Modules.Billing.Extensions;
using ErpSaas.Modules.Crm.Extensions;
using ErpSaas.Modules.Identity.Extensions;
using ErpSaas.Modules.Inventory.Extensions;
using ErpSaas.Modules.Masters.Extensions;
using ErpSaas.Modules.Shift.Extensions;
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

        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()));

        services.AddControllers(opts => opts.Filters.AddService<CaptchaValidationFilter>())
            .AddApplicationPart(typeof(Modules.Masters.Controllers.DdlController).Assembly)
            .AddApplicationPart(typeof(Modules.Identity.Controllers.AuthController).Assembly)
            .AddApplicationPart(typeof(Modules.Crm.Controllers.CrmController).Assembly)
            .AddApplicationPart(typeof(Modules.Inventory.Controllers.InventoryController).Assembly)
            .AddApplicationPart(typeof(Modules.Billing.Controllers.BillingController).Assembly)
            .AddApplicationPart(typeof(Modules.Wallet.Controllers.WalletController).Assembly)
            .AddApplicationPart(typeof(Modules.Shift.Controllers.ShiftController).Assembly);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
            c.SwaggerDoc("v1", new() { Title = "ShopEarth ERP API", Version = "v1" }));

        services.AddIdentityModule(configuration);
        services.AddMastersModule();
        services.AddCrmModule();
        services.AddInventoryModule();
        services.AddBillingModule();
        services.AddWalletModule();
        services.AddShiftModule();

        // Use (serviceProvider, cfg) overload so the connection string is resolved
        // at runtime (after all IConfiguration sources — including test overrides — are merged)
        // rather than at service-registration time.
        services.AddHangfire((sp, cfg) =>
        {
            var connStr = sp.GetRequiredService<IConfiguration>().GetConnectionString("LogDb");
            cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
               .UseSimpleAssemblyNameTypeSerializer()
               .UseRecommendedSerializerSettings()
               .UseSqlServerStorage(connStr);
        });
        // Skip the Hangfire background server in test mode to avoid it connecting to
        // SQL Server before migrations have run (tests drive jobs synchronously).
        if (!configuration.GetValue<bool>("Hangfire:DisableServer", false))
        {
            services.AddHangfireServer();
        }

        return services;
    }
}
