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
        public const string ShopNotFound = "ADMIN_001";
    }
}
