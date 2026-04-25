namespace ErpSaas.Shared.Messages;

public static class Constants
{
    public static class Plans
    {
        public const string Starter    = "Starter";
        public const string Growth     = "Growth";
        public const string Enterprise = "Enterprise";
    }

    public static class Roles
    {
        public const string PlatformOwner = "PlatformOwner";
        public const string ShopAdmin     = "ShopAdmin";
    }

    public static class DdlKeys
    {
        public const string InvoiceStatus   = "INVOICE_STATUS";
        public const string CustomerType    = "CUSTOMER_TYPE";
        public const string ProductCategory = "PRODUCT_CATEGORY";
        public const string PaymentMode     = "PAYMENT_MODE";
        public const string IndianState     = "INDIAN_STATE";
        public const string Currency        = "CURRENCY";
    }

    public static class SequenceCodes
    {
        public const string InvoiceRetail     = "INVOICE_RETAIL";
        public const string InvoiceWholesale  = "INVOICE_WHOLESALE";
        public const string PurchaseOrder     = "PURCHASE_ORDER";
        public const string SalesOrder        = "SALES_ORDER";
        public const string DeliveryChallan   = "DELIVERY_CHALLAN";
        public const string CreditNote        = "CREDIT_NOTE";
        public const string PaymentReceipt    = "PAYMENT_RECEIPT";
        public const string Voucher           = "VOUCHER";
        public const string WarrantyClaim     = "WARRANTY_CLAIM";
    }

    public static class SequencePrefixes
    {
        public const string InvoiceRetail     = "INV";
        public const string InvoiceWholesale  = "WINV";
        public const string PurchaseOrder     = "PO";
        public const string SalesOrder        = "SO";
        public const string DeliveryChallan   = "DC";
        public const string CreditNote        = "CN";
        public const string PaymentReceipt    = "RCP";
        public const string Voucher           = "VCH";
        public const string WarrantyClaim     = "WC";
    }

    public static class Pagination
    {
        public const int DefaultPageSize    = 20;
        public const int HsnSearchLimit     = 50;
        public const int NotificationBatch  = 50;
    }

    public static class Security
    {
        public const string ConfigSection               = "Security";
        public const string BcryptWorkFactorKey         = "Security:BcryptWorkFactor";
        public const string MaxFailedLoginAttemptsKey   = "Security:MaxFailedLoginAttempts";
        public const string LockoutDurationMinutesKey   = "Security:LockoutDurationMinutes";
        public const string TotpChallengeMinutesKey     = "Security:TotpChallengeMinutes";
        public const string RefreshTokenDaysKey         = "Jwt:RefreshTokenDays";

        public const int DefaultBcryptWorkFactor        = 12;
        public const int DefaultMaxFailedLoginAttempts  = 5;
        public const int DefaultLockoutDurationMinutes  = 15;
        public const int DefaultTotpChallengeMinutes    = 5;
        public const int DefaultRefreshTokenDays        = 30;
    }

    public static class Notifications
    {
        public const int MaxAttempts = 5;
    }

    public static class Performance
    {
        public const int SlowQueryThresholdMs = 500;
    }
}
