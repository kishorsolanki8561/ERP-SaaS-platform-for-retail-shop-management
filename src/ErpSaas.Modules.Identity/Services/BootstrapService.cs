#pragma warning disable CS9107
using BCrypt.Net;
using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Identity;
using ErpSaas.Infrastructure.Services;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace ErpSaas.Modules.Identity.Services;

public sealed class BootstrapService(
    PlatformDbContext db,
    IErrorLogger errorLogger)
    : BaseService<PlatformDbContext>(db, errorLogger), IBootstrapService
{
    public async Task<bool> HasProductOwnerAsync(CancellationToken ct = default)
        => await db.Users.AnyAsync(u => u.IsPlatformAdmin, ct);

    public async Task<Result<long>> RegisterProductOwnerAsync(
        RegisterOwnerDto dto, CancellationToken ct = default)
    {
        return await ExecuteAsync<long>("Identity.RegisterProductOwner", async () =>
        {
            if (await db.Users.AnyAsync(u => u.IsPlatformAdmin, ct))
                return Result<long>.Conflict("A product owner already exists.");

            var user = new User
            {
                Email = dto.Email,
                DisplayName = dto.Name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12),
                IsActive = true,
                IsPlatformAdmin = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            return Result<long>.Success(user.Id);
        }, ct, useTransaction: true);
    }
}
