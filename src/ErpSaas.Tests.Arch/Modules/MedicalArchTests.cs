using System.Reflection;
using ErpSaas.Modules.Verticals.Medical.Entities;
using ErpSaas.Modules.Verticals.Medical.Infrastructure;
using ErpSaas.Modules.Verticals.Medical.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class MedicalArchTests
{
    private static readonly Assembly MedicalAssembly = typeof(MedicalInventoryService).Assembly;

    private static string GetSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("src/ root not found");
    }

    [Fact]
    public void Medical_HasRequiredTestClasses()
    {
        var root = GetSrcRoot();
        string[] required =
        [
            Path.Combine(root, "ErpSaas.Tests.Unit", "Modules", "Medical", "MedicalInventoryServiceTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Arch", "Modules", "MedicalArchTests.cs"),
        ];
        foreach (var f in required)
            File.Exists(f).Should().BeTrue($"Missing required test file: {f}");
    }

    [Fact]
    public void MedicalEntities_AreInVerticalsMedicalSchema()
    {
        var modelBuilder = new ModelBuilder();
        MedicalModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        foreach (var entityType in new[] { typeof(DrugBatch), typeof(PrescriptionRecord) })
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull();
            meta!.GetSchema().Should().Be(MedicalModelConfiguration.Schema,
                $"{entityType.Name} must be in '{MedicalModelConfiguration.Schema}' schema");
        }
    }

    [Fact]
    public void MedicalInventoryService_DoesNotUseRawHttpClient()
    {
        var result = Types.InAssembly(MedicalAssembly)
            .That().ResideInNamespaceStartingWith("ErpSaas.Modules.Verticals.Medical.Services")
            .ShouldNot().HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Medical services must not use HttpClient directly");
    }

    [Fact]
    public void MedicalInventoryService_RegistersInterface()
    {
        var impl = MedicalAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IMedicalInventoryService).IsAssignableFrom(t))
            .ToList();
        impl.Should().ContainSingle();
    }
}
