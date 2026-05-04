namespace ErpSaas.Modules.Payment.Connectors.Results;

public sealed record GatewayInitiateResult(
    bool IsSuccess,
    string GatewayTxnId,
    string? PaymentUrl,
    string? FailureCode,
    string? FailureMessage)
{
    public static GatewayInitiateResult Success(string gatewayTxnId, string? paymentUrl = null)
        => new(true, gatewayTxnId, paymentUrl, null, null);

    public static GatewayInitiateResult Failure(string failureCode, string failureMessage)
        => new(false, string.Empty, null, failureCode, failureMessage);
}
