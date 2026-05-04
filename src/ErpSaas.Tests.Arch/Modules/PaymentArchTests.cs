using System.Reflection;
using ErpSaas.Modules.Payment.Entities;
using ErpSaas.Modules.Payment.Infrastructure;
using ErpSaas.Modules.Payment.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class PaymentArchTests
{
    private static readonly Assembly PaymentAssembly = typeof(PaymentGatewayService).Assembly;

    private static string GetSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("src/ root not found");
    }

    [Fact]
    public void Payment_HasAllSixRequiredTestClasses()
    {
        var root = GetSrcRoot();
        string[] required =
        [
            Path.Combine(root, "ErpSaas.Tests.Unit", "Modules", "Payment", "PaymentGatewayServiceTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Payment", "PaymentControllerTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Payment", "PaymentTenantIsolationTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Payment", "PaymentSubscriptionGateTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Payment", "PaymentAuditTrailTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Arch", "Modules", "PaymentArchTests.cs"),
        ];
        foreach (var f in required)
            File.Exists(f).Should().BeTrue($"Missing required test file: {f}");
    }

    [Fact]
    public void PaymentEntities_AreInPaymentSchema()
    {
        var modelBuilder = new ModelBuilder();
        PaymentModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        foreach (var entityType in new[] { typeof(PaymentGatewayTransaction), typeof(PaymentGatewayAccount), typeof(ReconciliationException) })
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull();
            meta!.GetSchema().Should().Be(PaymentModelConfiguration.Schema,
                $"{entityType.Name} must be in '{PaymentModelConfiguration.Schema}' schema");
        }
    }

    [Fact]
    public void PaymentService_DoesNotUseRawHttpClient()
    {
        var result = Types.InAssembly(PaymentAssembly)
            .That().ResideInNamespaceStartingWith("ErpSaas.Modules.Payment.Services")
            .ShouldNot().HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Payment services must not use HttpClient directly");
    }

    [Fact]
    public void PaymentServices_RegisterInterfaces()
    {
        var gatewayImpl = PaymentAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IPaymentGatewayService).IsAssignableFrom(t))
            .ToList();
        gatewayImpl.Should().ContainSingle("IPaymentGatewayService must have exactly one implementation");

        var reconcileImpl = PaymentAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IPaymentReconciliationService).IsAssignableFrom(t))
            .ToList();
        reconcileImpl.Should().ContainSingle("IPaymentReconciliationService must have exactly one implementation");
    }
}
