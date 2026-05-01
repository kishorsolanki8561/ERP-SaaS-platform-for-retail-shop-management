namespace ErpSaas.Shared.Messages;

public static class Errors
{
    public static class Auth
    {
        public const string UserNotFound          = "AUTH_001";
        public const string AccountInactive       = "AUTH_002";
        public const string AccountLocked         = "AUTH_003";
        public const string InvalidCredentials    = "AUTH_004";
        public const string InvalidRefreshToken   = "AUTH_005";
        public const string TotpRequired          = "AUTH_006";
        public const string InvalidTotpCode       = "AUTH_007";
        public const string BootstrapAlreadyDone  = "AUTH_008";

        public static string AccountLockedUntil(DateTimeOffset until)
            => $"{AccountLocked}:{until:O}";
    }

    public static class Shop
    {
        public const string CodeAlreadyExists     = "SHOP_001";
        public const string StarterPlanMissing    = "SHOP_002";

        public static string CodeConflict(string code)
            => $"{CodeAlreadyExists}:{code}";
    }

    public static class Masters
    {
        public const string CountryCodeExists     = "MASTERS_001";
        public const string StateCodeExists       = "MASTERS_002";
        public const string CityNameExists        = "MASTERS_003";
        public const string KeyRequired           = "MASTERS_004";

        public static string CountryConflict(string code)    => $"{CountryCodeExists}:{code}";
        public static string StateConflict(string code)      => $"{StateCodeExists}:{code}";
        public static string CityConflict(string name)       => $"{CityNameExists}:{name}";
    }

    public static class Crm
    {
        public const string CustomerPhoneExists   = "CRM_001";
        public const string CustomerNotFound      = "CRM_002";
        public const string GroupCodeExists       = "CRM_003";

        public static string PhoneConflict(string phone) => $"{CustomerPhoneExists}:{phone}";
        public static string GroupConflict(string code)  => $"{GroupCodeExists}:{code}";
    }

    public static class Inventory
    {
        public const string ProductNotFound       = "INV_001";
        public const string WarehouseCodeExists   = "INV_002";
        public const string ProductUnitNotFound   = "INV_003";

        public static string ProductConflict(long id)     => $"{ProductNotFound}:{id}";
        public static string WarehouseConflict(string c)  => $"{WarehouseCodeExists}:{c}";
        public static string UnitConflict(long id)        => $"{ProductUnitNotFound}:{id}";
    }

    public static class Billing
    {
        public const string InvoiceNotFound       = "BILL_001";
        public const string InvoiceNotDraft       = "BILL_002";
        public const string InvoiceAlreadyCancelled = "BILL_003";
    }

    public static class Admin
    {
        public const string ShopNotFound       = "ADMIN_001";
        public const string UserNotFound       = "ADMIN_002";
        public const string RoleNotFound       = "ADMIN_003";
        public const string RoleCodeTaken      = "ADMIN_004";
        public const string SystemRoleReadOnly = "ADMIN_005";
        public const string UserRoleNotFound   = "ADMIN_006";
    }

    public static class Wallet
    {
        public const string CustomerNotFound    = "WALLET_001";
        public const string InsufficientBalance = "WALLET_002";
        public const string InvalidAmount       = "WALLET_003";
    }

    public static class Shift
    {
        public const string NotFound   = "SHIFT_001";
        public const string AlreadyOpen = "SHIFT_002";
        public const string NotOpen    = "SHIFT_003";
    }

    public static class Metering
    {
        public const string QuotaExceeded = "METER_001";

        public static string QuotaConflict(string code) => $"{QuotaExceeded}:{code}";
    }

    public static class Accounting
    {
        public const string AccountNotFound             = "ACC_001";
        public const string AccountCodeExists           = "ACC_002";
        public const string SystemAccountReadOnly       = "ACC_003";
        public const string VoucherNotFound             = "ACC_004";
        public const string VoucherImbalanced           = "ACC_005";
        public const string VoucherAlreadyPosted        = "ACC_006";
        public const string VoucherNotPosted            = "ACC_007";
        public const string FinancialYearExists         = "ACC_008";
        public const string FinancialYearNotFound       = "ACC_009";
        public const string FinancialYearAlreadyClosed  = "ACC_010";
        public const string FinancialYearHasOpenVouchers = "ACC_011";
        public const string BankAccountExists           = "ACC_012";
        public const string BankStatementNotFound       = "ACC_013";
        public const string BankStatementAlreadyComplete = "ACC_014";
        public const string BankStatementLineNotFound   = "ACC_015";
        public const string BankStatementLineAlreadyMatched = "ACC_016";
        public const string ReconciliationRuleNotFound  = "ACC_017";
    }

    public static class Purchasing
    {
        public const string SupplierNotFound          = "PUR_001";
        public const string SupplierCodeExists        = "PUR_002";
        public const string PoNotFound                = "PUR_003";
        public const string PoNotDraft                = "PUR_004";
        public const string PoAlreadyCancelled        = "PUR_005";
        public const string BillNotFound              = "PUR_006";
        public const string BillNotDraft              = "PUR_007";
        public const string BillNotApproved           = "PUR_008";
        public const string BillAlreadyCancelled      = "PUR_009";
        public const string BillOverpayment               = "PUR_010";
        public const string PurchaseReturnNotFound         = "PUR_011";
        public const string PurchaseReturnNotDraft         = "PUR_012";
        public const string PurchaseReturnNotApproved      = "PUR_013";
        public const string PurchaseReturnAlreadyCancelled = "PUR_014";
        public const string DebitNoteAlreadyIssued         = "PUR_015";
    }

    public static class SalesReturns
    {
        public const string SalesReturnNotFound   = "SR_001";
        public const string SalesReturnNotDraft   = "SR_002";
        public const string SalesReturnCancelled  = "SR_003";
        public const string CreditNoteNotFound    = "SR_004";
        public const string CreditNoteNotIssued   = "SR_005";
        public const string CreditNoteExpired     = "SR_006";
        public const string CreditNoteInsufficient = "SR_007";
    }

    public static class Pricing
    {
        public const string DiscountRuleNotFound = "PRC_001";
        public const string ExtraChargeNotFound  = "PRC_002";
        public const string OfferNotFound        = "PRC_003";
        public const string OfferCodeExists      = "PRC_004";
    }

    public static class Warranty
    {
        public const string RegistrationNotFound  = "WRN_001";
        public const string SerialNumberExists    = "WRN_002";
        public const string WarrantyNotActive     = "WRN_003";
        public const string WarrantyExpired       = "WRN_004";
        public const string ClaimNotFound         = "WRN_005";
        public const string ClaimAlreadyClosed    = "WRN_006";
    }

    public static class Transport
    {
        public const string VehicleNotFound           = "TRN_001";
        public const string ProviderNotFound          = "TRN_002";
        public const string DeliveryNotFound          = "TRN_003";
        public const string DeliveryAlreadyDelivered  = "TRN_004";
        public const string LicensePlateExists        = "TRN_005";
    }

    public static class Quotations
    {
        public const string QuotationNotFound         = "QTN_001";
        public const string QuotationNotInDraft       = "QTN_002";
        public const string QuotationExpired          = "QTN_003";
        public const string SalesOrderNotFound        = "QTN_004";
        public const string SalesOrderAlreadyCancelled = "QTN_005";
        public const string DeliveryChallanNotFound   = "QTN_006";
        public const string DeliveryChallanAlreadyDispatched = "QTN_007";
    }

    public static class Files
    {
        public const string NotFound            = "FILE_001";
        public const string ExtensionNotAllowed = "FILE_002";
        public const string FileTooLarge        = "FILE_003";
        public const string StorageQuotaExceeded = "FILE_004";

        public static string ExtensionConflict(string ext) => $"{ExtensionNotAllowed}:{ext}";
        public static string SizeConflict(long max) => $"{FileTooLarge}:{max}";
    }
}
