using ErpSaas.Infrastructure.Extensions;
using ErpSaas.Modules.Payment.Connectors;
using ErpSaas.Modules.Payment.Connectors.Paytm;
using ErpSaas.Modules.Payment.Connectors.PhonePe;
using ErpSaas.Modules.Payment.Connectors.Razorpay;
using ErpSaas.Modules.Payment.Connectors.Simulated;
using ErpSaas.Modules.Payment.Connectors.Stripe;
using ErpSaas.Modules.Payment.Infrastructure;
using ErpSaas.Modules.Payment.Jobs;
using ErpSaas.Modules.Payment.Seeds;
using ErpSaas.Modules.Payment.Services;
using ErpSaas.Shared.Catalog;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Payment.Extensions;

public static class PaymentServiceExtensions
{
    public static IServiceCollection AddPaymentModule(this IServiceCollection services)
    {
        // ── Business services ──────────────────────────────────────────────────
        services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
        services.AddScoped<IPaymentReconciliationService, PaymentReconciliationService>();
        services.AddScoped<DailyReconciliationJob>();
        services.AddDataSeeder<PaymentSystemSeeder>();

        // ── Connector registry ─────────────────────────────────────────────────
        services.AddScoped<IGatewayConnectorRegistry, GatewayConnectorRegistry>();

        // Simulated — always available, used when no real account is configured
        services.AddKeyedScoped<IPaymentGatewayConnector, SimulatedGatewayConnector>("Simulated");

        // Real connectors — activated when shop saves credentials via /api/payment/gateways
        services.AddHttpClient<RazorpayConnector>(c => c.BaseAddress = new Uri("https://api.razorpay.com/"));
        services.AddKeyedScoped<IPaymentGatewayConnector, RazorpayConnector>("Razorpay");

        services.AddHttpClient<StripeConnector>(c => c.BaseAddress = new Uri("https://api.stripe.com/"));
        services.AddKeyedScoped<IPaymentGatewayConnector, StripeConnector>("Stripe");

        services.AddHttpClient<PhonePeConnector>(c => c.BaseAddress = new Uri("https://api.phonepe.com/"));
        services.AddKeyedScoped<IPaymentGatewayConnector, PhonePeConnector>("PhonePe");

        services.AddHttpClient<PaytmConnector>(c => c.BaseAddress = new Uri("https://securegw.paytm.in/"));
        services.AddKeyedScoped<IPaymentGatewayConnector, PaytmConnector>("Paytm");

        // ── EF configuration ───────────────────────────────────────────────────
        services.AddSingleton<IEntityModelConfigurator, PaymentConfigurator>();
        services.AddSingleton(new ServiceDescriptorEntry("Payment", "Online payment gateway integrations, webhooks, and daily reconciliation", "1.0"));

        return services;
    }
}

internal sealed class PaymentConfigurator : IEntityModelConfigurator
{
    public void Configure(ModelBuilder modelBuilder)
        => PaymentModelConfiguration.Configure(modelBuilder);
}
