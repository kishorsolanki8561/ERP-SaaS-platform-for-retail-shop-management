using ErpSaas.Infrastructure.Data;
using ErpSaas.Modules.Payment.Connectors.Simulated;
using ErpSaas.Modules.Payment.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ErpSaas.Modules.Payment.Connectors;

public sealed class GatewayConnectorRegistry(
    IServiceProvider services,
    TenantDbContext db) : IGatewayConnectorRegistry
{
    public async Task<IPaymentGatewayConnector> ResolveAsync(string gatewayCode, CancellationToken ct = default)
    {
        // Only activate a real connector when an active account exists for this shop.
        var hasActiveAccount = await db.Set<PaymentGatewayAccount>()
            .AnyAsync(a => a.GatewayCode == gatewayCode && a.IsActive, ct);

        if (hasActiveAccount)
        {
            var connector = services.GetKeyedService<IPaymentGatewayConnector>(gatewayCode);
            if (connector is not null)
                return connector;
        }

        // Fallback: simulated connector works without any credentials.
        return services.GetRequiredKeyedService<IPaymentGatewayConnector>("Simulated");
    }
}
