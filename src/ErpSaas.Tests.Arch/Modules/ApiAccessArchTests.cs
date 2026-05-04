using System.Reflection;
using ErpSaas.Modules.ApiAccess.Entities;
using ErpSaas.Modules.ApiAccess.Infrastructure;
using ErpSaas.Modules.ApiAccess.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class ApiAccessArchTests
{
    private static readonly Assembly ApiAccessAssembly = typeof(ShopApiKeyService).Assembly;

    private static string GetSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("src/ root not found");
    }

    [Fact]
    public void ApiAccess_HasAllSixRequiredTestClasses()
    {
        var root = GetSrcRoot();
        string[] required =
        [
            Path.Combine(root, "ErpSaas.Tests.Unit", "Modules", "ApiAccess", "ApiAccessServiceTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "ApiAccess", "ApiAccessControllerTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "ApiAccess", "ApiAccessTenantIsolationTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "ApiAccess", "ApiAccessSubscriptionGateTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "ApiAccess", "ApiAccessAuditTrailTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Arch", "Modules", "ApiAccessArchTests.cs"),
        ];
        foreach (var f in required)
            File.Exists(f).Should().BeTrue($"Missing required test file: {f}");
    }

    [Fact]
    public void ApiAccessEntities_AreInIntegrationSchema()
    {
        var modelBuilder = new ModelBuilder();
        ApiAccessModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        foreach (var entityType in new[] { typeof(ShopApiKey), typeof(WebhookEndpoint), typeof(WebhookDelivery) })
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull();
            meta!.GetSchema().Should().Be(ApiAccessModelConfiguration.Schema,
                $"{entityType.Name} must be in '{ApiAccessModelConfiguration.Schema}' schema");
        }
    }

    [Fact]
    public void ApiAccessServices_RegisterInterfaces()
    {
        var keyImpl = ApiAccessAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IShopApiKeyService).IsAssignableFrom(t))
            .ToList();
        keyImpl.Should().ContainSingle("IShopApiKeyService must have exactly one implementation");

        var webhookImpl = ApiAccessAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IWebhookDispatchService).IsAssignableFrom(t))
            .ToList();
        webhookImpl.Should().ContainSingle("IWebhookDispatchService must have exactly one implementation");
    }

    [Fact]
    public void WebhookSignatureGenerator_IsSealed()
    {
        typeof(WebhookSignatureGenerator).IsSealed.Should().BeTrue(
            "WebhookSignatureGenerator is a pure-function utility and must not be subclassed");
    }
}
