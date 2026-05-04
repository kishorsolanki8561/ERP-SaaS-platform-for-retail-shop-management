using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Shared.Seeds;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;

namespace ErpSaas.Tests.Integration.Fixtures;

/// <summary>
/// Shared test fixture. Inherits <see cref="WebApplicationFactory{TEntryPoint}"/> so that
/// <c>ConfigureWebHost</c> is called at exactly the right time (after all application
/// service registrations, before <c>builder.Build()</c> finalises the DI container).
///
/// Three problems this fixture solves, and why each solution is needed:
///
/// 1. Connection strings — <c>AddInfrastructure</c> registers <c>AddDbContext</c> lambdas
///    that call <c>configuration.GetConnectionString()</c> lazily (evaluated when the
///    <c>DbContextOptions&lt;T&gt;</c> is first resolved from DI, i.e. during
///    <c>InitializeAsync</c>). <c>ConfigureAppConfiguration</c> adds an in-memory provider
///    with the Testcontainers strings to the live <c>ConfigurationManager</c> before those
///    lambdas run, so they see the correct values. <c>RemoveAll + AddDbContext</c> in
///    <c>ConfigureTestServices</c> is kept as a second layer of defence.
///
/// 2. JWT — <c>AddIdentityModule</c> reads <c>Jwt:Secret/Issuer/Audience</c> as local
///    variables at service-registration time, before any configuration override can run.
///    <c>PostConfigure&lt;JwtBearerOptions&gt;</c> patches the already-registered options
///    with the test values after all <c>Configure</c> calls complete.
///
/// 3. Silent startup failures — <c>AppInitializationExtensions.InitializeAsync</c> wraps
///    all migrations in a single try/catch that swallows exceptions (intentional for dev
///    mode). If any <c>MigrateAsync()</c> call fails during startup the database is never
///    created, causing "Cannot open database" failures in tests. After <c>CreateClient()</c>
///    builds the host, <c>IAsyncLifetime.InitializeAsync</c> explicitly migrates all
///    databases using <c>Services.CreateAsyncScope()</c> — a scope guaranteed to use the
///    <c>ConfigureTestServices</c> overrides with the correct Testcontainers addresses.
///    <c>MigrateAsync()</c> is idempotent so this is a no-op when startup succeeded.
///
/// Use as xUnit <c>ICollectionFixture&lt;IntegrationTestFixture&gt;</c>.
/// </summary>
public sealed class IntegrationTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Only started when INTEGRATION_MSSQL_HOST is not set (local dev).
    // In CI the GitHub Actions service container provides SQL Server instead.
    private readonly MsSqlContainer _container = new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Test@1234!StrongPass")
        .Build();

    private bool _usingContainer;

    // Deterministic JWT secret for tests — never a real secret
    private const string TestJwtSecret = "TestOnly_SuperSecretKey_AtLeast32Chars_DoNotUse";
    private const string TestIssuer    = "test-issuer";
    private const string TestAudience  = "test-audience";

    // Set in InitializeAsync before CreateClient() triggers ConfigureWebHost
    private string _platformCs    = null!;
    private string _tenantCs      = null!;
    private string _analyticsCs   = null!;
    private string _logCs         = null!;
    private string _notifsCs      = null!;
    private string _marketplaceCs = null!;
    private string _syncCs        = null!;

    // ── WebApplicationFactory override ────────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Layer 1 — patch IConfiguration with Testcontainers connection strings.
        // The WebApplicationBuilder.Configuration is a live ConfigurationManager.
        // AddDbContext lambdas in AddInfrastructure evaluate configuration.GetConnectionString()
        // lazily when DbContextOptions<T> is first resolved (during InitializeAsync, after Build).
        // Adding an in-memory provider here ensures those lambdas see the correct values.
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDb"]          = _platformCs,
                ["ConnectionStrings:TenantDb"]            = _tenantCs,
                ["ConnectionStrings:AnalyticsDb"]         = _analyticsCs,
                ["ConnectionStrings:LogDb"]               = _logCs,
                ["ConnectionStrings:NotificationsDb"]     = _notifsCs,
                ["ConnectionStrings:MarketplaceEventsDb"] = _marketplaceCs,
                ["ConnectionStrings:SyncDb"]              = _syncCs,
            });
        });

        // Layer 2 — replace DbContextOptions<T> DI descriptors and fix JWT options.
        // ConfigureTestServices runs AFTER all application ConfigureServices calls.
        builder.ConfigureTestServices(services =>
        {
            // Belt: replace DbContextOptions descriptors directly in the DI container.
            services.RemoveAll<DbContextOptions<PlatformDbContext>>();
            services.RemoveAll<DbContextOptions<TenantDbContext>>();
            services.RemoveAll<DbContextOptions<AnalyticsDbContext>>();
            services.RemoveAll<DbContextOptions<LogDbContext>>();
            services.RemoveAll<DbContextOptions<NotificationsDbContext>>();
            services.RemoveAll<DbContextOptions<MarketplaceEventsDbContext>>();
            services.RemoveAll<DbContextOptions<SyncDbContext>>();

            services.AddDbContext<PlatformDbContext>(opts =>
                opts.UseSqlServer(_platformCs,
                    sql => sql.MigrationsAssembly(typeof(PlatformDbContext).Assembly.FullName)));
            services.AddDbContext<TenantDbContext>(opts =>
                opts.UseSqlServer(_tenantCs,
                    sql => sql.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName)));
            services.AddDbContext<AnalyticsDbContext>(opts =>
                opts.UseSqlServer(_analyticsCs,
                    sql => sql.MigrationsAssembly(typeof(AnalyticsDbContext).Assembly.FullName)));
            services.AddDbContext<LogDbContext>(opts =>
                opts.UseSqlServer(_logCs,
                    sql => sql.MigrationsAssembly(typeof(LogDbContext).Assembly.FullName)));
            services.AddDbContext<NotificationsDbContext>(opts =>
                opts.UseSqlServer(_notifsCs,
                    sql => sql.MigrationsAssembly(typeof(NotificationsDbContext).Assembly.FullName)));
            services.AddDbContext<MarketplaceEventsDbContext>(opts =>
                opts.UseSqlServer(_marketplaceCs,
                    sql => sql.MigrationsAssembly(typeof(MarketplaceEventsDbContext).Assembly.FullName)));
            services.AddDbContext<SyncDbContext>(opts =>
                opts.UseSqlServer(_syncCs,
                    sql => sql.MigrationsAssembly(typeof(SyncDbContext).Assembly.FullName)));

            // Fix JWT: AddIdentityModule captures Jwt:Secret/Issuer/Audience as local variables
            // at service-registration time, before ConfigureAppConfiguration overrides are
            // applied. PostConfigure runs after all Configure calls and patches the options
            // that are actually used by the JWT middleware at request time.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
            {
                opts.TokenValidationParameters.ValidIssuer          = TestIssuer;
                opts.TokenValidationParameters.ValidAudience        = TestAudience;
                opts.TokenValidationParameters.IssuerSigningKey     = key;
                opts.TokenValidationParameters.ValidateIssuerSigningKey = true;
            });
        });
    }

    // ── IAsyncLifetime ────────────────────────────────────────────────────────

    async Task IAsyncLifetime.InitializeAsync()
    {
        const string pass = "Test@1234!StrongPass";
        string host;
        int port;

        var ciHost = Environment.GetEnvironmentVariable("INTEGRATION_MSSQL_HOST");
        if (!string.IsNullOrEmpty(ciHost))
        {
            // CI path — GitHub Actions service container is already health-checked
            // (sqlcmd SELECT 1 must pass before the step runs), so we just read the
            // address from env vars that the workflow sets in the integration-tests step.
            host = ciHost;
            port = int.TryParse(Environment.GetEnvironmentVariable("INTEGRATION_MSSQL_PORT"), out var p) ? p : 1433;
            _usingContainer = false;
        }
        else
        {
            // Local dev path — spin up Testcontainers and wait for SQL Server to be
            // ready (Testcontainers 4.x only waits for TCP; we probe with SELECT 1).
            await _container.StartAsync();
            host = _container.Hostname;
            port = _container.GetMappedPublicPort(1433);
            await WaitForSqlServerReadyAsync(host, port, pass);
            _usingContainer = true;
        }

        string Cs(string db) =>
            $"Server={host},{port};Database={db};User Id=sa;Password={pass};" +
            "TrustServerCertificate=True;MultipleActiveResultSets=True";

        // Populate before CreateClient() so ConfigureWebHost closes over valid addresses.
        _platformCs    = Cs("ErpTest_Platform");
        _tenantCs      = Cs("ErpTest_Tenant");
        _analyticsCs   = Cs("ErpTest_Analytics");
        _logCs         = Cs("ErpTest_Log");
        _notifsCs      = Cs("ErpTest_Notifications");
        _marketplaceCs = Cs("ErpTest_MarketplaceEvents");
        _syncCs        = Cs("ErpTest_Sync");

        // Inject as environment variables BEFORE CreateClient() triggers the test host build.
        // ASP.NET Core reads env vars with __ as the hierarchy separator, so
        // ConnectionStrings__TenantDb maps to ConnectionStrings:TenantDb in IConfiguration.
        // This beats appsettings.json at the config source level — no DI patching required —
        // and guarantees that the AddDbContext<TenantDbContext> lambda inside AddInfrastructure
        // receives the Testcontainers address when the options factory is first invoked.
        Environment.SetEnvironmentVariable("ConnectionStrings__PlatformDb",          _platformCs);
        Environment.SetEnvironmentVariable("ConnectionStrings__TenantDb",            _tenantCs);
        Environment.SetEnvironmentVariable("ConnectionStrings__AnalyticsDb",         _analyticsCs);
        Environment.SetEnvironmentVariable("ConnectionStrings__LogDb",               _logCs);
        Environment.SetEnvironmentVariable("ConnectionStrings__NotificationsDb",     _notifsCs);
        Environment.SetEnvironmentVariable("ConnectionStrings__MarketplaceEventsDb", _marketplaceCs);
        Environment.SetEnvironmentVariable("ConnectionStrings__SyncDb",              _syncCs);

        // Warm up: triggers ConfigureWebHost → Program.cs startup →
        // migrations and seeds — all against the Testcontainers SQL Server.
        _ = CreateClient();

        // Explicit migration safety net.
        // Program.cs wraps all of InitializeAsync in a single try/catch that
        // swallows every exception and logs it.  If any DbContext's MigrateAsync()
        // fails during startup (wrong connection string captured before the env-var
        // override takes effect, or any other transient issue) the database simply
        // won't exist and every test that opens a connection will fail with
        // "Cannot open database".  Running MigrateAsync() here, after CreateClient()
        // has already built the test host, guarantees that we use the DI container
        // produced by ConfigureTestServices — which is unconditionally wired to the
        // Testcontainers addresses stored in the _*Cs fields above.
        // MigrateAsync() is idempotent: if the migration was already applied by
        // startup it becomes a fast no-op.
        await using var migrationScope = Services.CreateAsyncScope();
        var msp = migrationScope.ServiceProvider;
        await msp.GetRequiredService<PlatformDbContext>().Database.MigrateAsync();
        await msp.GetRequiredService<TenantDbContext>().Database.MigrateAsync();
        await msp.GetRequiredService<AnalyticsDbContext>().Database.MigrateAsync();
        await msp.GetRequiredService<LogDbContext>().Database.MigrateAsync();
        await msp.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
        await msp.GetRequiredService<MarketplaceEventsDbContext>().Database.MigrateAsync();
        await msp.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
    }

    /// <summary>
    /// Polls the SQL Server container with <c>SELECT 1</c> on <c>master</c> until
    /// the engine accepts connections.  Testcontainers only waits for the TCP port;
    /// SQL Server continues initializing after the port opens.
    /// </summary>
    private static async Task WaitForSqlServerReadyAsync(string host, int port, string password,
        int maxAttempts = 30, int delayMs = 2_000)
    {
        var masterCs = $"Server={host},{port};Database=master;User Id=sa;Password={password};" +
                       "TrustServerCertificate=True;Connect Timeout=5;";

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var conn = new SqlConnection(masterCs);
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                await cmd.ExecuteScalarAsync();
                return;
            }
            catch
            {
                if (attempt == maxAttempts)
                    throw new InvalidOperationException(
                        $"SQL Server container was not ready after {maxAttempts * delayMs / 1000} seconds.");
                await Task.Delay(delayMs);
            }
        }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        // Dispose the WebApplicationFactory (stops the test server).
        // WebApplicationFactory.Dispose() is idempotent so xUnit's own
        // IDisposable.Dispose() call after this is safe.
        Dispose();
        if (_usingContainer)
            await _container.DisposeAsync();

        // Remove env vars so they do not leak into any subsequent test process.
        foreach (var key in new[]
        {
            "ConnectionStrings__PlatformDb",
            "ConnectionStrings__TenantDb",
            "ConnectionStrings__AnalyticsDb",
            "ConnectionStrings__LogDb",
            "ConnectionStrings__NotificationsDb",
            "ConnectionStrings__MarketplaceEventsDb",
            "ConnectionStrings__SyncDb",
        })
            Environment.SetEnvironmentVariable(key, null);
    }

    // ── Public helpers used by test classes ───────────────────────────────────

    /// <summary>
    /// Returns an HTTP client authenticated as the given shop.
    /// Pass permissions=["*"] (default) to bypass all permission checks via is_platform_admin.
    /// Pass features to populate the "feats" claim checked by FeatureAuthorizationHandler.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(
        long shopId = 1,
        string[]? permissions = null,
        string role = "Admin",
        string[]? features = null)
    {
        var token = GenerateJwt(shopId, permissions ?? ["*"], role, features ?? []);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Returns an HTTP client authenticated with a limited set of permissions and no feature flags.
    /// Use this for permission-gate failure tests.
    /// </summary>
    public HttpClient CreateLimitedClient(long shopId = 1, string permissionCode = "None.None")
        => CreateAuthenticatedClient(shopId, [permissionCode], features: []);

    /// <summary>
    /// Returns an HTTP client authenticated with all permissions but the given feature flags
    /// explicitly absent. Use this for subscription-gate failure tests.
    /// The JWT uses the <c>perms</c> claim (not <c>is_platform_admin</c>) so that
    /// <see cref="FeatureAuthorizationHandler"/> is NOT bypassed and feature-gate 403 tests work.
    /// </summary>
    public HttpClient CreateNoFeatureClient(long shopId = 1)
    {
        // Generate token with all common permissions via perms claim but NO features.
        // This lets the FeatureAuthorizationHandler run (it won't be bypassed by is_platform_admin).
        var allPerms = new[]
        {
            "Dashboard.View", "Billing.View", "Billing.Create", "Billing.Edit", "Billing.Cancel",
            "Inventory.View", "Inventory.Manage", "Crm.View", "Crm.Create", "Crm.Edit",
            "Wallet.View", "Wallet.Credit", "Wallet.Debit", "Wallet.TopUp",
            "Shift.View", "Shift.Open", "Shift.Close",
            "Accounting.View", "Accounting.CreateVoucher", "Accounting.PostVoucher",
            "Purchasing.View", "Purchasing.ManageSuppliers", "Purchasing.CreatePurchaseOrder",
            "SalesReturns.Create", "SalesReturns.Approve",
            "Reports.ViewSales", "Reports.ViewAccounting", "Reports.ViewGst", "Reports.Export",
            "Warranty.View", "Warranty.Manage",
            "Pricing.View", "Pricing.Manage",
            "Transport.View", "Transport.Manage",
            "Quotation.View", "Quotation.Create",
            "Payment.View", "Payment.Configure", "Payment.Initiate",
            "HR.View", "HR.Manage",
            "Marketplace.View", "Marketplace.Manage",
            "Files.View", "Files.Upload",
            "ShopProfile.View", "ShopProfile.Edit",
            "Users.View", "Users.Manage",
            "Hardware.CashDrawer", "Device.Configure", "Device.Register", "Device.Manage",
            "Integration.ManageApiKeys", "Integration.ManageWebhooks", "Integration.ViewDeliveries",
            "OnlineOrder.View", "OnlineOrder.Manage",
            "Sync.View", "Sync.ResolveException",
        };
        var token = GenerateJwtWithPermsClaim(shopId, allPerms, []);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient CreateUnauthenticatedClient() => CreateClient();

    /// <summary>Opens a new DI scope for direct service access in tests.</summary>
    public AsyncServiceScope CreateScope() => Services.CreateAsyncScope();

    public Microsoft.Data.SqlClient.SqlConnection OpenTenantConnection()
    {
        var scope    = Services.CreateScope();
        var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var conn     = new Microsoft.Data.SqlClient.SqlConnection(
            tenantDb.Database.GetConnectionString()!);
        conn.Open();
        return conn;
    }

    public Microsoft.Data.SqlClient.SqlConnection OpenPlatformConnection()
    {
        var scope = Services.CreateScope();
        var db    = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var conn  = new Microsoft.Data.SqlClient.SqlConnection(
            db.Database.GetConnectionString()!);
        conn.Open();
        return conn;
    }

    // ── Seed helpers for tests that need real DB entities ─────────────────────

    /// <summary>
    /// Seeds a Shop + User in PlatformDbContext so that the real AuthController.LoginAsync
    /// can authenticate them. Returns (shopId, email, password).
    /// </summary>
    public async Task<(long shopId, string email, string password)> SeedTestUserAsync(
        string? email = null,
        string? password = null)
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        email    ??= $"test-{uniqueSuffix}@integration.test";
        password ??= "TestPass@123";

        await using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var shop = new Shop
        {
            ShopCode   = $"SHOP-{uniqueSuffix}",
            LegalName  = $"Integration Test Shop {uniqueSuffix}",
            IsActive   = true,
            CurrencyCode = "INR",
            TimeZone   = "Asia/Kolkata",
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Shops.Add(shop);
        await db.SaveChangesAsync();

        var user = new User
        {
            Email        = email,
            DisplayName  = $"Test User {uniqueSuffix}",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4),
            IsActive     = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.UserShops.Add(new UserShop
        {
            UserId       = user.Id,
            ShopId       = shop.Id,
            IsActive     = true,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        return (shop.Id, email, password);
    }

    // ── Tenant seed helper ────────────────────────────────────────────────────

    /// <summary>
    /// Seeds per-tenant data (COA, default catalog, etc.) for the given shopId.
    /// Must be called before accounting tests that use GetFirstAccountGroupIdAsync().
    /// </summary>
    public async Task SeedTenantDataAsync(long shopId)
    {
        await using var scope = Services.CreateAsyncScope();
        var seeders = scope.ServiceProvider.GetServices<ITenantSeeder>()
            .OrderBy(s => s.Order);
        foreach (var seeder in seeders)
            await seeder.SeedAsync(shopId);
    }

    // ── JWT helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a JWT that always uses the <c>perms</c> claim (never <c>is_platform_admin</c>).
    /// This ensures FeatureAuthorizationHandler is not bypassed, allowing feature-gate tests to
    /// assert 403 when a feature is absent.
    /// </summary>
    private static string GenerateJwtWithPermsClaim(long shopId, string[] permissions, string[] features)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, shopId.ToString()),
            new("shop_id", shopId.ToString()),
            new("role", "Admin"),
            new("perms", string.Join(',', permissions)),
        };
        if (features.Length > 0)
            claims.Add(new Claim("feats", string.Join(',', features)));

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TestIssuer, audience: TestAudience,
            claims: claims, expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateJwt(long shopId, string[] permissions, string role, string[] features)
    {
        // TenantContextMiddleware reads "shop_id" for ShopId and numeric "sub" for CurrentUserId.
        // PermissionAuthorizationHandler reads "perms" as a comma-separated list.
        // FeatureAuthorizationHandler reads "feats" as a comma-separated list.
        // is_platform_admin = "true" bypasses both permission and feature checks.
        var isAdmin = permissions.Length == 1 && permissions[0] == "*";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, shopId.ToString()),
            new("shop_id", shopId.ToString()),
            new("role", role),
        };

        if (isAdmin)
        {
            claims.Add(new Claim("is_platform_admin", "true"));
        }
        else
        {
            claims.Add(new Claim("perms", string.Join(',', permissions)));
        }

        if (features.Length > 0)
            claims.Add(new Claim("feats", string.Join(',', features)));

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
