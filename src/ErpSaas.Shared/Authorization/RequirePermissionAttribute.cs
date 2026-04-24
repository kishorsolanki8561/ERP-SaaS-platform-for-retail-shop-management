using Microsoft.AspNetCore.Authorization;

namespace ErpSaas.Shared.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationRequirement
{
    public const string PolicyPrefix = "Permission:";

    public string PermissionCode { get; }

    public RequirePermissionAttribute(string permissionCode)
        : base(PolicyPrefix + permissionCode)
    {
        PermissionCode = permissionCode;
    }
}
