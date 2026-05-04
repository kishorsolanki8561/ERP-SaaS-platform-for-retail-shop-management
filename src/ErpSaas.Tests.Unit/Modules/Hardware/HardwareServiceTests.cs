using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Interceptors;
using ErpSaas.Modules.Hardware.Enums;
using ErpSaas.Modules.Hardware.Infrastructure;
using ErpSaas.Modules.Hardware.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Hardware;

// ── Test-local DbContext ───────────────────────────────────────────────────────

internal sealed class HardwareTenantDbContext(
    DbContextOptions<TenantDbContext> options,
    ITenantContext tenantContext,
    AuditSaveChangesInterceptor auditInterceptor,
    TenantSaveChangesInterceptor tenantInterceptor)
    : TenantDbContext(options, tenantContext, auditInterceptor, tenantInterceptor, [])
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        HardwareModelConfiguration.Configure(modelBuilder);
    }
}

// ── LabelTemplateService tests ─────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class LabelTemplateServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly LabelTemplateService _sut;
    private readonly SqliteConnection _connection;

    private const long ShopId = 1L;

    public LabelTemplateServiceTests()
    {
        var stubCtx = new StubTenantContext(ShopId);
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection).Options;

        _db = new HardwareTenantDbContext(opts, stubCtx,
            new AuditSaveChangesInterceptor(stubCtx),
            new TenantSaveChangesInterceptor(stubCtx));
        _db.Database.EnsureCreated();

        _sut = new LabelTemplateService(_db, _errorLogger, stubCtx);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidTemplate_ReturnsSuccessWithId()
    {
        var result = await _sut.CreateAsync(MakeCreateDto());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_PersistedTemplate_HasCorrectFields()
    {
        var id = (await _sut.CreateAsync(MakeCreateDto(name: "My Label"))).Value;

        var dto = await _sut.GetByIdAsync(id);
        dto.Should().NotBeNull();
        dto!.Name.Should().Be("My Label");
        dto.LabelType.Should().Be(LabelType.ProductTag);
    }

    [Fact]
    public async Task CreateAsync_SetDefault_ClearsOtherDefaults()
    {
        var id1 = (await _sut.CreateAsync(MakeCreateDto(isDefault: true))).Value;
        await _sut.CreateAsync(MakeCreateDto(isDefault: true));

        var first = await _sut.GetByIdAsync(id1);
        first!.IsDefault.Should().BeFalse();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingTemplate_UpdatesFields()
    {
        var id = (await _sut.CreateAsync(MakeCreateDto())).Value;

        var result = await _sut.UpdateAsync(id, new UpdateLabelTemplateDto("Updated Name", null, null, null, null));

        result.IsSuccess.Should().BeTrue();
        var dto = await _sut.GetByIdAsync(id);
        dto!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.UpdateAsync(9999L, new UpdateLabelTemplateDto(null, null, null, null, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingTemplate_Removes()
    {
        var id = (await _sut.CreateAsync(MakeCreateDto())).Value;

        var result = await _sut.DeleteAsync(id);

        result.IsSuccess.Should().BeTrue();
        var dto = await _sut.GetByIdAsync(id);
        dto.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.DeleteAsync(9999L);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── RenderAsync — ZPL renderer ────────────────────────────────────────────

    [Fact]
    public async Task RenderAsync_NoTemplateId_UsesDefaultOrBuiltIn_ReturnsZpl()
    {
        var request = new PrintLabelRequest(1L, "Widget A", "1234567890123", 299.99m, 1);

        var result = await _sut.RenderAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ZplContent.Should().StartWith("^XA");
        result.Value.ZplContent.Should().Contain("Widget A");
        result.Value.ZplContent.Should().Contain("1234567890123");
        result.Value.ZplContent.Should().Contain("299.99");
    }

    [Fact]
    public async Task RenderAsync_WithCustomTemplate_SubstitutesAllPlaceholders()
    {
        var id = (await _sut.CreateAsync(new CreateLabelTemplateDto(
            "Custom", LabelType.ProductTag, 50, 30,
            "^XA^FO10,10^FD{{productName}}^FS^FO10,40^FD{{barcode}}^FS^FO10,70^FDRs {{price}}^FS^XZ",
            null, true))).Value;

        var result = await _sut.RenderAsync(new PrintLabelRequest(
            2L, "Premium Cable", "BARCODEABC", 149.00m, 1, id));

        result.IsSuccess.Should().BeTrue();
        var zpl = result.Value!.ZplContent;
        zpl.Should().Contain("Premium Cable");
        zpl.Should().Contain("BARCODEABC");
        zpl.Should().Contain("149.00");
        zpl.Should().NotContain("{{productName}}");
        zpl.Should().NotContain("{{barcode}}");
        zpl.Should().NotContain("{{price}}");
    }

    [Fact]
    public async Task RenderAsync_TemplateNotFound_ReturnsNotFound()
    {
        var result = await _sut.RenderAsync(new PrintLabelRequest(1L, "X", "Y", 10m, 1, 9999L));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RenderAsync_ReturnsRequestedCopyCount()
    {
        var result = await _sut.RenderAsync(
            new PrintLabelRequest(1L, "Widget", "BAR001", 50m, 3));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Copies.Should().Be(3);
    }

    [Fact]
    public async Task RenderAsync_TsplTemplate_RenderedWhenPresent()
    {
        var id = (await _sut.CreateAsync(new CreateLabelTemplateDto(
            "Dual", LabelType.PriceTag, 40, 25,
            "^XA^FD{{productName}}^FS^XZ",
            "SIZE 40 mm,25 mm\r\nTEXT 10,10,\"0\",0,1,1,\"{{productName}}\"\r\nPRINT 1",
            false))).Value;

        var result = await _sut.RenderAsync(new PrintLabelRequest(1L, "Cable", "C1", 20m, 1, id));

        result.IsSuccess.Should().BeTrue();
        result.Value!.TsplContent.Should().Contain("Cable");
        result.Value.TsplContent.Should().NotContain("{{productName}}");
    }

    // ── ListAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_FilterByLabelType_ReturnsOnlyMatching()
    {
        await _sut.CreateAsync(MakeCreateDto(labelType: LabelType.ProductTag));
        await _sut.CreateAsync(MakeCreateDto(labelType: LabelType.PriceTag));

        var productTags = await _sut.ListAsync(LabelType.ProductTag);
        productTags.Should().HaveCount(1);
        productTags[0].LabelType.Should().Be(LabelType.ProductTag);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CreateLabelTemplateDto MakeCreateDto(
        string name = "Test Template",
        LabelType labelType = LabelType.ProductTag,
        bool isDefault = false) => new(
            name, labelType, 40, 25,
            "^XA^FD{{productName}}^FS^XZ",
            null, isDefault);

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}

// ── DeviceProfileService tests ─────────────────────────────────────────────────

[Trait("Category", "Unit")]
public class DeviceProfileServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly DeviceProfileService _sut;
    private readonly SqliteConnection _connection;

    private const long ShopId = 2L;

    public DeviceProfileServiceTests()
    {
        var stubCtx = new StubTenantContext(ShopId);
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection).Options;

        _db = new HardwareTenantDbContext(opts, stubCtx,
            new AuditSaveChangesInterceptor(stubCtx),
            new TenantSaveChangesInterceptor(stubCtx));
        _db.Database.EnsureCreated();

        _sut = new DeviceProfileService(_db, _errorLogger, stubCtx);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NewDevice_ReturnsSuccessWithId()
    {
        var result = await _sut.CreateAsync(MakeCreateDto());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateAsync_DuplicateDeviceId_ReturnsConflict()
    {
        await _sut.CreateAsync(MakeCreateDto("DEV-001"));

        var result = await _sut.CreateAsync(MakeCreateDto("DEV-001"));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingDevice_UpdatesRole()
    {
        var id = (await _sut.CreateAsync(MakeCreateDto())).Value;

        var result = await _sut.UpdateAsync(id,
            new UpdateDeviceProfileDto(null, null, null, "LabelPrinter", null, null));

        result.IsSuccess.Should().BeTrue();
        var dto = await _sut.GetByIdAsync(id);
        dto!.Role.Should().Be("LabelPrinter");
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNotFound()
    {
        var result = await _sut.UpdateAsync(9999L,
            new UpdateDeviceProfileDto(null, null, null, null, null, null));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingDevice_Removes()
    {
        var id = (await _sut.CreateAsync(MakeCreateDto())).Value;

        var result = await _sut.DeleteAsync(id);

        result.IsSuccess.Should().BeTrue();
        (await _sut.GetByIdAsync(id)).Should().BeNull();
    }

    // ── ListAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_MultipleDevices_ReturnsAll()
    {
        await _sut.CreateAsync(MakeCreateDto("DEV-A"));
        await _sut.CreateAsync(MakeCreateDto("DEV-B"));

        var list = await _sut.ListAsync();
        list.Should().HaveCount(2);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CreateDeviceProfileDto MakeCreateDto(string deviceId = "DEV-001") => new(
        deviceId, DeviceClass.Printer, "Epson", "TM-T82", "{}", "ReceiptPrinter");

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}

// ── ReceiptTemplateService / ESC-POS renderer tests ──────────────────────────

[Trait("Category", "Unit")]
public class ReceiptTemplateServiceTests : IDisposable
{
    private readonly TenantDbContext _db;
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ReceiptTemplateService _sut;
    private readonly SqliteConnection _connection;

    private const long ShopId = 3L;

    public ReceiptTemplateServiceTests()
    {
        var stubCtx = new StubTenantContext(ShopId);
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<TenantDbContext>()
            .UseSqlite(_connection).Options;

        _db = new HardwareTenantDbContext(opts, stubCtx,
            new AuditSaveChangesInterceptor(stubCtx),
            new TenantSaveChangesInterceptor(stubCtx));
        _db.Database.EnsureCreated();

        _sut = new ReceiptTemplateService(_db, _errorLogger, stubCtx);
    }

    public void Dispose() { _db.Dispose(); _connection.Dispose(); }

    // ── RenderAsync — ESC/POS ─────────────────────────────────────────────────

    [Fact]
    public async Task RenderAsync_ValidRequest_ReturnsSuccessWithBase64()
    {
        var result = await _sut.RenderAsync(MakeReceiptRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value!.EscPosBase64.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RenderAsync_Base64IsValidBytes()
    {
        var result = await _sut.RenderAsync(MakeReceiptRequest());

        var act = () => Convert.FromBase64String(result.Value!.EscPosBase64);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task RenderAsync_BytesContainShopName()
    {
        var result = await _sut.RenderAsync(MakeReceiptRequest(shopName: "GreenMart"));

        var bytes = Convert.FromBase64String(result.Value!.EscPosBase64);
        var text = System.Text.Encoding.ASCII.GetString(bytes);
        text.Should().Contain("GreenMart");
    }

    [Fact]
    public async Task RenderAsync_BytesContainInvoiceNumber()
    {
        var result = await _sut.RenderAsync(MakeReceiptRequest());

        var bytes = Convert.FromBase64String(result.Value!.EscPosBase64);
        var text = System.Text.Encoding.ASCII.GetString(bytes);
        text.Should().Contain("INV-00001");
    }

    [Fact]
    public async Task RenderAsync_TemplateNotFound_ReturnsNotFound()
    {
        var result = await _sut.RenderAsync(MakeReceiptRequest(templateId: 9999L));

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAsync_ValidTemplate_Persisted()
    {
        var dto = new CreateReceiptTemplateDto("80mm Default", "Retail80mm", "{}", "{}", true);

        var result = await _sut.CreateAsync(dto);

        result.IsSuccess.Should().BeTrue();
        var template = await _sut.GetByIdAsync(result.Value);
        template!.Name.Should().Be("80mm Default");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PrintReceiptRequest MakeReceiptRequest(
        string shopName = "TestShop",
        long? templateId = null) => new(
            shopName,
            "123 Test Street",
            "22AAAAA0000A1Z5",
            "INV-00001",
            DateTime.UtcNow,
            "Walk-in Customer",
            [new ReceiptLineItem("Widget", 2m, "PCS", 100m, 200m)],
            200m, 36m, 236m,
            "Cash",
            templateId);

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
