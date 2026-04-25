using ErpSaas.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace ErpSaas.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<RequirePermissionAttribute>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequirePermissionAttribute requirement)
    {
        if (context.User.FindFirst("is_platform_admin")?.Value == "true")
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var permsClaim = context.User.FindFirst("perms")?.Value ?? "";
        var userPerms = permsClaim.Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (userPerms.Contains(requirement.PermissionCode, StringComparer.OrdinalIgnoreCase))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
