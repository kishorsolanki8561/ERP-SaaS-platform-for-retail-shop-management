using ErpSaas.Shared.Services;
using ErpSaas.Shared.ServiceCatalog;
using ErpSaas.Modules.{Module}.Services;
using ErpSaas.Modules.{Module}.Seeds;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.{Module};

public static class {Module}Module
{
    /// <summary>SQL schema owned by this module. Arch test enforces every entity lives here.</summary>
    public const string Schema = "{schema}";       // MUST match docs/MASTER_PLAN.md §4.6 mapping

    public static IServiceCollection Add{Module}Module(this IServiceCollection services, IConfiguration config)
    {
        // ── Services ───────────────────────────────────────────────────────
        services.AddScoped<I{Module}Service, {Module}Service>();
        // services.AddScoped<I{Module}ReportQueries, {Module}ReportQueries>();  // if reports exist

        // ── Validators (FluentValidation — auto-discovered by assembly scan
        //     if wired; if not, register explicitly here) ──────────────────
        // services.AddValidatorsFromAssemblyContaining<{Module}Module>();

        // ── Seeders ────────────────────────────────────────────────────────
        services.AddScoped<ISystemSeeder, {Module}SystemSeeder>();
        // services.AddScoped<ITenantSeeder, {Module}TenantSeeder>();  // if per-shop seed needed

        // ── Service catalog registration (§5.14) ───────────────────────────
        services.AddServiceCatalogEntry(new ServiceDescriptor
        {
            Code       = "{MODULE_CODE}",                       // e.g. "BILLING"
            Category   = ServiceCategory.{Category},            // CoreRetail / Financial / Operations / Hr / Marketplace / Platform / AdminSupport
            Name       = "{Human readable name}",               // e.g. "Billing & Invoicing"
            Tagline    = "{One-line pitch}",                    // shown on /api/services + landing page
            IconCode   = "pi-{icon}",                           // PrimeIcons
            RequiresFeature = "{Module}.{PrimaryFeature}",      // null if always-on
            DocsUrl    = "/docs/{module}",
            HealthCheck = "/health/{module}",
            Version    = "1.0.0"
        });

        // ── Module-specific options (bind from appsettings) ────────────────
        // services.Configure<{Module}Options>(config.GetSection("{Module}"));

        return services;
    }
}
