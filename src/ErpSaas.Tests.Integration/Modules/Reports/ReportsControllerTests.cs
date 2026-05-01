namespace ErpSaas.Tests.Integration.Modules.Reports;

[Trait("Category", "Integration")]
public sealed class ReportsControllerTests
{
    [Fact(Skip = "Phase 2 integration stubs — implement after containers wired")]
    public Task TrialBalance_WithoutAuth_Returns401() => Task.CompletedTask;

    [Fact(Skip = "Phase 2 integration stubs — implement after containers wired")]
    public Task TrialBalance_WithValidToken_Returns200() => Task.CompletedTask;

    [Fact(Skip = "Phase 2 integration stubs — implement after containers wired")]
    public Task Export_PdfFormat_ReturnsPdfFile() => Task.CompletedTask;

    [Fact(Skip = "Phase 2 integration stubs — implement after containers wired")]
    public Task Export_ExcelFormat_ReturnsXlsx() => Task.CompletedTask;

    [Fact(Skip = "Phase 2 integration stubs — implement after containers wired")]
    public Task Gstr1B2b_WithoutGstPermission_Returns403() => Task.CompletedTask;
}
