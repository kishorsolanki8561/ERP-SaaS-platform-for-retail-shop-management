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
        public const string TopUpNotFound       = "WALLET_004";
        public const string TopUpNotPending     = "WALLET_005";
        public const string RefundSplitMismatch = "WALLET_006";
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

    public static class Payment
    {
        public const string GatewayAccountNotFound    = "PAY_001";
        public const string TransactionNotFound       = "PAY_002";
        public const string TransactionAlreadyFinal   = "PAY_003";
        public const string RefundRequiresSuccess     = "PAY_004";
        public const string RefundExceedsAmount       = "PAY_005";
        public const string ExceptionNotFound         = "PAY_006";
        public const string ExceptionAlreadyResolved  = "PAY_007";
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

    public static class Hardware
    {
        public const string DeviceNotFound            = "HW_001";
        public const string DeviceAlreadyRegistered   = "HW_002";
        public const string LabelTemplateNotFound     = "HW_003";
        public const string ReceiptTemplateNotFound   = "HW_004";
    }

    public static class Hr
    {
        public const string EmployeeNotFound          = "HR_001";
        public const string EmployeeCodeExists        = "HR_002";
        public const string AttendanceAlreadyExists   = "HR_003";
        public const string AttendanceNotFound        = "HR_004";
        public const string LeaveTypeNotFound         = "HR_005";
        public const string LeaveTypeCodeExists       = "HR_006";
        public const string LeaveRequestNotFound      = "HR_007";
        public const string LeaveRequestNotPending    = "HR_008";
        public const string InsufficientLeaveBalance  = "HR_009";
        public const string PayrollNotFound           = "HR_010";
        public const string PayrollAlreadyExists      = "HR_011";
        public const string PayrollNotDraft           = "HR_012";
        public const string PayrollNotApproved        = "HR_013";
    }

    public static class Marketplace
    {
        public const string AccountNotFound        = "MKT_001";
        public const string OrderNotFound          = "MKT_002";
        public const string OrderAlreadyIngested   = "MKT_003";
        public const string OrderAlreadyConverted  = "MKT_004";
        public const string OrderCancelled         = "MKT_005";
        public const string MappingAlreadyExists   = "MKT_006";
    }

    public static class CustomerPortal
    {
        public const string OrderNotFound         = "PORTAL_010";
        public const string OrderNotPending       = "PORTAL_011";
        public const string InquiryNotFound       = "PORTAL_020";
        public const string InquiryAlreadyClosed  = "PORTAL_021";
        public const string CustomerNotFound      = "PORTAL_030";
    }

    public static class Subscription
    {
        public const string PlanNotFound          = "SUB_001";
        public const string NoActiveSubscription  = "SUB_002";
        public const string AlreadyOnPlan         = "SUB_003";
        public const string CannotCancelFree      = "SUB_004";
        public const string InvalidBillingCycle   = "SUB_005";
    }

    public static class Lead
    {
        public const string NotFound                       = "LEAD_001";
        public const string UserNotFound                   = "LEAD_002";
        public const string InvalidStatus                  = "LEAD_003";
        public const string AlreadyConverted               = "LEAD_004";
        public const string CannotMarkConvertedWithoutShop = "LEAD_005";
        public const string SlugExists                     = "LEAD_006";
        public const string ContentNotFound                = "LEAD_007";
        public const string BlogPostNotFound               = "LEAD_008";
        public const string BlogPostAlreadyPublished       = "LEAD_009";
    }

    public static class PlatformAdmin
    {
        public const string PlanNotFound     = "PADMIN_001";
        public const string PlanCodeExists   = "PADMIN_002";
        public const string ShopNotFound     = "PADMIN_003";
        public const string ShopAlreadySuspended  = "PADMIN_004";
        public const string ShopAlreadyActive     = "PADMIN_005";
    }

    public static class ApiAccess
    {
        public const string KeyNotFound              = "APIACCESS_001";
        public const string KeyAlreadyRevoked        = "APIACCESS_002";
        public const string EndpointNotFound         = "APIACCESS_003";
        public const string InvalidWebhookUrl        = "APIACCESS_004";
        public const string DeliveryNotFound         = "APIACCESS_005";
        public const string DeliveryAlreadySucceeded = "APIACCESS_006";
    }

    public static class Sync
    {
        public const string DeviceNotFound            = "SYNC_001";
        public const string InvalidDeviceType         = "SYNC_002";
        public const string AllocationNotFound        = "SYNC_003";
        public const string AllocationAlreadyReleased = "SYNC_004";
    }

    public static class OnPrem
    {
        public const string NotFound         = "ONPREM_001";
        public const string ConflictNotFound = "ONPREM_002";
        public const string AlreadyResolved  = "ONPREM_003";
    }

    public static class Verticals
    {
        public const string PackNotFound      = "VERT_001";
        public const string PackInactive      = "VERT_002";
    }

    public static class ServiceJobs
    {
        public const string NotFound                = "SJ_001";
        public const string InvalidStatusTransition = "SJ_002";
        public const string AlreadyDelivered        = "SJ_003";
        public const string PartProductNotFound     = "SJ_004";
    }

    public static class Medical
    {
        public const string BatchNotFound     = "MED_001";
        public const string BatchExpired      = "MED_002";
        public const string BatchNumberExists = "MED_003";
    }

    public static class Grocery
    {
        public const string ProgramNotFound    = "GRC_001";
        public const string InsufficientPoints = "GRC_002";
    }
}
