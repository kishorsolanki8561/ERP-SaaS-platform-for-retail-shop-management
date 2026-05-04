using ErpSaas.Modules.CustomerPortal.Controllers;
using ErpSaas.Modules.CustomerPortal.Services;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace ErpSaas.Tests.Arch;

[Trait("Category", "Architecture")]
public class CustomerPortalArchTests
{
    private static Types InModule() =>
        Types.InAssembly(typeof(PortalAuthController).Assembly);

    [Fact]
    public void CustomerPortal_Services_ShouldNotDependOn_Controllers()
    {
        var result = InModule()
            .That().ResideInNamespace("ErpSaas.Modules.CustomerPortal.Services")
            .ShouldNot().HaveDependencyOn("ErpSaas.Modules.CustomerPortal.Controllers")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames));
    }

    [Fact]
    public void CustomerPortal_Controllers_ShouldDependOn_Services()
    {
        var result = InModule()
            .That().ResideInNamespace("ErpSaas.Modules.CustomerPortal.Controllers")
            .ShouldNot().HaveDependencyOn("ErpSaas.Infrastructure.Data")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames));
    }

    [Fact]
    public void CustomerPortal_Entities_ShouldNotDependOn_Services()
    {
        var result = InModule()
            .That().ResideInNamespace("ErpSaas.Modules.CustomerPortal.Entities")
            .ShouldNot().HaveDependencyOn("ErpSaas.Modules.CustomerPortal.Services")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames));
    }

    [Fact]
    public void CustomerPortal_Services_MustNotUseRawHttpClient()
    {
        var result = InModule()
            .That().ResideInNamespace("ErpSaas.Modules.CustomerPortal.Services")
            .ShouldNot().HaveDependencyOn("System.Net.Http.HttpClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames));
    }

    [Fact]
    public void CustomerPortal_ServiceInterfaces_ShouldExistForEveryServiceImpl()
    {
        var serviceImpls = InModule()
            .That().ResideInNamespace("ErpSaas.Modules.CustomerPortal.Services")
            .And().AreClasses()
            .And().ImplementInterface(typeof(ICustomerPortalAuthService))
            .Or().ImplementInterface(typeof(ICustomerPortalService))
            .Or().ImplementInterface(typeof(IOnlineOrderService))
            .Or().ImplementInterface(typeof(ICustomerInquiryService))
            .GetTypes();

        serviceImpls.Should().HaveCount(4);
    }
}
