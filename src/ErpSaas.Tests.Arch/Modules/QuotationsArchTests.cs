using System.Reflection;
using ErpSaas.Modules.Quotations.Entities;
using ErpSaas.Modules.Quotations.Infrastructure;
using ErpSaas.Modules.Quotations.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class QuotationsArchTests
{
    private static readonly Assembly QuotationsAssembly = typeof(QuotationsService).Assembly;

    private static string GetSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("src/ root not found");
    }

    [Fact]
    public void Quotations_HasAllSixRequiredTestClasses()
    {
        var root = GetSrcRoot();
        string[] required =
        [
            Path.Combine(root, "ErpSaas.Tests.Unit", "Modules", "Quotations", "QuotationsServiceTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Quotations", "QuotationsControllerTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Quotations", "QuotationsTenantIsolationTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Quotations", "QuotationsSubscriptionGateTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Integration", "Modules", "Quotations", "QuotationsAuditTrailTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Arch", "Modules", "QuotationsArchTests.cs"),
        ];
        foreach (var f in required)
            File.Exists(f).Should().BeTrue($"Missing required test file: {f}");
    }

    [Fact]
    public void QuotationsEntities_AreInSalesSchema()
    {
        var modelBuilder = new ModelBuilder();
        QuotationsModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        foreach (var entityType in new[]
        {
            typeof(Quotation), typeof(QuotationLine),
            typeof(SalesOrder), typeof(SalesOrderLine),
            typeof(DeliveryChallan), typeof(DeliveryChallanLine),
        })
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull();
            meta!.GetSchema().Should().Be(QuotationsModelConfiguration.Schema,
                $"{entityType.Name} must be in '{QuotationsModelConfiguration.Schema}' schema");
        }
    }

    [Fact]
    public void QuotationsService_DoesNotUseRawHttpClient()
    {
        var result = Types.InAssembly(QuotationsAssembly)
            .That().ResideInNamespaceStartingWith("ErpSaas.Modules.Quotations.Services")
            .ShouldNot().HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Quotations services must not use HttpClient directly");
    }

    [Fact]
    public void QuotationsService_RegistersInterface()
    {
        var impl = QuotationsAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IQuotationsService).IsAssignableFrom(t))
            .ToList();
        impl.Should().ContainSingle();
    }
}
