using ErpSaas.Infrastructure.Data;
using ErpSaas.Infrastructure.Data.Entities.Portal;
using ErpSaas.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ErpSaas.Modules.CustomerPortal.Services;

public sealed class CustomerPortalAuthService(
    PlatformDbContext db,
    IConfiguration configuration,
    IErrorLogger errorLogger,
    ILogger<CustomerPortalAuthService> logger) : ICustomerPortalAuthService
{
    private const int OtpLength = 6;
    private const int OtpExpiryMinutes = 10;
    private const int AccessTokenMinutes = 15;
    private const int RefreshTokenDays = 30;

    public async Task<Result<OtpChallengeResult>> RequestOtpAsync(string identifier, CancellationToken ct = default)
    {
        try
        {
            var customer = await db.PlatformCustomers
                .FirstOrDefaultAsync(c => c.Phone == identifier || c.Email == identifier, ct);

            if (customer is null)
            {
                customer = new PlatformCustomer
                {
                    Phone = identifier.Contains('@') ? null : identifier,
                    Email = identifier.Contains('@') ? identifier : null,
                    DisplayName = identifier,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                db.PlatformCustomers.Add(customer);
                await db.SaveChangesAsync(ct);
            }

            var otp = GenerateOtp();
            var tokenHash = HashToken(otp);

            var session = new CustomerLoginSession
            {
                PlatformCustomerId = customer.Id,
                TokenHash = tokenHash,
                Purpose = "OtpChallenge",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.CustomerLoginSessions.Add(session);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("OTP challenge created for customer {Id} (identifier={Identifier})", customer.Id, identifier);

            // In production this OTP is sent via SMS/Email through ThirdPartyApiClientBase.
            // For dev/staging the OTP is returned directly in the response.
            return Result<OtpChallengeResult>.Success(new OtpChallengeResult(otp, session.ExpiresAtUtc));
        }
        catch (Exception ex)
        {
            await errorLogger.LogAsync("CustomerPortal.RequestOtp", ex, ct);
            return Result<OtpChallengeResult>.Failure("PORTAL_001");
        }
    }

    public async Task<Result<CustomerTokenResult>> VerifyOtpAsync(string identifier, string otp, string? deviceFingerprint, CancellationToken ct = default)
    {
        try
        {
            var customer = await db.PlatformCustomers
                .FirstOrDefaultAsync(c => c.Phone == identifier || c.Email == identifier, ct);

            if (customer is null || !customer.IsActive)
                return Result<CustomerTokenResult>.Unauthorized(Shared.Messages.Errors.Auth.InvalidCredentials);

            var hash = HashToken(otp);
            var session = await db.CustomerLoginSessions
                .Where(s => s.PlatformCustomerId == customer.Id
                         && s.TokenHash == hash
                         && s.Purpose == "OtpChallenge"
                         && s.ConsumedAtUtc == null
                         && s.ExpiresAtUtc > DateTime.UtcNow)
                .FirstOrDefaultAsync(ct);

            if (session is null)
                return Result<CustomerTokenResult>.Unauthorized(Shared.Messages.Errors.Auth.InvalidCredentials);

            session.ConsumedAtUtc = DateTime.UtcNow;
            customer.LastLoginAtUtc = DateTime.UtcNow;

            var refreshToken = GenerateRefreshToken();
            var refreshSession = new CustomerLoginSession
            {
                PlatformCustomerId = customer.Id,
                TokenHash = HashToken(refreshToken),
                Purpose = "RefreshToken",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays),
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.CustomerLoginSessions.Add(refreshSession);
            await db.SaveChangesAsync(ct);

            var accessToken = BuildJwt(customer);
            return Result<CustomerTokenResult>.Success(new CustomerTokenResult(accessToken, refreshToken, customer.Id, customer.DisplayName));
        }
        catch (Exception ex)
        {
            await errorLogger.LogAsync("CustomerPortal.VerifyOtp", ex, ct);
            return Result<CustomerTokenResult>.Failure("PORTAL_002");
        }
    }

    public async Task<Result<CustomerTokenResult>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        try
        {
            var hash = HashToken(refreshToken);
            var session = await db.CustomerLoginSessions
                .Include(s => s.PlatformCustomer)
                .Where(s => s.TokenHash == hash
                         && s.Purpose == "RefreshToken"
                         && s.ConsumedAtUtc == null
                         && s.ExpiresAtUtc > DateTime.UtcNow)
                .FirstOrDefaultAsync(ct);

            if (session is null || !session.PlatformCustomer.IsActive)
                return Result<CustomerTokenResult>.Failure(Shared.Messages.Errors.Auth.InvalidRefreshToken);

            session.ConsumedAtUtc = DateTime.UtcNow;

            var newRefreshToken = GenerateRefreshToken();
            db.CustomerLoginSessions.Add(new CustomerLoginSession
            {
                PlatformCustomerId = session.PlatformCustomerId,
                TokenHash = HashToken(newRefreshToken),
                Purpose = "RefreshToken",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenDays),
                CreatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(ct);

            var accessToken = BuildJwt(session.PlatformCustomer);
            return Result<CustomerTokenResult>.Success(new CustomerTokenResult(accessToken, newRefreshToken, session.PlatformCustomerId, session.PlatformCustomer.DisplayName));
        }
        catch (Exception ex)
        {
            await errorLogger.LogAsync("CustomerPortal.Refresh", ex, ct);
            return Result<CustomerTokenResult>.Failure("PORTAL_003");
        }
    }

    public async Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        try
        {
            var hash = HashToken(refreshToken);
            var session = await db.CustomerLoginSessions
                .Where(s => s.TokenHash == hash && s.Purpose == "RefreshToken" && s.ConsumedAtUtc == null)
                .FirstOrDefaultAsync(ct);

            if (session is not null)
            {
                session.ConsumedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await errorLogger.LogAsync("CustomerPortal.Logout", ex, ct);
            return Result<bool>.Failure("PORTAL_004");
        }
    }

    private string BuildJwt(PlatformCustomer customer)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("token_scope", "customer"),
            new("customer_id", customer.Id.ToString()),
            new("display_name", customer.DisplayName),
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateOtp() =>
        RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();

    private static string GenerateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
