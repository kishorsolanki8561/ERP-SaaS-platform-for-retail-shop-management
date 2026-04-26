using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErpSaas.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

    // Connection-string env-var names that mirror the double-underscore convention
    // used by ASP.NET Core to map env vars → IConfiguration keys.
    private static readonly string[] CsEnvVarNames =
    [
        "ConnectionStrings__PlatformDb",
        "ConnectionStrings__TenantDb",
        "ConnectionStrings__AnalyticsDb",
        "ConnectionStrings__LogDb",
        "ConnectionStrings__NotificationsDb",
    ];

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        var host = _sqlContainer.Hostname;
        var port = _sqlContainer.GetMappedPublicPort(1433);
        const string pass = "Test@1234!StrongPass";

        string Cs(string db) =>
            $"Server={host},{port};Database={db};User Id=sa;Password={pass};" +
            "TrustServerCertificate=True;MultipleActiveResultSets=True";

        // Inject Testcontainers addresses as env vars BEFORE the factory runs
        // Program.cs. WebApplication.CreateBuilder snapshots env vars into
        // IConfiguration at construction time — before AddInfrastructure reads
        // any connection string — so every DbContext is registered with a real
        // SQL Server address from the very first DI registration.
        Environment.SetEnvironmentVariable("ConnectionStrings__PlatformDb",     Cs("ErpTest_Platform"));
        Environment.SetEnvironmentVariable("ConnectionStrings__TenantDb",        Cs("ErpTest_Tenant"));
        Environment.SetEnvironmentVariable("ConnectionStrings__AnalyticsDb",     Cs("ErpTest_Analytics"));
        Environment.SetEnvironmentVariable("ConnectionStrings__LogDb",           Cs("ErpTest_Log"));
        Environment.SetEnvironmentVariable("ConnectionStrings__NotificationsDb", Cs("ErpTest_Notifications"));

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // appsettings.Testing.json (loaded via UseEnvironment) provides:
                //   Jwt:Secret, Hangfire:DisableServer, Turnstile:AlwaysValidate
                // Connection strings are already in IConfiguration via env vars above.
                builder.UseEnvironment("Testing");
            });

        // Warm up — triggers Program.cs startup, migrations, and seeds.
        _ = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _sqlContainer.DisposeAsync();

        // Remove env vars so they don't leak into subsequent test processes.
        foreach (var name in CsEnvVarNames)
            Environment.SetEnvironmentVariable(name, null);
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
