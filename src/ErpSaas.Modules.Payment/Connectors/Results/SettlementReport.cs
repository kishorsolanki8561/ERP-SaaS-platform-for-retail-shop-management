namespace ErpSaas.Modules.Payment.Connectors.Results;

public sealed record SettlementReport(
    string GatewayCode,
    DateTime SettlementDate,
    IReadOnlyList<SettlementLine> Lines);
