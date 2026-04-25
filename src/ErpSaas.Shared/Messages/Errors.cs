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
