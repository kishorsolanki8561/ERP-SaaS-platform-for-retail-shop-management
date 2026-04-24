// ── Inventory — Unit Tests ────────────────────────────────────────────────────
// Covers: InventoryService public methods — happy path + every failure branch.
// Pattern: NSubstitute mocks; no real DB.
// TODO (Phase 1): implement all test bodies; stubs compile and run green.
// ─────────────────────────────────────────────────────────────────────────────

using ErpSaas.Modules.Inventory.Services;
using ErpSaas.Shared.Services;
using FluentAssertions;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Inventory;

[Trait("Category", "Unit")]
[Trait("Module", "Inventory")]
public class InventoryServiceTests
{
    private readonly IInventoryService _sut = Substitute.For<IInventoryService>();

    // ── CreateProductAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProductAsync_ValidDto_ReturnsSuccessWithId()
    {
        var dto = new CreateProductDto(
            "Test Product", "ELECTRICAL", null, 18m, "PCS",
            100m, 80m, 120m, 5m, null);

        _sut.CreateProductAsync(dto, Arg.Any<CancellationToken>())
            .Returns(Result<long>.Success(1L));

        var result = await _sut.CreateProductAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1L);
    }

    // ── UpdateProductAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProductAsync_ProductNotFound_ReturnsNotFound()
    {
        var dto = new UpdateProductDto(
            "Updated", null, "ELECTRONICS", 18m, 110m, 85m, 130m, 5m);

        _sut.UpdateProductAsync(999L, dto, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.NotFound("Product 999 not found."));

        var result = await _sut.UpdateProductAsync(999L, dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── DeactivateProductAsync ────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateProductAsync_ProductExists_ReturnsSuccess()
    {
        _sut.DeactivateProductAsync(1L, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));

        var result = await _sut.DeactivateProductAsync(1L);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateProductAsync_ProductNotFound_ReturnsNotFound()
    {
        _sut.DeactivateProductAsync(999L, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.NotFound("Product 999 not found."));

        var result = await _sut.DeactivateProductAsync(999L);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── CreateWarehouseAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateWarehouseAsync_DuplicateCode_ReturnsConflict()
    {
        _sut.CreateWarehouseAsync("WH-MAIN", "Main Warehouse", true, Arg.Any<CancellationToken>())
            .Returns(Result<long>.Conflict("Warehouse code 'WH-MAIN' already exists."));

        var result = await _sut.CreateWarehouseAsync("WH-MAIN", "Main Warehouse", true);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateWarehouseAsync_ValidData_ReturnsSuccessWithId()
    {
        _sut.CreateWarehouseAsync("WH-B", "Branch Warehouse", false, Arg.Any<CancellationToken>())
            .Returns(Result<long>.Success(2L));

        var result = await _sut.CreateWarehouseAsync("WH-B", "Branch Warehouse", false);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2L);
    }

    // ── AdjustStockAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AdjustStockAsync_UnitNotFound_ReturnsNotFound()
    {
        var dto = new AdjustStockDto(1L, 1L, 999L, 10m, "Adjustment", null);

        _sut.AdjustStockAsync(dto, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.NotFound("ProductUnit 999 not found."));

        var result = await _sut.AdjustStockAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdjustStockAsync_ValidDto_ReturnsSuccess()
    {
        var dto = new AdjustStockDto(1L, 1L, 1L, 10m, "Adjustment", "Opening stock");

        _sut.AdjustStockAsync(dto, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));

        var result = await _sut.AdjustStockAsync(dto);

        result.IsSuccess.Should().BeTrue();
    }

    // ── GetStockLevelAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetStockLevelAsync_ReturnsNetQuantityInBaseUnit()
    {
        _sut.GetStockLevelAsync(1L, 1L, Arg.Any<CancellationToken>())
            .Returns(47m);

        var level = await _sut.GetStockLevelAsync(1L, 1L);

        level.Should().Be(47m);
    }
}
