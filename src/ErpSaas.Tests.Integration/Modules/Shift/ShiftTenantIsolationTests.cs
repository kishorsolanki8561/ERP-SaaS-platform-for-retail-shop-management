using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Shift;

/// <summary>
/// Verifies that shifts created in Shop A are never visible to Shop B,
/// and that mutations from Shop B cannot affect Shop A's shifts.
///
/// Full implementation requires <c>IntegrationTestFixture</c> with two
/// pre-onboarded shops — deferred to Phase 1.
/// </summary>
[Trait("Category", "Integration")]
public class ShiftTenantIsolationTests
{
    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task ListShifts_ShopA_DoesNotReturnShopBShifts()
    {
        // Arrange: open shift in ShopA, authenticate as ShopB
        // Act: GET /api/shift
        // Assert: result does not contain ShopA shift
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task GetShiftSummary_ShopBCannotReadShopAShift_Returns404()
    {
        // Arrange: open shift in ShopA, get its Id
        // Act: GET /api/shift/{shopA_shiftId} authenticated as ShopB
        // Assert: 404 (not 403 — must not leak existence)
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task CloseShift_ShopBCannotCloseShopAShift_Returns404()
    {
        // Arrange: open shift in ShopA, get its Id
        // Act: POST /api/shift/{shopA_shiftId}/close as ShopB
        // Assert: 404
        await Task.CompletedTask;
    }

    [Fact(Skip = "Requires IntegrationTestFixture with two shops — Phase 1")]
    public async Task ForceClose_ShopBCannotForceCloseShopAShift_Returns404()
    {
        // Arrange: open shift in ShopA
        // Act: POST /api/shift/{shopA_shiftId}/force-close as ShopB
        // Assert: 404
        await Task.CompletedTask;
    }
}
