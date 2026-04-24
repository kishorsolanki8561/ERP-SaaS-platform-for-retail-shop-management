using ErpSaas.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ErpSaas.Infrastructure.Authorization;

public sealed class DynamicAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var basePolicy = await base.GetPolicyAsync(policyName);
        if (basePolicy is not null) return basePolicy;

        if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix))
        {
            var code = policyName[RequirePermissionAttribute.PolicyPrefix.Length..];
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new RequirePermissionAttribute(code))
                .Build();
        }

        if (policyName.StartsWith(RequireFeatureAttribute.PolicyPrefix))
        {
            var code = policyName[RequireFeatureAttribute.PolicyPrefix.Length..];
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new RequireFeatureAttribute(code))
                .Build();
        }

        return null;
    }
}
