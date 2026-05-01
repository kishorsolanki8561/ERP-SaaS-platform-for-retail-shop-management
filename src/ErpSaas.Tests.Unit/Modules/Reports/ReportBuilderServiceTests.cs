using ErpSaas.Infrastructure.Services;
using ErpSaas.Modules.Reports.Enums;
using ErpSaas.Modules.Reports.Services;
using ErpSaas.Shared.Data;
using ErpSaas.Shared.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErpSaas.Tests.Unit.Modules.Reports;

[Trait("Category", "Unit")]
public sealed class ReportBuilderServiceTests
{
    private readonly IReportQueryRepository _queries = Substitute.For<IReportQueryRepository>();
    private readonly ITenantContext _tenant = new StubTenantContext(1L);
    private readonly IErrorLogger _errorLogger = Substitute.For<IErrorLogger>();
    private readonly ReportBuilderService _sut;

    public ReportBuilderServiceTests()
    {
        _queries.QueryTrialBalanceAsync(Arg.Any<long>(), Arg.Any<DateRangeParams>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TrialBalanceRow>());
        _queries.QueryProfitLossAsync(Arg.Any<long>(), Arg.Any<DateRangeParams>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProfitLossRow>());
        _queries.QueryBalanceSheetAsync(Arg.Any<long>(), Arg.Any<DateRangeParams>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BalanceSheetRow>());
        _queries.QueryDayBookAsync(Arg.Any<long>(), Arg.Any<DateRangeParams>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DayBookEntry>());
        _queries.QueryLedgerAsync(Arg.Any<long>(), Arg.Any<long>(), Arg.Any<DateRangeParams>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LedgerEntry>());
        _queries.QueryGstr1B2bAsync(Arg.Any<long>(), Arg.Any<DateRangeParams>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GstR1B2bRow>());
        _queries.QueryHsnSummaryAsync(Arg.Any<long>(), Arg.Any<DateRangeParams>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<HsnSummaryRow>());

        _sut = new ReportBuilderService(_queries, _tenant, _errorLogger, NullLogger<ReportBuilderService>.Instance);
    }

    [Fact]
    public async Task ExportAsync_UnknownReportCode_ReturnsEmptySuccessFile()
    {
        var result = await _sut.ExportAsync("Unknown", new(DateTime.Today, DateTime.Today), ReportFormat.Csv);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task ExportAsync_PdfFormat_ReturnsPdfContentType()
    {
        var result = await _sut.ExportAsync("TrialBalance", new(DateTime.Today, DateTime.Today), ReportFormat.Pdf);
        Assert.True(result.IsSuccess);
        Assert.Equal("application/pdf", result.Value!.ContentType);
    }

    [Fact]
    public async Task ExportAsync_ExcelFormat_ReturnsXlsxContentType()
    {
        var result = await _sut.ExportAsync("ProfitLoss", new(DateTime.Today, DateTime.Today), ReportFormat.Excel);
        Assert.True(result.IsSuccess);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.Value!.ContentType);
    }

    [Fact]
    public async Task ExportAsync_CsvFormat_ReturnsCsvContentType()
    {
        var result = await _sut.ExportAsync("HsnSummary", new(DateTime.Today, DateTime.Today), ReportFormat.Csv);
        Assert.True(result.IsSuccess);
        Assert.Equal("text/csv", result.Value!.ContentType);
    }

    [Fact]
    public async Task ExportAsync_FileNameContainsReportCodeAndDates()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 3, 31);
        var result = await _sut.ExportAsync("DayBook", new(from, to), ReportFormat.Csv);
        Assert.True(result.IsSuccess);
        Assert.Contains("DayBook", result.Value!.FileName);
        Assert.Contains("20260101", result.Value!.FileName);
        Assert.Contains("20260331", result.Value!.FileName);
    }

    [Fact]
    public async Task GetTrialBalanceAsync_DelegatesToRepository()
    {
        var p = new DateRangeParams(DateTime.Today, DateTime.Today);
        await _sut.GetTrialBalanceAsync(p);
        await _queries.Received(1).QueryTrialBalanceAsync(1L, p, default);
    }

    [Fact]
    public async Task GetGstr1B2bAsync_DelegatesToRepository()
    {
        var p = new DateRangeParams(DateTime.Today, DateTime.Today);
        await _sut.GetGstr1B2bAsync(p);
        await _queries.Received(1).QueryGstr1B2bAsync(1L, p, default);
    }

    private sealed class StubTenantContext(long shopId) : ITenantContext
    {
        public long ShopId => shopId;
        public long CurrentUserId => 1L;
        public IReadOnlyList<string> CurrentUserRoles => [];
    }
}
