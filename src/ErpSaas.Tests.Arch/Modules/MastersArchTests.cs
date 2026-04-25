using System.Reflection;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Masters.Services;
using FluentAssertions;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class MastersArchTests
{
    private static readonly Assembly MastersAssembly =
        typeof(MasterDataService).Assembly;

    // ── Service extends BaseService (PlatformDbContext) ───────────────────────

    [Fact]
    public void MastersService_ExtendsBaseServiceOfPlatformDbContext()
    {
        var serviceTypes = MastersAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service"))
            .ToList();

        serviceTypes.Should().NotBeEmpty(
            "Masters module must have at least one concrete service class");

        var violations = serviceTypes
            .Where(t => !IsSubclassOfBaseService(t))
            .Select(t => t.FullName!)
            .ToList();

        violations.Should().BeEmpty(
            $"these service classes do not extend BaseService<PlatformDbContext>: " +
            $"{string.Join(", ", violations)}");
    }

    private static bool IsSubclassOfBaseService(Type t)
    {
        var current = t.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType
                && current.GetGenericTypeDefinition() == typeof(BaseService<>)
                && current.GetGenericArguments().Length == 1
                && current.GetGenericArguments()[0] == typeof(PlatformDbContext))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    // ── Controller rules ──────────────────────────────────────────────────────

    [Fact]
    public void MastersControllers_ExtendBaseController()
    {
        var result = Types.InAssembly(MastersAssembly)
            .That().HaveNameEndingWith("Controller")
            .Should().Inherit(typeof(ErpSaas.Shared.Controllers.BaseController))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    // ── No dependency on business modules ────────────────────────────────────

    [Fact]
    public void MastersModule_DoesNotDependOnBusinessModules()
    {
        var businessModuleNamespaces = new[]
        {
            "ErpSaas.Modules.Billing",
            "ErpSaas.Modules.Crm",
            "ErpSaas.Modules.Inventory",
            "ErpSaas.Modules.Hr",
            "ErpSaas.Modules.Accounting",
            "ErpSaas.Modules.Wallet",
            "ErpSaas.Modules.Shift",
        };

        foreach (var ns in businessModuleNamespaces)
        {
            var result = Types.InAssembly(MastersAssembly)
                .ShouldNot().HaveDependencyOn(ns)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Masters must not depend on {ns}: " +
                $"{string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    // ── IMasterDataService has proper interface ───────────────────────────────

    [Fact]
    public void MasterDataService_ImplementsIMasterDataService()
    {
        typeof(MasterDataService)
            .GetInterfaces()
            .Should().Contain(typeof(IMasterDataService),
                "MasterDataService must implement IMasterDataService");
    }

    // ── Required test classes (this class is one of them) ────────────────────

    [Fact]
    public void MastersModule_HasAllSixRequiredTestClasses()
    {
        var repoRoot = GetRepoRoot();

        var required = new[]
        {
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Unit",        "Modules", "Masters", "MastersServiceTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Masters", "MastersControllerTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Masters", "MastersTenantIsolationTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Masters", "MastersSubscriptionGateTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Masters", "MastersAuditTrailTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Arch",        "Modules", "MastersArchTests.cs"),
        };

        var missing = required.Where(f => !File.Exists(f)).ToList();
        missing.Should().BeEmpty(
            $"the following required Masters test files are missing: {string.Join(", ", missing)}");
    }

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ErpSaas.sln")))
            dir = dir.Parent;
        return dir?.Parent?.FullName
            ?? throw new InvalidOperationException("Could not locate repo root (no ErpSaas.sln found).");
    }
}
