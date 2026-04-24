using BCrypt.Net;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Data.Entities.Subscription;
using ErpSaas.Shared.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ErpSaas.Modules.Identity.Seeds;

public sealed class IdentityDataSeeder(
    PlatformDbContext db,
    IConfiguration configuration,
    ILogger<IdentityDataSeeder> logger) : IDataSeeder
{
    public int Order => 20;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await SeedSubscriptionPlansAsync(ct);
            await SeedProductOwnerAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            logger.LogError(ex, "IdentityDataSeeder failed — rolled back");
            throw;
        }
    }

    private async Task SeedSubscriptionPlansAsync(CancellationToken ct)
    {
        var plans = new[]
        {
            ("Starter",    "Starter",    0m,    0m,    2),
            ("Growth",     "Growth",     999m,  9990m, 10),
            ("Enterprise", "Enterprise", 2999m, 29990m, 100),
        };

        foreach (var (code, label, monthly, annual, maxUsers) in plans)
        {
            if (!await db.SubscriptionPlans.AnyAsync(p => p.Code == code, ct))
            {
                db.SubscriptionPlans.Add(new SubscriptionPlan
                {
                    Code = code, Label = label,
                    MonthlyPrice = monthly, AnnualPrice = annual,
                    MaxUsers = maxUsers, IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
                logger.LogInformation("Seeding subscription plan: {Code}", code);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task SeedProductOwnerAsync(CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.IsPlatformAdmin, ct))
            return;

        var email    = configuration["PRODUCT_OWNER_EMAIL"];
        var name     = configuration["PRODUCT_OWNER_NAME"];
        var password = configuration["PRODUCT_OWNER_PASSWORD"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("PRODUCT_OWNER_EMAIL/PASSWORD not set — skipping PO seed");
            return;
        }

        var user = new User
        {
            Email = email,
            DisplayName = name ?? "Product Owner",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
            IsActive = true, IsPlatformAdmin = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var role = new Role
        {
            Code = "PlatformOwner", Label = "Platform Owner",
            IsSystemRole = true, CreatedAtUtc = DateTime.UtcNow
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        db.UserRoles.Add(new UserRole
        {
            UserId = user.Id, ShopId = 0, RoleId = role.Id,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seeded product owner: {Email}", email);
    }
}
