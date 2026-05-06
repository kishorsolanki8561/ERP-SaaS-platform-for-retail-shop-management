using ErpSaas.Api.Seeds;
using ErpSaas.Infrastructure.Authorization;
using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Accounting.Extensions;
using ErpSaas.Modules.Billing.Extensions;
using ErpSaas.Modules.Purchasing.Extensions;
using ErpSaas.Modules.SalesReturns.Extensions;
using ErpSaas.Modules.Reports.Extensions;
using ErpSaas.Modules.Warranty.Extensions;
using ErpSaas.Modules.Pricing.Extensions;
using ErpSaas.Modules.Transport.Extensions;
using ErpSaas.Modules.Quotations.Extensions;
using ErpSaas.Modules.Payment.Extensions;
using ErpSaas.Modules.Hardware.Extensions;
using ErpSaas.Modules.Hr.Extensions;
using ErpSaas.Modules.ApiAccess.Extensions;
using ErpSaas.Modules.Sync.Extensions;
using ErpSaas.Modules.CustomerPortal.Extensions;
using ErpSaas.Modules.Marketplace.Extensions;
using ErpSaas.Modules.Crm.Extensions;
using ErpSaas.Modules.Identity.Extensions;
using ErpSaas.Modules.Inventory.Extensions;
using ErpSaas.Modules.Masters.Extensions;
using ErpSaas.Modules.Shift.Extensions;
using ErpSaas.Modules.Wallet.Extensions;
using ErpSaas.Modules.Verticals.Extensions;
using ErpSaas.Modules.ServiceJobs.Extensions;
using ErpSaas.Modules.Verticals.Medical.Extensions;
using ErpSaas.Modules.Verticals.Grocery.Extensions;
using ErpSaas.Shared.Authorization;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

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

        // ── Rate limiting ──────────────────────────────────────────────────────
        // RateLimit:Disabled=true (set by integration tests) raises permit limits
        // to int.MaxValue so concurrent test requests never trip 429 inside a run.
        var rateLimitDisabled = configuration.GetValue<bool>("RateLimit:Disabled");
        services.AddRateLimiter(opts =>
        {
            opts.OnRejected = async (ctx, ct) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                ctx.HttpContext.Response.Headers["Retry-After"] = "60";
                await ctx.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "TOO_MANY_REQUESTS", message = "Rate limit exceeded. Please try again later." }, ct);
            };

            // Auth endpoints — 5 requests / minute / IP
            opts.AddFixedWindowLimiter(RateLimitPolicies.Auth, o =>
            {
                o.Window            = TimeSpan.FromMinutes(1);
                o.PermitLimit       = rateLimitDisabled ? int.MaxValue : 5;
                o.QueueLimit        = 0;
                o.AutoReplenishment = true;
            });

            // OTP / signup endpoints — 3 requests / hour / IP
            opts.AddFixedWindowLimiter(RateLimitPolicies.Otp, o =>
            {
                o.Window            = TimeSpan.FromHours(1);
                o.PermitLimit       = rateLimitDisabled ? int.MaxValue : 3;
                o.QueueLimit        = 0;
                o.AutoReplenishment = true;
            });

            // General public / authenticated API — 100 requests / minute / IP
            opts.AddFixedWindowLimiter(RateLimitPolicies.Api, o =>
            {
                o.Window            = TimeSpan.FromMinutes(1);
                o.PermitLimit       = rateLimitDisabled ? int.MaxValue : 100;
                o.QueueLimit        = 0;
                o.AutoReplenishment = true;
            });
        });

        services.AddControllers(opts => opts.Filters.AddService<CaptchaValidationFilter>())
            .AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter());
            })
            .AddApplicationPart(typeof(Modules.Masters.Controllers.DdlController).Assembly)
            .AddApplicationPart(typeof(Modules.Identity.Controllers.AuthController).Assembly)
            .AddApplicationPart(typeof(Modules.Crm.Controllers.CrmController).Assembly)
            .AddApplicationPart(typeof(Modules.Inventory.Controllers.InventoryController).Assembly)
            .AddApplicationPart(typeof(Modules.Billing.Controllers.BillingController).Assembly)
            .AddApplicationPart(typeof(Modules.Wallet.Controllers.WalletController).Assembly)
            .AddApplicationPart(typeof(Modules.Shift.Controllers.ShiftController).Assembly)
            .AddApplicationPart(typeof(Modules.Accounting.Controllers.AccountingController).Assembly)
            .AddApplicationPart(typeof(Modules.Purchasing.Controllers.PurchasingController).Assembly)
            .AddApplicationPart(typeof(Modules.SalesReturns.Controllers.SalesReturnsController).Assembly)
            .AddApplicationPart(typeof(Modules.Reports.Controllers.ReportsController).Assembly)
            .AddApplicationPart(typeof(Modules.Warranty.Controllers.WarrantyController).Assembly)
            .AddApplicationPart(typeof(Modules.Pricing.Controllers.PricingController).Assembly)
            .AddApplicationPart(typeof(Modules.Transport.Controllers.TransportController).Assembly)
            .AddApplicationPart(typeof(Modules.Quotations.Controllers.QuotationsController).Assembly)
            .AddApplicationPart(typeof(Modules.Payment.Controllers.PaymentGatewayController).Assembly)
            .AddApplicationPart(typeof(Modules.Hardware.Controllers.DeviceProfilesController).Assembly)
            .AddApplicationPart(typeof(Modules.Hr.Controllers.HrController).Assembly)
            .AddApplicationPart(typeof(Modules.Marketplace.Controllers.MarketplaceController).Assembly)
            .AddApplicationPart(typeof(Modules.CustomerPortal.Controllers.PortalAuthController).Assembly)
            .AddApplicationPart(typeof(Modules.ApiAccess.Controllers.ShopApiKeysController).Assembly)
            .AddApplicationPart(typeof(Modules.Sync.Controllers.DevicesController).Assembly)
            .AddApplicationPart(typeof(Modules.Verticals.Controllers.VerticalPacksController).Assembly)
            .AddApplicationPart(typeof(Modules.ServiceJobs.Controllers.ServiceJobsController).Assembly)
            .AddApplicationPart(typeof(Modules.Verticals.Medical.Controllers.MedicalInventoryController).Assembly)
            .AddApplicationPart(typeof(Modules.Verticals.Grocery.Controllers.LoyaltyController).Assembly);

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
        services.AddAccountingModule();
        services.AddPurchasingModule();
        services.AddSalesReturnsModule();
        services.AddReportsModule();
        services.AddWarrantyModule();
        services.AddPricingModule();
        services.AddTransportModule();
        services.AddQuotationsModule();
        services.AddPaymentModule();
        services.AddHardwareModule();
        services.AddHrModule();
        services.AddMarketplaceModule();
        services.AddCustomerPortalModule();
        services.AddApiAccessModule();
        services.AddSyncModule();
        services.AddVerticalsModule();
        services.AddServiceJobsModule();
        services.AddMedicalModule();
        services.AddGroceryModule();

        // Demo data seeder — only active when Features:SeedDemoData = true
        services.AddDataSeeder<DemoDataSeeder>();

        services.AddSignalR();

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
