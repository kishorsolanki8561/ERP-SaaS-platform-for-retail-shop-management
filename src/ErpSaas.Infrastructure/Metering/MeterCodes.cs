namespace ErpSaas.Infrastructure.Metering;

public static class MeterCodes
{
    public const string Invoices    = "invoices";
    public const string Products    = "products";
    public const string ActiveUsers = "active_users";
    public const string SmsPerMonth = "sms";
    public const string EmailPerMonth = "email";
    public const string StorageMb   = "storage_mb";

    // Monthly meters reset each billing period; persistent meters accumulate indefinitely.
    public static bool IsMonthly(string code) =>
        code is Invoices or SmsPerMonth or EmailPerMonth;
}
