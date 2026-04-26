using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Seeds;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.MsSql;

namespace ErpSaas.Tests.Integration.Fixtures;

/// <summary>
/// Shared test fixture that starts one SQL Server container, creates all
/// required databases, runs migrations, and seeds reference data.
/// Use as xUnit <c>IClassFixture&lt;IntegrationTestFixture&gt;</c>.
/// </summary>
public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder(
            "mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Test@1234!StrongPass")
        .Build();

    // Publicly visible after InitializeAsync
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public IServiceProvider Services => Factory.Services;

    // Deterministic JWT secret for tests — never a real secret
    private const string TestJwtSecret = "TestOnly_SuperSecretKey_AtLeast32Chars_DoNotUse";
    private const string TestIssuer    = "test-issuer";
    private const string TestAudience  = "test-audience";

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        var host = _sqlContainer.Hostname;
        var port = _sqlContainer.GetMappedPublicPort(1433);
        var pass = "Test@1234!StrongPass";

        string Cs(string db) =>
            $"Server={host},{port};Database={db};User Id=sa;Password={pass};" +
            "TrustServerCertificate=True;MultipleActiveResultSets=True";

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:PlatformDb"]       = Cs("ErpTest_Platform"),
                        ["ConnectionStrings:TenantDb"]         = Cs("ErpTest_Tenant"),
                        ["ConnectionStrings:AnalyticsDb"]      = Cs("ErpTest_Analytics"),
                        ["ConnectionStrings:LogDb"]            = Cs("ErpTest_Log"),
                        ["ConnectionStrings:NotificationsDb"]  = Cs("ErpTest_Notifications"),
                        // Disable Hangfire background server in tests (avoids it connecting
                        // to SQL Server before migrations have run)
                        ["Hangfire:DisableServer"]             = "true",
                        // Turnstile bypass in tests
                        ["Turnstile:AlwaysValidate"]           = "false",
                        // JWT
                        ["Jwt:Secret"]                         = TestJwtSecret,
                        ["Jwt:Issuer"]                         = TestIssuer,
                        ["Jwt:Audience"]                       = TestAudience,
                        ["Jwt:AccessTokenExpiryMinutes"]       = "60",
                    });
                });

                // Override DbContext options at the DI level so the Testcontainers
                // connection strings are used regardless of when AddInfrastructure()
                // captured them from IConfiguration (service-registration-time capture
                // predates ConfigureAppConfiguration overrides being applied).
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<PlatformDbContext>>();
                    services.RemoveAll<DbContextOptions<TenantDbContext>>();
                    services.RemoveAll<DbContextOptions<AnalyticsDbContext>>();
                    services.RemoveAll<DbContextOptions<LogDbContext>>();
                    services.RemoveAll<DbContextOptions<NotificationsDbContext>>();

                    services.AddDbContext<PlatformDbContext>(opts =>
                        opts.UseSqlServer(Cs("ErpTest_Platform"),
                            sql => sql.MigrationsAssembly(typeof(PlatformDbContext).Assembly.FullName)));
                    services.AddDbContext<TenantDbContext>(opts =>
                        opts.UseSqlServer(Cs("ErpTest_Tenant"),
                            sql => sql.MigrationsAssembly(typeof(TenantDbContext).Assembly.FullName)));
                    services.AddDbContext<AnalyticsDbContext>(opts =>
                        opts.UseSqlServer(Cs("ErpTest_Analytics"),
                            sql => sql.MigrationsAssembly(typeof(AnalyticsDbContext).Assembly.FullName)));
                    services.AddDbContext<LogDbContext>(opts =>
                        opts.UseSqlServer(Cs("ErpTest_Log"),
                            sql => sql.MigrationsAssembly(typeof(LogDbContext).Assembly.FullName)));
                    services.AddDbContext<NotificationsDbContext>(opts =>
                        opts.UseSqlServer(Cs("ErpTest_Notifications"),
                            sql => sql.MigrationsAssembly(typeof(NotificationsDbContext).Assembly.FullName)));

                    // JWT is configured at registration time from IConfiguration, so the CI
                    // env-var Jwt__Secret is captured before ConfigureAppConfiguration overrides
                    // apply. PostConfigure runs after all Configure calls and overwrites the key.
                    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
                    {
                        opts.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer           = true,
                            ValidIssuer              = TestIssuer,
                            ValidateAudience         = true,
                            ValidAudience            = TestAudience,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey         = new SymmetricSecurityKey(
                                                           Encoding.UTF8.GetBytes(TestJwtSecret)),
                            ValidateLifetime         = true,
                            ClockSkew                = TimeSpan.Zero,
                        };
                    });
                });
            });

        // Warm up the factory — this runs InitializeAsync (migrations + seeds)
        _ = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }

    /// <summary>
    /// Returns an HttpClient authenticated as the given shop admin.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(
        long shopId = 1,
        string[] permissions = null!,
        string role = "Admin")
    {
        var token = GenerateJwt(shopId, permissions ?? ["*"], role);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Shop-Id", shopId.ToString());
        return client;
    }

    public HttpClient CreateUnauthenticatedClient() => Factory.CreateClient();

    /// <summary>
    /// Opens a new DI scope for direct service access in tests.
    /// Dispose the returned scope after use.
    /// </summary>
    public AsyncServiceScope CreateScope() => Factory.Services.CreateAsyncScope();

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

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Returns a direct connection to the Tenant test database for row-count assertions.
    /// </summary>
    public Microsoft.Data.SqlClient.SqlConnection OpenTenantConnection()
    {
        var scope = Factory.Services.CreateScope();
        var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        var connStr = tenantDb.Database.GetConnectionString()!;
        var conn = new Microsoft.Data.SqlClient.SqlConnection(connStr);
        conn.Open();
        return conn;
    }

    public Microsoft.Data.SqlClient.SqlConnection OpenPlatformConnection()
    {
        var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var connStr = db.Database.GetConnectionString()!;
        var conn = new Microsoft.Data.SqlClient.SqlConnection(connStr);
        conn.Open();
        return conn;
    }
}
