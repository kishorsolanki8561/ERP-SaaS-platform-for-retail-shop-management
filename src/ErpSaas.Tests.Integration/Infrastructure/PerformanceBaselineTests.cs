using System.Diagnostics;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Infrastructure;

/// <summary>
/// Smoke-level performance baselines for key read endpoints.
/// These tests do not replace load testing — they catch obvious regressions
/// (e.g., N+1 queries, missing indexes) before they reach staging.
///
/// Thresholds:
///   - Cold p1  (first request, no cache): &lt; 3 s
///   - Warm p95 (after warm-up, over 20 runs): &lt; 200 ms
///
/// Phase exit-gate: universal check #10 / <c>PerformanceBaselineTests</c>.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Performance")]
public sealed class PerformanceBaselineTests(IntegrationTestFixture fixture)
    : IClassFixture<IntegrationTestFixture>
{
    private const int WarmUpRuns = 3;
    private const int MeasureRuns = 20;
    private static readonly TimeSpan ColdMax = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan WarmP95Max = TimeSpan.FromMilliseconds(200);

    [Theory]
    [InlineData("/api/ddl/PAYMENT_MODE")]
    [InlineData("/api/masters/countries")]
    public async Task GetEndpoint_WarmP95_UnderThreshold(string path)
    {
        var client = fixture.CreateAuthenticatedClient();

        // ── Warm-up ───────────────────────────────────────────────────────────
        for (var i = 0; i < WarmUpRuns; i++)
        {
            var warmup = await client.GetAsync(path);
            warmup.IsSuccessStatusCode.Should().BeTrue(
                $"warm-up request to {path} must succeed (got {(int)warmup.StatusCode})");
        }

        // ── Measure ───────────────────────────────────────────────────────────
        var latencies = new List<long>(MeasureRuns);
        for (var i = 0; i < MeasureRuns; i++)
        {
            var sw = Stopwatch.StartNew();
            await client.GetAsync(path);
            sw.Stop();
            latencies.Add(sw.ElapsedMilliseconds);
        }

        latencies.Sort();
        var p95Index = (int)Math.Ceiling(MeasureRuns * 0.95) - 1;
        var p95Ms = latencies[p95Index];

        p95Ms.Should().BeLessThanOrEqualTo(
            (long)WarmP95Max.TotalMilliseconds,
            $"p95 latency for {path} was {p95Ms}ms — exceeds {WarmP95Max.TotalMilliseconds}ms threshold");
    }

    [Theory]
    [InlineData("/api/billing/invoices")]
    [InlineData("/api/wallet/balances")]
    [InlineData("/api/shifts")]
    [InlineData("/api/crm/customers")]
    [InlineData("/api/inventory/products")]
    public async Task TenantListEndpoint_ColdRequest_UnderThreshold(string path)
    {
        // A fresh client on each call → no connection reuse → approximates cold start
        var client = fixture.CreateAuthenticatedClient();
        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync(path);
        sw.Stop();

        // 401 / 403 are also fine for this test — we're only measuring latency, not auth
        ((int)response.StatusCode).Should().BeLessThan(500,
            $"{path} must not return 5xx (got {(int)response.StatusCode})");

        sw.Elapsed.Should().BeLessThan(
            ColdMax,
            $"cold request to {path} took {sw.ElapsedMilliseconds}ms — exceeds {ColdMax.TotalSeconds}s threshold");
    }
}
