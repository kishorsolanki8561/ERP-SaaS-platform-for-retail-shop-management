using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Marketing;
using ErpSaas.Infrastructure.Data.Entities.Masters;
using ErpSaas.Infrastructure.Data.Entities.Menu;
using ErpSaas.Infrastructure.Data.Entities.Files;
using ErpSaas.Infrastructure.Data.Entities.Portal;
using ErpSaas.Infrastructure.Data.Entities.Replication;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Infrastructure.Data.Entities.Verticals;
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

    // Shop feature overrides + per-user permission overrides
    public DbSet<ShopFeatureOverride> ShopFeatureOverrides => Set<ShopFeatureOverride>();
    public DbSet<UserPermissionOverride> UserPermissionOverrides => Set<UserPermissionOverride>();

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

    // Marketing & Leads
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<MarketingContent> MarketingContents => Set<MarketingContent>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();

    // On-prem replication
    public DbSet<OnPremDeployment> OnPremDeployments => Set<OnPremDeployment>();
    public DbSet<ReplicationLog> ReplicationLogs => Set<ReplicationLog>();
    public DbSet<ConflictArchive> ConflictArchives => Set<ConflictArchive>();
    public DbSet<ChangeTrackingLog> ChangeTrackingLogs => Set<ChangeTrackingLog>();

    // Vertical packs
    public DbSet<VerticalPack> VerticalPacks => Set<VerticalPack>();

    // Shop registration requests
    public DbSet<ShopRegistrationRequest> ShopRegistrationRequests => Set<ShopRegistrationRequest>();

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
        ConfigureMarketing(modelBuilder);
        ConfigureReplication(modelBuilder);
        ConfigureVerticals(modelBuilder);
        ConfigureRegistration(modelBuilder);
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

        b.Entity<ShopFeatureOverride>(e =>
        {
            e.ToTable("ShopFeatureOverride", schema: "identity");
            e.HasKey(x => x.Id);
            e.Property(x => x.FeatureCode).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.FeatureCode }).IsUnique();
            e.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId);
        });

        b.Entity<UserPermissionOverride>(e =>
        {
            e.ToTable("UserPermissionOverride", schema: "identity");
            e.HasKey(x => x.Id);
            e.Property(x => x.PermissionCode).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.UserId, x.ShopId, x.PermissionCode }).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId);
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

    private static void ConfigureMarketing(ModelBuilder b)
    {
        b.Entity<Lead>(e =>
        {
            e.ToTable("Lead", schema: "marketing");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            e.Property(x => x.BusinessName).HasMaxLength(200);
            e.Property(x => x.CityCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.StateCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.VerticalCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.Message).HasMaxLength(2000);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.UtmSource).HasMaxLength(200);
            e.Property(x => x.UtmCampaign).HasMaxLength(200);
            e.Property(x => x.Source).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.AssignedUserId);
        });

        b.Entity<MarketingContent>(e =>
        {
            e.ToTable("MarketingContent", schema: "marketing");
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(200).IsRequired();
            e.Property(x => x.Locale).HasMaxLength(10).IsRequired();
            e.HasIndex(x => new { x.Key, x.Locale }).IsUnique();
            e.Property(x => x.Title).HasMaxLength(400);
            e.Property(x => x.Body).IsRequired();
        });

        b.Entity<BlogPost>(e =>
        {
            e.ToTable("BlogPost", schema: "marketing");
            e.HasKey(x => x.Id);
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Title).HasMaxLength(400).IsRequired();
            e.Property(x => x.Body).IsRequired();
            e.Property(x => x.AuthorName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Tags).HasMaxLength(500);
            e.HasIndex(x => x.IsPublished);
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

    private static void ConfigureReplication(ModelBuilder b)
    {
        b.Entity<OnPremDeployment>(e =>
        {
            e.ToTable("OnPremDeployment", schema: "replication");
            e.HasKey(x => x.Id);
            e.Property(x => x.DeploymentId).HasMaxLength(100).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.DeploymentId }).IsUnique();
            e.Property(x => x.ShopLocalEndpoint).HasMaxLength(500).IsRequired();
            e.Property(x => x.PublicKey).HasMaxLength(2000).IsRequired();
            e.Property(x => x.SoftwareVersion).HasMaxLength(50).IsRequired();
            e.Property(x => x.Mode).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.ShopId);
        });

        b.Entity<ReplicationLog>(e =>
        {
            e.ToTable("ReplicationLog", schema: "replication");
            e.HasKey(x => x.Id);
            e.Property(x => x.DeploymentId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(x => x.ErrorSummary).HasMaxLength(2000);
            e.HasIndex(x => new { x.ShopId, x.DeploymentId });
            e.HasIndex(x => x.StartedAtUtc);
        });

        b.Entity<ConflictArchive>(e =>
        {
            e.ToTable("ConflictArchive", schema: "replication");
            e.HasKey(x => x.Id);
            e.Property(x => x.DeploymentId).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Strategy).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(30).IsRequired();
            e.Property(x => x.ResolutionNote).HasMaxLength(2000);
            e.HasIndex(x => new { x.ShopId, x.Outcome });
            e.HasIndex(x => x.DeploymentId);
        });

        b.Entity<ChangeTrackingLog>(e =>
        {
            e.ToTable("ChangeTrackingLog", schema: "replication");
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Operation).HasMaxLength(20).IsRequired();
            e.Property(x => x.OriginDeploymentId).HasMaxLength(100);
            e.HasIndex(x => new { x.ShopId, x.EntityName, x.EntityId });
            e.HasIndex(x => x.VersionNumber);
        });
    }

    private static void ConfigureRegistration(ModelBuilder b)
    {
        b.Entity<ShopRegistrationRequest>(e =>
        {
            e.ToTable("ShopRegistrationRequest", schema: "identity");
            e.HasKey(x => x.Id);
            e.Property(x => x.ShopCode).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.ShopCode);
            e.Property(x => x.LegalName).HasMaxLength(200).IsRequired();
            e.Property(x => x.TradeName).HasMaxLength(200);
            e.Property(x => x.GstNumber).HasMaxLength(15);
            e.Property(x => x.AdminEmail).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.AdminEmail);
            e.Property(x => x.AdminDisplayName).HasMaxLength(200).IsRequired();
            e.Property(x => x.PasswordHashSnapshot).HasMaxLength(72).IsRequired();
            e.Property(x => x.ContactPhone).HasMaxLength(20);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Status);
            e.Property(x => x.RejectionReason).HasMaxLength(1000);
        });
    }

    private static void ConfigureVerticals(ModelBuilder b)
    {
        b.Entity<VerticalPack>(e =>
        {
            e.ToTable("VerticalPack", schema: "verticals");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.FeatureFlagsCsv).HasMaxLength(2000).IsRequired();
            e.Property(x => x.DefaultInvoiceTemplateCode).HasMaxLength(100);
            e.Property(x => x.IconClass).HasMaxLength(100);
        });
    }
}
