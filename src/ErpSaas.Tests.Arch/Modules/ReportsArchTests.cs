using System.Reflection;
using ErpSaas.Modules.Reports.Services;
using FluentAssertions;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class ReportsArchTests
{
    private static readonly Assembly ReportsAssembly = typeof(ReportBuilderService).Assembly;

    private static string GetSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("src/ root not found");
    }

    // ── Six test classes required ─────────────────────────────────────────────

    [Fact]
    public void Reports_HasAllSixRequiredTestClasses()
    {
        var root = GetSrcRoot();
        string[] required =
        [
            Path.Combine(root, "ErpSaas.Tests.Unit", "Modules", "Reports", "ReportBuilderServiceTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Reports", "ReportsControllerTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Reports", "ReportsTenantIsolationTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Reports", "ReportsSubscriptionGateTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Reports", "ReportsAuditTrailTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Arch", "Modules", "ReportsArchTests.cs"),
        ];

        foreach (var f in required)
            File.Exists(f).Should().BeTrue($"Missing required test file: {f}");
    }

    // ── No raw SQL in service layer ───────────────────────────────────────────

    [Fact]
    public void ReportsService_DoesNot_UseRawHttpClient()
    {
        var result = Types.InAssembly(ReportsAssembly)
            .That().ResideInNamespaceStartingWith("ErpSaas.Modules.Reports.Services")
            .ShouldNot().HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Reports services must not use HttpClient directly");
    }

    // ── DI registration ───────────────────────────────────────────────────────

    [Fact]
    public void ReportsModule_RegistersServiceInterface()
    {
        var serviceType = typeof(IReportBuilderService);
        serviceType.IsInterface.Should().BeTrue();

        var impl = ReportsAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && serviceType.IsAssignableFrom(t))
            .ToList();

        impl.Should().ContainSingle("IReportBuilderService must have exactly one implementation");
    }

    // ── Dapper usage ──────────────────────────────────────────────────────────

    [Fact]
    public void ReportsService_UsesDapperContext_NotRawSqlConnection()
    {
        var result = Types.InAssembly(ReportsAssembly)
            .That().ResideInNamespaceStartingWith("ErpSaas.Modules.Reports.Services")
            .ShouldNot().HaveDependencyOn("Microsoft.Data.SqlClient.SqlConnection")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Reports services must use IDapperContext, not SqlConnection directly");
    }
}
