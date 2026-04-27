using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErpSaas.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
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
    private readonly MsSqlContainer _container = new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Test@1234!StrongPass")
        .Build();

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
        await _container.StartAsync();

        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(1433);
        const string pass = "Test@1234!StrongPass";

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

    async Task IAsyncLifetime.DisposeAsync()
    {
        // Dispose the WebApplicationFactory (stops the test server).
        // WebApplicationFactory.Dispose() is idempotent so xUnit's own
        // IDisposable.Dispose() call after this is safe.
        Dispose();
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

    /// <summary>Returns an HttpClient authenticated as the given shop admin.</summary>
    public HttpClient CreateAuthenticatedClient(
        long shopId = 1,
        string[] permissions = null!,
        string role = "Admin")
    {
        var token = GenerateJwt(shopId, permissions ?? ["*"], role);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Shop-Id", shopId.ToString());
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

    // ── JWT helpers ───────────────────────────────────────────────────────────

    private static string GenerateJwt(long shopId, string[] permissions, string role)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, $"user-{shopId}"),
            new("shopId", shopId.ToString()),
            new("role", role),
        };

        foreach (var p in permissions)
            claims.Add(new Claim("permission", p));

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
