namespace ErpSaas.Modules.Payment.Connectors.Results;

public sealed record GatewayRefundResult(
    bool IsSuccess,
    string? GatewayRefundId,
    decimal RefundedAmount,
    string? FailureCode,
    string? FailureMessage)
{
    public static GatewayRefundResult Success(string gatewayRefundId, decimal refundedAmount)
        => new(true, gatewayRefundId, refundedAmount, null, null);

    public static GatewayRefundResult Failure(string failureCode, string failureMessage)
        => new(false, null, 0m, failureCode, failureMessage);
}
