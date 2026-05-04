namespace ErpSaas.Modules.Payment.Connectors;

/// <summary>
/// Resolves the correct IPaymentGatewayConnector for a given gateway code.
/// Falls back to SimulatedGatewayConnector when no active PaymentGatewayAccount
/// exists for the requested code — ensuring all flows work without real credentials.
/// </summary>
public interface IGatewayConnectorRegistry
{
    Task<IPaymentGatewayConnector> ResolveAsync(string gatewayCode, CancellationToken ct = default);
}
