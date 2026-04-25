using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Files;
using ErpSaas.Infrastructure.Data.Entities.Portal;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Infrastructure.Data;

public class PlatformDbContext(
    DbContextOptions<PlatformDbContext> options,
    AuditSaveChangesInterceptor auditInterceptor)
    : DbContext(options)
{
    // Masters
    public DbSet<DdlCatalog> DdlCatalogs => Set<DdlCatalog>();
    public DbSet<DdlItem> DdlItems => Set<DdlItem>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<State> States => Set<State>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<HsnSacCode> HsnSacCodes => Set<HsnSacCode>();

    // Identity
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserShop> UserShops => Set<UserShop>();
    public DbSet<UserSecurityToken> UserSecurityTokens => Set<UserSecurityToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    // Subscription
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<SubscriptionPlanFeature> SubscriptionPlanFeatures => Set<SubscriptionPlanFeature>();
    public DbSet<ShopSubscription> ShopSubscriptions => Set<ShopSubscription>();

    // Menu
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    // File Upload
    public DbSet<FileUploadConfig> FileUploadConfigs => Set<FileUploadConfig>();

    // Customer Portal
    public DbSet<PlatformCustomer> PlatformCustomers => Set<PlatformCustomer>();
    public DbSet<CustomerLink> CustomerLinks => Set<CustomerLink>();
    public DbSet<CustomerLoginSession> CustomerLoginSessions => Set<CustomerLoginSession>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(auditInterceptor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureMasters(modelBuilder);
        ConfigureIdentity(modelBuilder);
        ConfigureSubscription(modelBuilder);
        ConfigureMenu(modelBuilder);
        ConfigurePortal(modelBuilder);
        ConfigureFiles(modelBuilder);
    }

    private static void ConfigureMasters(ModelBuilder b)
    {
        b.Entity<DdlCatalog>(e =>
        {
            e.ToTable("DdlCatalog", schema: "masters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Key).IsUnique();
            e.HasMany(x => x.Items).WithOne(i => i.Catalog).HasForeignKey(i => i.CatalogId);
        });

        b.Entity<DdlItem>(e =>
        {
            e.ToTable("DdlItem", schema: "masters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.Property(x => x.Label).HasMaxLength(200).IsRequired();
            e.Property(x => x.ParentCode).HasMaxLength(100);
        });

        b.Entity<Country>(e =>
        {
            e.ToTable("Country", schema: "masters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(3).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.PhoneCode).HasMaxLength(10);
            e.Property(x => x.CurrencyCode).HasMaxLength(3);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<State>(e =>
        {
            e.ToTable("State", schema: "masters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(10).IsRequired();
            e.HasIndex(x => new { x.CountryId, x.Code }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.GstStateCode).HasMaxLength(2);
            e.HasOne(x => x.Country).WithMany(c => c.States).HasForeignKey(x => x.CountryId);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<City>(e =>
        {
            e.ToTable("City", schema: "masters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.StateId, x.Name }).IsUnique();
            e.HasOne(x => x.State).WithMany(s => s.Cities).HasForeignKey(x => x.StateId);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<Currency>(e =>
        {
            e.ToTable("Currency", schema: "masters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(3).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Symbol).HasMaxLength(10).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<HsnSacCode>(e =>
        {
            e.ToTable("HsnSacCode", schema: "masters");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(8).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.GstRate).HasPrecision(5, 2);
            e.Property(x => x.RowVersion).IsRowVersion();
        });
    }

    private static void ConfigureIdentity(ModelBuilder b)
    {
        b.Entity<Shop>(e =>
        {
            e.ToTable("Shop", schema: "identity");
            e.HasKey(x => x.Id);
            e.Property(x => x.ShopCode).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.ShopCode).IsUnique();
            e.Property(x => x.LegalName).HasMaxLength(200).IsRequired();
            e.Property(x => x.TradeName).HasMaxLength(200);
            e.Property(x => x.GstNumber).HasMaxLength(15);
            e.Property(x => x.AddressLine1).HasMaxLength(300);
            e.Property(x => x.AddressLine2).HasMaxLength(300);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.StateCode).HasMaxLength(3);
            e.Property(x => x.PinCode).HasMaxLength(10);
            e.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            e.Property(x => x.TimeZone).HasMaxLength(50).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<Branch>(e =>
        {
            e.ToTable("Branch", schema: "identity");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.AddressLine1).HasMaxLength(300);
            e.Property(x => x.AddressLine2).HasMaxLength(300);
            e.Property(x => x.City).HasMaxLength(100);
            e.Property(x => x.StateCode).HasMaxLength(3);
            e.Property(x => x.PinCode).HasMaxLength(10);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.GstNumber).HasMaxLength(15);
            e.HasIndex(x => x.ShopId);
            e.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId);
        });

        b.Entity<User>(e =>
        {
            e.ToTable("User", schema: "identity");
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(100);
            e.HasIndex(x => x.Username).IsUnique().HasFilter("[Username] IS NOT NULL");
            e.Property(x => x.Email).HasMaxLength(256);
            e.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            e.Property(x => x.Phone).HasMaxLength(20);
            e.HasIndex(x => x.Phone).IsUnique().HasFilter("[Phone] IS NOT NULL");
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(72).IsRequired();
            e.Property(x => x.TotpSecretEncrypted).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<UserShop>(e =>
        {
            e.ToTable("UserShop", schema: "identity");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.ShopId }).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.UserShops).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Shop).WithMany(s => s.UserShops).HasForeignKey(x => x.ShopId);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<UserSecurityToken>(e =>
        {
            e.ToTable("UserSecurityToken", schema: "identity");
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.SecurityTokens).HasForeignKey(x => x.UserId);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<Role>(e =>
        {
            e.ToTable("Role", schema: "identity");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.Code, x.ShopId }).IsUnique();
            e.Property(x => x.Label).HasMaxLength(200).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<Permission>(e =>
        {
            e.ToTable("Permission", schema: "identity");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Module).HasMaxLength(100).IsRequired();
            e.Property(x => x.Label).HasMaxLength(200).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<RolePermission>(e =>
        {
            e.ToTable("RolePermission", schema: "identity");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
            e.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId);
            e.HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId);
        });

        b.Entity<UserRole>(e =>
        {
            e.ToTable("UserRole", schema: "identity");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.ShopId, x.RoleId }).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.UserRoles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(r => r.UserRoles).HasForeignKey(x => x.RoleId);
        });
    }

    private static void ConfigureSubscription(ModelBuilder b)
    {
        b.Entity<SubscriptionPlan>(e =>
        {
            e.ToTable("SubscriptionPlan", schema: "identity");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Label).HasMaxLength(200).IsRequired();
            e.Property(x => x.MonthlyPrice).HasPrecision(18, 2);
            e.Property(x => x.AnnualPrice).HasPrecision(18, 2);
            e.Property(x => x.MaxUsers).HasDefaultValue(5);
            e.Property(x => x.MaxProducts).HasDefaultValue(500);
            e.Property(x => x.MaxInvoicesPerMonth).HasDefaultValue(1000);
            e.Property(x => x.StorageQuotaMb).HasDefaultValue(500);
            e.Property(x => x.SmsQuotaPerMonth).HasDefaultValue(100);
            e.Property(x => x.EmailQuotaPerMonth).HasDefaultValue(500);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<SubscriptionPlanFeature>(e =>
        {
            e.ToTable("SubscriptionPlanFeature", schema: "identity");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PlanId, x.FeatureCode }).IsUnique();
            e.Property(x => x.FeatureCode).HasMaxLength(100).IsRequired();
            e.HasOne(x => x.Plan).WithMany(p => p.Features).HasForeignKey(x => x.PlanId);
        });

        b.Entity<ShopSubscription>(e =>
        {
            e.ToTable("ShopSubscription", schema: "identity");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Shop).WithMany(s => s.Subscriptions).HasForeignKey(x => x.ShopId);
            e.HasOne(x => x.Plan).WithMany(p => p.ShopSubscriptions).HasForeignKey(x => x.PlanId);
            e.Property(x => x.RowVersion).IsRowVersion();
        });
    }

    private static void ConfigureMenu(ModelBuilder b)
    {
        b.Entity<MenuItem>(e =>
        {
            e.ToTable("MenuItem", schema: "menu");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Label).HasMaxLength(200).IsRequired();
            e.Property(x => x.Icon).HasMaxLength(100);
            e.Property(x => x.Route).HasMaxLength(300);
            e.Property(x => x.RequiredPermission).HasMaxLength(100);
            e.Property(x => x.RequiredFeature).HasMaxLength(100);
            e.HasOne(x => x.Parent).WithMany(m => m.Children).HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.RowVersion).IsRowVersion();
        });
    }

    private static void ConfigureFiles(ModelBuilder b)
    {
        b.Entity<FileUploadConfig>(e =>
        {
            e.ToTable("FileUploadConfig", schema: "files");
            e.HasKey(x => x.Id);
            e.Property(x => x.Purpose).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Purpose).IsUnique();
            e.Property(x => x.AllowedExtensions).HasMaxLength(500).IsRequired();
        });
    }

    private static void ConfigurePortal(ModelBuilder b)
    {
        b.Entity<PlatformCustomer>(e =>
        {
            e.ToTable("PlatformCustomer", schema: "portal");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256);
            e.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
            e.Property(x => x.Phone).HasMaxLength(20);
            e.HasIndex(x => x.Phone).IsUnique().HasFilter("[Phone] IS NOT NULL");
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        });

        b.Entity<CustomerLink>(e =>
        {
            e.ToTable("CustomerLink", schema: "portal");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.PlatformCustomerId, x.ShopId, x.TenantCustomerId }).IsUnique();
            e.HasOne(x => x.PlatformCustomer).WithMany(c => c.CustomerLinks)
                .HasForeignKey(x => x.PlatformCustomerId);
        });

        b.Entity<CustomerLoginSession>(e =>
        {
            e.ToTable("CustomerLoginSession", schema: "portal");
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.Property(x => x.Purpose).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.PlatformCustomer).WithMany(c => c.LoginSessions)
                .HasForeignKey(x => x.PlatformCustomerId);
        });
    }
}
