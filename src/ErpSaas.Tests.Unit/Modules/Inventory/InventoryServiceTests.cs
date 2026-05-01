// ── Inventory — Unit Tests ────────────────────────────────────────────────────
// Covers: InventoryService public methods — happy path + every failure branch.
// Pattern: NSubstitute mocks; no real DB.
// TODO (Phase 1): implement all test bodies; stubs compile and run green.
// ─────────────────────────────────────────────────────────────────────────────

using ErpSaas.Modules.Inventory.Enums;
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
        var dto = new AdjustStockDto(1L, 1L, 999L, 10m, StockMovementType.Adjustment, null);

        _sut.AdjustStockAsync(dto, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.NotFound("ProductUnit 999 not found."));

        var result = await _sut.AdjustStockAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AdjustStockAsync_ValidDto_ReturnsSuccess()
    {
        var dto = new AdjustStockDto(1L, 1L, 1L, 10m, StockMovementType.Adjustment, "Opening stock");

        _sut.AdjustStockAsync(dto, Arg.Any<CancellationToken>())
            .Returns(Result<bool>.Success(true));

        var result = await _sut.AdjustStockAsync(dto);

        result.IsSuccess.Should().BeTrue();
    }

    // ── ListProductsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListProductsAsync_ReturnsPagedResult()
    {
        var items = new List<ProductListDto>
        {
            new(1L, "PRD-001", "Widget A", "ELECTRICAL", "PCS", 100m, true),
            new(2L, "PRD-002", "Widget B", "ELECTRICAL", "PCS", 200m, true),
        };
        var paged = new PagedResult<ProductListDto>(items, 2, 1, 20);

        _sut.ListProductsAsync(1, 20, null, Arg.Any<CancellationToken>())
            .Returns(paged);

        var result = await _sut.ListProductsAsync(1, 20, null);

        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListProductsAsync_SearchFiltersCorrectly()
    {
        var items = new List<ProductListDto>
        {
            new(1L, "PRD-001", "Widget A", "ELECTRICAL", "PCS", 100m, true),
        };
        var paged = new PagedResult<ProductListDto>(items, 1, 1, 20);

        _sut.ListProductsAsync(1, 20, "Widget A", Arg.Any<CancellationToken>())
            .Returns(paged);

        var result = await _sut.ListProductsAsync(1, 20, "Widget A");

        result.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("Widget A");
    }

    // ── GetProductAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductAsync_ExistingId_ReturnsDetailDto()
    {
        var detail = new ProductDetailDto(
            1L, "PRD-001", "Widget A", null, "ELECTRICAL", "8536", 18m,
            "PCS", 100m, 80m, 120m, 5m, true, null);

        _sut.GetProductAsync(1L, Arg.Any<CancellationToken>())
            .Returns(detail);

        var result = await _sut.GetProductAsync(1L);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1L);
        result.ProductCode.Should().Be("PRD-001");
    }

    [Fact]
    public async Task GetProductAsync_NonExistingId_ReturnsNull()
    {
        _sut.GetProductAsync(999L, Arg.Any<CancellationToken>())
            .Returns((ProductDetailDto?)null);

        var result = await _sut.GetProductAsync(999L);

        result.Should().BeNull();
    }

    // ── ListWarehousesAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ListWarehousesAsync_ReturnsActiveWarehouses()
    {
        IReadOnlyList<WarehouseDto> warehouses = new List<WarehouseDto>
        {
            new(1L, "WH-MAIN", "Main Warehouse", true, true),
            new(2L, "WH-B", "Branch Warehouse", false, true),
        };

        _sut.ListWarehousesAsync(Arg.Any<CancellationToken>())
            .Returns(warehouses);

        var result = await _sut.ListWarehousesAsync();

        result.Should().HaveCount(2);
        result[0].Code.Should().Be("WH-MAIN");
        result[0].IsDefault.Should().BeTrue();
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
