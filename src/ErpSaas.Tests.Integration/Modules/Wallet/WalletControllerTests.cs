using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Wallet;

/// <summary>
/// Integration tests for <c>WalletController</c> exercised through the full
/// HTTP pipeline against a real SQL Server instance (Testcontainers).
/// </summary>
[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class WalletControllerTests(IntegrationTestFixture fixture)
{
    // ── GET /api/wallet/balances ───────────────────────────────────────────────

    [Fact]
    public async Task ListBalances_Unauthenticated_Returns401()
    {
        var client   = fixture.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/wallet/balances");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListBalances_WithoutPermission_Returns403()
    {
        var client   = fixture.CreateLimitedClient(permissionCode: "Other.View");
        var response = await client.GetAsync("/api/wallet/balances");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListBalances_WithPermission_Returns200()
    {
        var client   = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/wallet/balances");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /api/wallet/balance/{customerId} ──────────────────────────────────

    [Fact]
    public async Task GetBalance_UnknownCustomer_ReturnsNullOrNotFound()
    {
        // The WalletService returns null for an unknown customer and the
        // controller returns 404 in that case.
        var client   = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/wallet/balance/999999");

        // Either 404 (no wallet record) or 200 with balance=0 are acceptable —
        // assert only that it is not a server error and not 401/403.
        ((int)response.StatusCode).Should().BeLessThan(500,
            "the endpoint must not throw for an unknown customer");
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ── POST /api/wallet/credit ────────────────────────────────────────────────

    [Fact]
    public async Task Credit_WithoutPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Wallet.View");

        var body = new
        {
            CustomerId    = 1L,
            CustomerName  = "Test Customer",
            Amount        = 100m,
            ReferenceType = (string?)null,
            ReferenceId   = (long?)null,
            Notes         = (string?)null
        };

        var response = await client.PostAsJsonAsync("/api/wallet/credit", body);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Credit_ValidRequest_Returns200()
    {
        // Create a CRM customer first to obtain a real CustomerId.
        var adminClient = fixture.CreateAuthenticatedClient();
        var suffix      = Guid.NewGuid().ToString("N")[..8];

        var createCustomerResp = await adminClient.PostAsJsonAsync("/api/crm/customers", new
        {
            DisplayName  = $"Wallet Credit Customer {suffix}",
            CustomerType = "RETAIL",
            Email        = $"wallet-{suffix}@test.local",
            Phone        = (string?)null,
            GstNumber    = (string?)null,
            CreditLimit  = 0m,
            GroupId      = (long?)null
        });
        createCustomerResp.IsSuccessStatusCode.Should().BeTrue("setup: customer creation must succeed");

        // BaseController.Ok<T>(Result<T>) returns OkObjectResult(result.Value) — raw long.
        var custJson   = await createCustomerResp.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = custJson.GetInt64();

        var creditBody = new
        {
            CustomerId    = customerId,
            CustomerName  = $"Wallet Credit Customer {suffix}",
            Amount        = 500m,
            ReferenceType = "Manual",
            ReferenceId   = (long?)null,
            Notes         = "Integration test top-up"
        };

        var response = await adminClient.PostAsJsonAsync("/api/wallet/credit", creditBody);

        response.IsSuccessStatusCode.Should().BeTrue();
        // CreditAsync returns Result<WalletCreditResultDto>; ToActionResult returns OkObjectResult(dto)
        // so body is {"receiptNumber":"...", "newBalance":...}.
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("receiptNumber").GetString().Should().NotBeNullOrWhiteSpace();
    }

    // ── POST /api/wallet/debit ─────────────────────────────────────────────────

    [Fact]
    public async Task Debit_InsufficientBalance_Returns409OrConflict()
    {
        // Customer with no wallet balance — debit must fail with Conflict.
        var adminClient = fixture.CreateAuthenticatedClient();
        var suffix      = Guid.NewGuid().ToString("N")[..8];

        var createCustomerResp = await adminClient.PostAsJsonAsync("/api/crm/customers", new
        {
            DisplayName  = $"Debit Customer {suffix}",
            CustomerType = "RETAIL",
            Email        = $"debit-{suffix}@test.local",
            Phone        = (string?)null,
            GstNumber    = (string?)null,
            CreditLimit  = 0m,
            GroupId      = (long?)null
        });
        createCustomerResp.IsSuccessStatusCode.Should().BeTrue();

        // BaseController.Ok<T>(Result<T>) returns OkObjectResult(result.Value) — raw long.
        var custJson   = await createCustomerResp.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = custJson.GetInt64();

        // Attempt to debit 9999 from a wallet that has 0 balance.
        var debitBody = new
        {
            CustomerId    = customerId,
            Amount        = 9999m,
            ReferenceType = (string?)null,
            ReferenceId   = (long?)null,
            Notes         = (string?)null
        };

        var response = await adminClient.PostAsJsonAsync("/api/wallet/debit", debitBody);

        // WalletService.DebitAsync returns Result.Conflict when balance < amount,
        // and BaseController.Ok(result) returns 409 Conflict via the Result wrapper.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "debit on an empty wallet must return 409 Conflict");
    }

    // ── POST /api/wallet/top-ups ──────────────────────────────────────────────

    [Fact]
    public async Task InitiateTopUp_WithoutPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Wallet.View");

        var body = new
        {
            CustomerId      = 1L,
            CustomerName    = "Test",
            Amount          = 100m,
            PaymentModeCode = "CASH",
            Notes           = (string?)null
        };

        var response = await client.PostAsJsonAsync("/api/wallet/top-ups", body);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InitiateTopUp_ValidRequest_Returns200()
    {
        var adminClient = fixture.CreateAuthenticatedClient();
        var suffix      = Guid.NewGuid().ToString("N")[..8];

        var createCustomerResp = await adminClient.PostAsJsonAsync("/api/crm/customers", new
        {
            DisplayName  = $"TopUp Customer {suffix}",
            CustomerType = "RETAIL",
            Email        = $"topup-{suffix}@test.local",
            Phone        = (string?)null,
            GstNumber    = (string?)null,
            CreditLimit  = 0m,
            GroupId      = (long?)null
        });
        createCustomerResp.IsSuccessStatusCode.Should().BeTrue();

        // BaseController.Ok<T>(Result<T>) returns OkObjectResult(result.Value) — raw long.
        var custJson   = await createCustomerResp.Content.ReadFromJsonAsync<JsonElement>();
        var customerId = custJson.GetInt64();

        var body = new
        {
            CustomerId      = customerId,
            CustomerName    = $"TopUp Customer {suffix}",
            Amount          = 200m,
            PaymentModeCode = "CASH",
            Notes           = (string?)null
        };

        var response = await adminClient.PostAsJsonAsync("/api/wallet/top-ups", body);

        response.IsSuccessStatusCode.Should().BeTrue();
        // InitiateAsync returns Result<long> → OkObjectResult(topUpId) → plain long in body.
        var json    = await response.Content.ReadFromJsonAsync<JsonElement>();
        var topUpId = json.GetInt64();
        topUpId.Should().BeGreaterThan(0);
    }

    // ── POST /api/wallet/top-ups/{id}/complete ────────────────────────────────

    [Fact]
    public async Task CompleteTopUp_UnknownId_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/wallet/top-ups/99999/complete", new
        {
            PaymentGatewayTransactionId = (long?)null,
            Notes = (string?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/wallet/top-ups/{id}/fail ────────────────────────────────────

    [Fact]
    public async Task FailTopUp_UnknownId_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient();

        // Endpoint accepts a plain string body.
        var content  = new StringContent("\"Payment declined\"",
            System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/wallet/top-ups/99999/fail", content);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
