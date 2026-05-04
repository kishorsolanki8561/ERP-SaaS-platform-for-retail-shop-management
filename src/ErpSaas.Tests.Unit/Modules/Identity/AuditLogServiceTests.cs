using ErpSaas.Infrastructure.Audit;
using ErpSaas.Shared.Services;
using FluentAssertions;

namespace ErpSaas.Tests.Unit.Modules.Identity;

[Trait("Category", "Unit")]
public class AuditFieldRegistryTests
{
    private class SampleEntity
    {
        [AuditField("Customer Name")]
        public string Name { get; set; } = "";

        [AuditField("Email Address")]
        public string? Email { get; set; }

        // No [AuditField] — should be excluded from diff
        public string InternalSecret { get; set; } = "";

        public long ShopId { get; set; }
    }

    [Fact]
    public void GetFields_ReturnOnlyAnnotatedProperties()
    {
        var fields = AuditFieldRegistry.GetFields(typeof(SampleEntity));

        fields.Should().HaveCount(2);
        fields.Select(f => f.PropertyName).Should().BeEquivalentTo(["Name", "Email"]);
        fields.Select(f => f.DisplayName).Should().BeEquivalentTo(["Customer Name", "Email Address"]);
    }

    [Fact]
    public void ComputeDiff_Insert_ReturnsAllAuditFields()
    {
        var newValues = """{"Name":"Alice","Email":"alice@test.com","InternalSecret":"s3cr3t","ShopId":"1"}""";

        var diff = AuditFieldRegistry.ComputeDiff(nameof(SampleEntity), oldJson: null, newJson: newValues);

        diff.Should().HaveCount(2);
        diff.Should().Contain(f => f.Field == "Name" && f.NewValue == "Alice" && f.OldValue == null);
        diff.Should().Contain(f => f.Field == "Email" && f.NewValue == "alice@test.com" && f.OldValue == null);
    }

    [Fact]
    public void ComputeDiff_Delete_ReturnsOldValuesOnly()
    {
        var oldValues = """{"Name":"Alice","Email":"alice@test.com","InternalSecret":"s3cr3t","ShopId":"1"}""";

        var diff = AuditFieldRegistry.ComputeDiff(nameof(SampleEntity), oldJson: oldValues, newJson: null);

        diff.Should().HaveCount(2);
        diff.Should().Contain(f => f.Field == "Name" && f.OldValue == "Alice" && f.NewValue == null);
    }

    [Fact]
    public void ComputeDiff_Update_ReturnsOnlyChangedAuditFields()
    {
        var oldValues = """{"Name":"Alice","Email":"alice@test.com","InternalSecret":"s3cr3t","ShopId":"1"}""";
        var newValues = """{"Name":"Alice Updated","Email":"alice@test.com","InternalSecret":"different","ShopId":"1"}""";

        var diff = AuditFieldRegistry.ComputeDiff(nameof(SampleEntity), oldJson: oldValues, newJson: newValues);

        // Only Name changed — Email unchanged, InternalSecret excluded because no [AuditField]
        diff.Should().HaveCount(1);
        diff[0].Field.Should().Be("Name");
        diff[0].OldValue.Should().Be("Alice");
        diff[0].NewValue.Should().Be("Alice Updated");
    }

    [Fact]
    public void ComputeDiff_NoChangesToAuditFields_ReturnsEmpty()
    {
        var oldValues = """{"Name":"Alice","Email":"alice@test.com","InternalSecret":"changed","ShopId":"99"}""";
        var newValues = """{"Name":"Alice","Email":"alice@test.com","InternalSecret":"different","ShopId":"99"}""";

        // InternalSecret and ShopId changed but neither has [AuditField]
        var diff = AuditFieldRegistry.ComputeDiff(nameof(SampleEntity), oldJson: oldValues, newJson: newValues);

        diff.Should().BeEmpty();
    }

    [Fact]
    public void ComputeDiff_MalformedJson_ReturnsEmpty()
    {
        var diff = AuditFieldRegistry.ComputeDiff(nameof(SampleEntity), oldJson: "not-json", newJson: "also-not-json");

        diff.Should().BeEmpty();
    }

    [Fact]
    public void GetFields_UnknownEntityName_ReturnsEmpty()
    {
        var fields = AuditFieldRegistry.GetFields("NonExistentEntityXyz");

        fields.Should().BeEmpty();
    }

    [Fact]
    public void GetFields_CachesResult_ReturnsSameInstance()
    {
        var first  = AuditFieldRegistry.GetFields(typeof(SampleEntity));
        var second = AuditFieldRegistry.GetFields(typeof(SampleEntity));

        first.Should().BeSameAs(second);
    }
}
