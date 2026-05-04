namespace ErpSaas.Modules.Payment.Enums;

public enum ReconciliationExceptionType
{
    MissingInGateway,
    MissingInOurDb,
    AmountMismatch,
    FeeUnexpected,
}
