using System.Reflection;
using ErpSaas.Modules.Verticals.Grocery.Entities;
using ErpSaas.Modules.Verticals.Grocery.Infrastructure;
using ErpSaas.Modules.Verticals.Grocery.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class GroceryArchTests
{
    private static readonly Assembly GroceryAssembly = typeof(LoyaltyService).Assembly;

    private static string GetSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("src/ root not found");
    }

    [Fact]
    public void Grocery_HasRequiredTestClasses()
    {
        var root = GetSrcRoot();
        string[] required =
        [
            Path.Combine(root, "ErpSaas.Tests.Unit", "Modules", "Grocery", "LoyaltyServiceTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Arch", "Modules", "GroceryArchTests.cs"),
        ];
        foreach (var f in required)
            File.Exists(f).Should().BeTrue($"Missing required test file: {f}");
    }

    [Fact]
    public void GroceryEntities_AreInVerticalsGrocerySchema()
    {
        var modelBuilder = new ModelBuilder();
        GroceryModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        foreach (var entityType in new[] { typeof(LoyaltyProgram), typeof(LoyaltyTransaction) })
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull();
            meta!.GetSchema().Should().Be(GroceryModelConfiguration.Schema,
                $"{entityType.Name} must be in '{GroceryModelConfiguration.Schema}' schema");
        }
    }

    [Fact]
    public void LoyaltyService_DoesNotUseRawHttpClient()
    {
        var result = Types.InAssembly(GroceryAssembly)
            .That().ResideInNamespaceStartingWith("ErpSaas.Modules.Verticals.Grocery.Services")
            .ShouldNot().HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Grocery services must not use HttpClient directly");
    }

    [Fact]
    public void LoyaltyService_RegistersInterface()
    {
        var impl = GroceryAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ILoyaltyService).IsAssignableFrom(t))
            .ToList();
        impl.Should().ContainSingle();
    }
}
