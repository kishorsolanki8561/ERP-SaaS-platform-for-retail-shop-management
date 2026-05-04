namespace ErpSaas.Modules.Payment.Connectors.Results;

public sealed record SettlementLine(
    string GatewayTxnId,
    string OurReference,
    decimal SettledAmount,
    decimal Fee,
    decimal GstOnFee,
    decimal NetSettled,
    DateTime SettledAtUtc);
