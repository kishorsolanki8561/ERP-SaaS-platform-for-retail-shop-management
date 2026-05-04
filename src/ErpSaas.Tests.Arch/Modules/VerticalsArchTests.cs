using System.Reflection;
using ErpSaas.Modules.Verticals.Entities;
using ErpSaas.Modules.Verticals.Infrastructure;
using ErpSaas.Modules.Verticals.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class VerticalsArchTests
{
    private static readonly Assembly VerticalsAssembly = typeof(VerticalPackService).Assembly;

    private static string GetSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("src/ root not found");
    }

    [Fact]
    public void Verticals_HasRequiredTestClasses()
    {
        var root = GetSrcRoot();
        string[] required =
        [
            Path.Combine(root, "ErpSaas.Tests.Unit", "Modules", "Verticals", "VerticalPackServiceTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Arch", "Modules", "VerticalsArchTests.cs"),
        ];
        foreach (var f in required)
            File.Exists(f).Should().BeTrue($"Missing required test file: {f}");
    }

    [Fact]
    public void ShopVertical_IsInVerticalsSchema()
    {
        var modelBuilder = new ModelBuilder();
        VerticalModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        var meta = model.FindEntityType(typeof(ShopVertical));
        meta.Should().NotBeNull();
        meta!.GetSchema().Should().Be(VerticalModelConfiguration.Schema,
            "ShopVertical must be in 'verticals' schema");
    }

    [Fact]
    public void VerticalPackService_DoesNotUseRawHttpClient()
    {
        var result = Types.InAssembly(VerticalsAssembly)
            .That().ResideInNamespaceStartingWith("ErpSaas.Modules.Verticals.Services")
            .ShouldNot().HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Verticals services must not use HttpClient directly");
    }

    [Fact]
    public void VerticalPackService_RegistersInterface()
    {
        var impl = VerticalsAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IVerticalPackService).IsAssignableFrom(t))
            .ToList();
        impl.Should().ContainSingle();
    }
}
