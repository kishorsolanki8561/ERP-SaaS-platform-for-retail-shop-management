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
        public const string InvoiceStatus       = "INVOICE_STATUS";
        public const string CustomerType        = "CUSTOMER_TYPE";
        public const string ProductCategory     = "PRODUCT_CATEGORY";
        public const string PaymentMode         = "PAYMENT_MODE";
        public const string IndianState         = "INDIAN_STATE";
        public const string Currency            = "CURRENCY";
        public const string WalletReferenceType = "WALLET_REFERENCE_TYPE";
        public const string ShiftCashReason     = "SHIFT_CASH_REASON";
        public const string VoucherType         = "VOUCHER_TYPE";
        public const string GstSlab             = "GST_SLAB";
        public const string ChequeBounceReason  = "CHEQUE_BOUNCE_REASON";
        public const string FixedAssetCategory  = "FIXED_ASSET_CATEGORY";
        public const string PaymentGateway       = "PAYMENT_GATEWAY";
        public const string AttendanceStatus         = "ATTENDANCE_STATUS";
        public const string SalaryComponent          = "SALARY_COMPONENT";
        public const string Marketplace              = "MARKETPLACE";
        public const string MarketplaceOrderStatus   = "MARKETPLACE_ORDER_STATUS";
        public const string ServiceJobStatus         = "SERVICE_JOB_STATUS";
        public const string VerticalPackCode         = "VERTICAL_PACK";
        public const string DrugSchedule             = "DRUG_SCHEDULE";
        public const string LoyaltyTransactionType   = "LOYALTY_TRANSACTION_TYPE";
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
        public const string VoucherJournal   = "VOUCHER_JOURNAL";
        public const string VoucherPayment   = "VOUCHER_PAYMENT";
        public const string VoucherReceipt   = "VOUCHER_RECEIPT";
        public const string VoucherContra    = "VOUCHER_CONTRA";
        public const string Bill              = "BILL";
        public const string DebitNote         = "DEBIT_NOTE";
        public const string PurchaseReturn    = "PURCHASE_RETURN";
        public const string SalesReturn       = "SALES_RETURN";
        public const string WarrantyClaim     = "WARRANTY_CLAIM";
        public const string Quotation         = "QUOTATION";
        public const string Delivery          = "DELIVERY";
        public const string FixedAsset        = "FIXED_ASSET";
        public const string PaymentTransaction = "PAYMENT_TRANSACTION";
        public const string WalletTopUp        = "WALLET_TOP_UP";
        public const string Employee           = "EMPLOYEE";
        public const string OnlineOrder        = "ONLINE_ORDER";
        public const string CustomerInquiry    = "CUSTOMER_INQUIRY";
        public const string ServiceJob         = "SERVICE_JOB";
        public const string DrugBatch          = "DRUG_BATCH";
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
        public const string VoucherJournal   = "VJ";
        public const string VoucherPayment   = "VP";
        public const string VoucherReceipt   = "VR";
        public const string VoucherContra    = "VC";
        public const string Bill              = "BILL";
        public const string DebitNote         = "DN";
        public const string PurchaseReturn    = "PR";
        public const string SalesReturn       = "SR";
        public const string WarrantyClaim     = "WC";
        public const string Quotation         = "QT";
        public const string Delivery          = "DEL";
        public const string FixedAsset        = "FA";
        public const string PaymentTransaction = "PT";
        public const string Employee           = "EMP";
        public const string OnlineOrder        = "ORD";
        public const string CustomerInquiry    = "INQ";
        public const string ServiceJob         = "SJ";
        public const string DrugBatch          = "BATCH";
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

    public static class NotificationCodes
    {
        public const string InvoiceFinalized = "INVOICE_FINALIZED";
        public const string InvoiceCancelled = "INVOICE_CANCELLED";
        public const string WalletCredited   = "WALLET_CREDITED";
        public const string WalletDebited    = "WALLET_DEBITED";
        public const string UserInvite       = "USER_INVITE";
        public const string PasswordReset    = "PASSWORD_RESET";
        public const string LowStock         = "LOW_STOCK_ALERT";
        public const string ShiftClosed      = "SHIFT_CLOSED";
    }

    public static class Performance
    {
        public const int SlowQueryThresholdMs = 500;
    }
}
