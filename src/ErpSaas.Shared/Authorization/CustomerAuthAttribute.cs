using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace ErpSaas.Shared.Authorization;

/// <summary>
/// Validates that the JWT carries token_scope=customer, rejecting staff tokens on portal endpoints.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CustomerAuthAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var scope = user.FindFirstValue("token_scope");
        if (!string.Equals(scope, "customer", StringComparison.Ordinal))
            context.Result = new ForbidResult();
    }
}
