namespace ErpSaas.Shared.Services;

/// <summary>
/// Cross-module contract: Billing verifies an open shift without depending on the Shift module.
/// </summary>
public interface IShiftLookup
{
    Task<bool> IsShiftOpenAsync(long shiftId, long shopId, CancellationToken ct = default);
}
