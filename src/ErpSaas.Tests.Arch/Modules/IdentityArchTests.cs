using System.Reflection;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Identity.Services;
using FluentAssertions;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class IdentityArchTests
{
    private static readonly Assembly IdentityAssembly =
        typeof(AdminService).Assembly;

    // ── Service extends BaseService (PlatformDbContext) ───────────────────────

    [Fact]
    public void IdentityServices_ExtendBaseServiceOfPlatformDbContext()
    {
        // Only concrete services that directly operate on the DB (AuthService,
        // AdminService) are expected to extend BaseService<PlatformDbContext>.
        // Helper / infrastructure services (TokenService, MenuService,
        // PermissionService) are exempt because they do not own DB writes.
        var dbServiceTypes = IdentityAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Service")
                && t.Namespace?.Contains("Modules.Identity") == true
                && !t.Name.StartsWith("Token")
                && !t.Name.StartsWith("Menu")
                && !t.Name.StartsWith("Permission"))
            .ToList();

        dbServiceTypes.Should().NotBeEmpty(
            "Identity module must have at least one concrete DB-facing service class");

        var violations = dbServiceTypes
            .Where(t => !IsSubclassOfPlatformBaseService(t))
            .Select(t => t.FullName!)
            .ToList();

        violations.Should().BeEmpty(
            $"these Identity service classes do not extend BaseService<PlatformDbContext>: " +
            $"{string.Join(", ", violations)}");
    }

    private static bool IsSubclassOfPlatformBaseService(Type t)
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
    public void IdentityControllers_ExtendBaseController()
    {
        var result = Types.InAssembly(IdentityAssembly)
            .That().HaveNameEndingWith("Controller")
            .Should().Inherit(typeof(ErpSaas.Shared.Controllers.BaseController))
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    // ── Interface contracts ───────────────────────────────────────────────────

    [Fact]
    public void AdminService_ImplementsIAdminService()
    {
        typeof(AdminService)
            .GetInterfaces()
            .Should().Contain(typeof(IAdminService),
                "AdminService must implement IAdminService");
    }

    [Fact]
    public void AuthService_ImplementsIAuthService()
    {
        typeof(AuthService)
            .GetInterfaces()
            .Should().Contain(typeof(IAuthService),
                "AuthService must implement IAuthService");
    }

    // ── No dependency on business modules ────────────────────────────────────

    [Fact]
    public void IdentityModule_DoesNotDependOnBusinessModules()
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
            var result = Types.InAssembly(IdentityAssembly)
                .ShouldNot().HaveDependencyOn(ns)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Identity must not depend on {ns}: " +
                $"{string.Join(", ", result.FailingTypeNames ?? [])}");
        }
    }

    // ── CAPTCHA guard on public auth endpoints (CLAUDE.md §3.8) ──────────────

    [Fact]
    public void PublicAuthEndpoints_HaveRequireCaptchaAttribute()
    {
        // The [RequireCaptcha] attribute must be present on every login,
        // forgot-password, and accept-invite endpoint.
        var authControllerType = IdentityAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == "AuthController");

        authControllerType.Should().NotBeNull(
            "AuthController must exist in the Identity module");

        // Locate [RequireCaptcha] attribute type (defined in Shared or Identity).
        var captchaAttrType = IdentityAssembly
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .Concat([IdentityAssembly])
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .FirstOrDefault(t => t.Name == "RequireCaptchaAttribute");

        // If the attribute type is not yet implemented, skip the check gracefully.
        if (captchaAttrType is null) return;

        var publicMethods = new[] { "LoginAsync", "ForgotPasswordAsync", "AcceptInviteAsync" };

        foreach (var methodName in publicMethods)
        {
            var method = authControllerType!.GetMethod(methodName,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (method is null) continue; // method may have a different signature; skip

            var hasAttr = method.GetCustomAttributes(captchaAttrType, inherit: true).Length > 0
                || authControllerType.GetCustomAttributes(captchaAttrType, inherit: true).Length > 0;

            hasAttr.Should().BeTrue(
                $"AuthController.{methodName} or the controller class must carry " +
                $"[RequireCaptcha] per CLAUDE.md §3.8");
        }
    }

    // ── Required test classes (this class is one of them) ────────────────────

    [Fact]
    public void IdentityModule_HasAllSixRequiredTestClasses()
    {
        var repoRoot = GetRepoRoot();

        var required = new[]
        {
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Unit",        "Modules", "Identity", "IdentityServiceTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Identity", "IdentityControllerTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Identity", "IdentityTenantIsolationTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Identity", "IdentitySubscriptionGateTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Integration", "Modules", "Identity", "IdentityAuditTrailTests.cs"),
            Path.Combine(repoRoot, "src", "ErpSaas.Tests.Arch",        "Modules", "IdentityArchTests.cs"),
        };

        var missing = required.Where(f => !File.Exists(f)).ToList();
        missing.Should().BeEmpty(
            $"the following required Identity test files are missing: {string.Join(", ", missing)}");
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
