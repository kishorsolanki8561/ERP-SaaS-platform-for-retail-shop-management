using ErpSaas.Shared.Services;

namespace ErpSaas.Modules.Identity.Services;

public interface IShopAccessService
{
    Task<IReadOnlyList<ModuleAccessDto>> GetModuleAccessAsync(CancellationToken ct = default);
    Task<Result<bool>> SetModuleVisibilityAsync(SetModuleVisibilityDto dto, CancellationToken ct = default);
    Task<UserPermissionSummaryDto?> GetUserPermissionsAsync(long targetUserId, CancellationToken ct = default);
    Task<Result<bool>> SetUserPermissionOverrideAsync(long targetUserId, SetPermissionOverrideDto dto, CancellationToken ct = default);
    Task<Result<bool>> RemoveUserPermissionOverrideAsync(long targetUserId, string permissionCode, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetShopPlanFeaturesAsync(long shopId, CancellationToken ct = default);
}

public sealed record ModuleAccessDto(
    string FeatureCode,
    string Label,
    string Icon,
    bool IsInPlan,
    bool IsEffectivelyEnabled,
    bool HasOverride,
    bool? OverrideValue);

public sealed record SetModuleVisibilityDto(string FeatureCode, bool IsVisible);

public sealed record UserPermissionSummaryDto(
    long UserId,
    string DisplayName,
    IReadOnlyList<PermissionStatusDto> Permissions);

public sealed record PermissionStatusDto(
    string Code,
    string Label,
    string Module,
    bool IsFromRole,
    bool HasOverride,
    bool IsGranted);

public sealed record SetPermissionOverrideDto(string PermissionCode, bool IsGranted);
