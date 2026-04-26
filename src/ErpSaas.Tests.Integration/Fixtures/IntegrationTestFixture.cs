using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErpSaas.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;

namespace ErpSaas.Tests.Integration.Fixtures;

/// <summary>
/// Shared test fixture. Inherits <see cref="WebApplicationFactory{TEntryPoint}"/> so that
/// <c>ConfigureWebHost</c> is called at exactly the right time (after all application
/// service registrations, before <c>builder.Build()</c>). This guarantees that the
/// <c>RemoveAll + AddDbContext</c> overrides target the correct DI descriptors.
///
/// Use as xUnit <c>IClassFixture&lt;IntegrationTestFixture&gt;</c>.
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
    private string _platformCs  = null!;
    private string _tenantCs    = null!;
    private string _analyticsCs = null!;
    private string _logCs       = null!;
    private string _notifsCs    = null!;

    // ── WebApplicationFactory override ────────────────────────────────────────

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // appsettings.Testing.json (loaded via UseEnvironment) provides
        // Jwt:Secret, Hangfire:DisableServer, Turnstile:AlwaysValidate.
        builder.UseEnvironment("Testing");

        // ConfigureTestServices runs AFTER all application ConfigureServices calls,
        // so RemoveAll correctly replaces the options registered by AddInfrastructure.
        // The _*Cs fields are already populated because InitializeAsync starts the
        // container and sets them before calling CreateClient().
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<PlatformDbContext>>();
            services.RemoveAll<DbContextOptions<TenantDbContext>>();
            services.RemoveAll<DbContextOptions<AnalyticsDbContext>>();
            services.RemoveAll<DbContextOptions<LogDbContext>>();
            services.RemoveAll<DbContextOptions<NotificationsDbContext>>();

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

        _platformCs  = Cs("ErpTest_Platform");
        _tenantCs    = Cs("ErpTest_Tenant");
        _analyticsCs = Cs("ErpTest_Analytics");
        _logCs       = Cs("ErpTest_Log");
        _notifsCs    = Cs("ErpTest_Notifications");

        // For minimal-API apps, WebApplication.CreateBuilder(args) reads IConfiguration
        // from the real process environment BEFORE ConfigureWebHost/ConfigureTestServices
        // can run. Setting env vars here — before CreateClient() — ensures:
        //   (a) appsettings.Testing.json is loaded (ASPNETCORE_ENVIRONMENT)
        //   (b) AddInfrastructure's configuration.GetConnectionString() lambdas see
        //       the Testcontainers strings instead of the appsettings.json localhost ones
        //   (c) AddIdentityModule reads the test JWT secret/issuer/audience
        // ConfigureTestServices below is kept as defence-in-depth for the DbContextOptions.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT",             "Testing");
        Environment.SetEnvironmentVariable("ConnectionStrings__PlatformDb",      _platformCs);
        Environment.SetEnvironmentVariable("ConnectionStrings__TenantDb",        _tenantCs);
        Environment.SetEnvironmentVariable("ConnectionStrings__AnalyticsDb",     _analyticsCs);
        Environment.SetEnvironmentVariable("ConnectionStrings__LogDb",           _logCs);
        Environment.SetEnvironmentVariable("ConnectionStrings__NotificationsDb", _notifsCs);
        Environment.SetEnvironmentVariable("Jwt__Secret",   TestJwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer",   TestIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", TestAudience);

        // Warm up: triggers ConfigureWebHost → Program.cs startup →
        // migrations and seeds — all against the Testcontainers SQL Server.
        _ = CreateClient();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        // Dispose the WebApplicationFactory (stops the test server).
        // WebApplicationFactory.Dispose() is idempotent so xUnit's own
        // IDisposable.Dispose() call after this is safe.
        Dispose();
        await _container.DisposeAsync();
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
