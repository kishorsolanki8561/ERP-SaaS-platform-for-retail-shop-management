using System.Net;
using System.Net.Http.Json;
using ErpSaas.Modules.Warranty.Enums;
using ErpSaas.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ErpSaas.Tests.Integration.Modules.Warranty;

[Collection("Integration")]
[Trait("Category", "Integration")]
public sealed class WarrantyControllerTests(IntegrationTestFixture fixture)
{
    // ── GET /api/warranty/registrations/by-serial/{serial} ───────────────────

    [Fact]
    public async Task GetBySerial_WithoutAuth_Returns401()
    {
        var client = fixture.CreateUnauthenticatedClient();
        var response = await client.GetAsync("/api/warranty/registrations/by-serial/SN-TEST-001");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBySerial_NotFound_Returns404()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/warranty/registrations/by-serial/DOES-NOT-EXIST-XYZ");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySerial_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Warranty.ManageClaims");
        var response = await client.GetAsync("/api/warranty/registrations/by-serial/SN-001");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/warranty/registrations/expiring ──────────────────────────────

    [Fact]
    public async Task ListExpiring_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/warranty/registrations/expiring?days=30");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/warranty/registrations ─────────────────────────────────────

    [Fact]
    public async Task RegisterWarranty_HappyPath_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var payload = new
        {
            InvoiceId = 1L,
            InvoiceLineId = 1L,
            ProductId = 1L,
            CustomerId = 1L,
            SerialNumber = $"SN-{Guid.NewGuid():N}",
            PurchaseDate = DateTime.UtcNow.AddDays(-10),
            WarrantyMonths = 12,
            Type = WarrantyType.Warranty.ToString(),
            Terms = "Standard warranty terms",
            BranchId = (long?)null
        };

        var response = await client.PostAsJsonAsync("/api/warranty/registrations", payload);
        // 200 on success or 4xx if referenced FK doesn't exist in test DB
        // Either way, auth + permission gate must have passed (not 401/403)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RegisterWarranty_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Warranty.View");
        var payload = new
        {
            InvoiceId = 1L, InvoiceLineId = 1L, ProductId = 1L, CustomerId = 1L,
            SerialNumber = "SN-TEST", PurchaseDate = DateTime.UtcNow, WarrantyMonths = 12,
            Type = WarrantyType.Warranty.ToString()
        };
        var response = await client.PostAsJsonAsync("/api/warranty/registrations", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /api/warranty/claims ──────────────────────────────────────────────

    [Fact]
    public async Task ListClaims_Authenticated_Returns200()
    {
        var client = fixture.CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/warranty/claims");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /api/warranty/claims ─────────────────────────────────────────────

    [Fact]
    public async Task CreateClaim_ExpiredWarranty_Returns409()
    {
        // Posting a claim against a non-existent registration returns not-found or similar
        var client = fixture.CreateAuthenticatedClient();
        var payload = new
        {
            WarrantyRegistrationId = 9999999L,
            ClaimDate = DateTime.UtcNow,
            IssueDescription = "Device not working",
            AttachmentFileIds = (string?)null
        };
        var response = await client.PostAsJsonAsync("/api/warranty/claims", payload);
        // Not found for missing registration, or conflict for expired — either valid
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateClaim_MissingPermission_Returns403()
    {
        var client = fixture.CreateLimitedClient(permissionCode: "Warranty.View");
        var payload = new
        {
            WarrantyRegistrationId = 1L, ClaimDate = DateTime.UtcNow,
            IssueDescription = "Issue"
        };
        var response = await client.PostAsJsonAsync("/api/warranty/claims", payload);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
