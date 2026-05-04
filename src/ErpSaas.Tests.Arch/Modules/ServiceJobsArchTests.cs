using System.Reflection;
using ErpSaas.Modules.ServiceJobs.Entities;
using ErpSaas.Modules.ServiceJobs.Infrastructure;
using ErpSaas.Modules.ServiceJobs.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch.Modules;

[Trait("Category", "Architecture")]
public class ServiceJobsArchTests
{
    private static readonly Assembly ServiceJobsAssembly = typeof(ServiceJobService).Assembly;

    private static string GetSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("src/ root not found");
    }

    [Fact]
    public void ServiceJobs_HasRequiredTestClasses()
    {
        var root = GetSrcRoot();
        string[] required =
        [
            Path.Combine(root, "ErpSaas.Tests.Unit", "Modules", "ServiceJobs", "ServiceJobServiceTests.cs"),
            Path.Combine(root, "ErpSaas.Tests.Arch", "Modules", "ServiceJobsArchTests.cs"),
        ];
        foreach (var f in required)
            File.Exists(f).Should().BeTrue($"Missing required test file: {f}");
    }

    [Fact]
    public void ServiceJobEntities_AreInServiceSchema()
    {
        var modelBuilder = new ModelBuilder();
        ServiceJobModelConfiguration.Configure(modelBuilder);
        var model = modelBuilder.FinalizeModel();

        foreach (var entityType in new[] { typeof(ServiceJob), typeof(ServiceJobPart), typeof(ServiceJobLabor) })
        {
            var meta = model.FindEntityType(entityType);
            meta.Should().NotBeNull();
            meta!.GetSchema().Should().Be(ServiceJobModelConfiguration.Schema,
                $"{entityType.Name} must be in '{ServiceJobModelConfiguration.Schema}' schema");
        }
    }

    [Fact]
    public void ServiceJobService_DoesNotUseRawHttpClient()
    {
        var result = Types.InAssembly(ServiceJobsAssembly)
            .That().ResideInNamespaceStartingWith("ErpSaas.Modules.ServiceJobs.Services")
            .ShouldNot().HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue("ServiceJob services must not use HttpClient directly");
    }

    [Fact]
    public void ServiceJobService_RegistersInterface()
    {
        var impl = ServiceJobsAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IServiceJobService).IsAssignableFrom(t))
            .ToList();
        impl.Should().ContainSingle();
    }
}
