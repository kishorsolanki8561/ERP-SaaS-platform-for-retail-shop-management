using System.Reflection;
using ErpSaas.Infrastructure.Http;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetArchTest.Rules;

namespace ErpSaas.Tests.Arch;

[Trait("Category", "Architecture")]
public class SecurityArchTests
{
    // Assemblies that contain controller code
    private static readonly Assembly[] ControllerAssemblies =
    [
        typeof(ErpSaas.Modules.Identity.Controllers.AuthController).Assembly,
        typeof(ErpSaas.Modules.Masters.Controllers.DdlController).Assembly,
        typeof(ErpSaas.Api.Controllers.SystemController).Assembly,
    ];

    [Fact]
    public void EveryPublicAuthEndpoint_HasCaptchaGuard()
    {
        // Every action method that is [AllowAnonymous] AND touches auth (login, register, refresh, bootstrap)
        // must carry [RequireCaptcha].
        var violations = new List<string>();

        foreach (var asm in ControllerAssemblies)
        {
            var controllers = asm.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

            foreach (var controller in controllers)
            {
                var methods = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null
                                || m.GetCustomAttribute<HttpGetAttribute>() != null
                                || m.GetCustomAttribute<HttpPutAttribute>() != null
                                || m.GetCustomAttribute<HttpDeleteAttribute>() != null);

                foreach (var method in methods)
                {
                    var isAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>() != null
                                     || controller.GetCustomAttribute<AllowAnonymousAttribute>() != null;
                    if (!isAnonymous) continue;

                    // Any anonymous endpoint whose name suggests an auth operation must have RequireCaptcha
                    var isAuthSensitive = method.Name.Contains("Login", StringComparison.OrdinalIgnoreCase)
                                         || method.Name.Contains("Register", StringComparison.OrdinalIgnoreCase)
                                         || method.Name.Contains("Bootstrap", StringComparison.OrdinalIgnoreCase)
                                         || method.Name.Contains("ForgotPassword", StringComparison.OrdinalIgnoreCase)
                                         || method.Name.Contains("ResetPassword", StringComparison.OrdinalIgnoreCase)
                                         || method.Name.Contains("RequestOtp", StringComparison.OrdinalIgnoreCase);

                    if (!isAuthSensitive) continue;

                    var hasCaptcha = method.GetCustomAttribute<RequireCaptchaAttribute>() != null;
                    if (!hasCaptcha)
                        violations.Add($"{controller.Name}.{method.Name}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"these public auth endpoints are missing [RequireCaptcha]: {string.Join(", ", violations)}");
    }

    [Fact]
    public void NoRawHttpClient_OutsideThirdPartyApiClientBase()
    {
        // Business service classes must not inject HttpClient directly — only via ThirdPartyApiClientBase
        var serviceAssemblies = ControllerAssemblies
            .Concat([typeof(ErpSaas.Infrastructure.Services.ErrorLogger).Assembly])
            .ToArray();

        foreach (var asm in serviceAssemblies)
        {
            var violations = asm.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract
                    && t.Name.EndsWith("Service", StringComparison.Ordinal)
                    && !typeof(ThirdPartyApiClientBase).IsAssignableFrom(t))
                .Where(t => t.GetConstructors()
                    .Any(c => c.GetParameters()
                        .Any(p => p.ParameterType == typeof(System.Net.Http.HttpClient))))
                .Select(t => t.FullName!)
                .ToList();

            violations.Should().BeEmpty(
                $"these service classes inject HttpClient directly — use ThirdPartyApiClientBase instead: {string.Join(", ", violations)}");
        }
    }
}
