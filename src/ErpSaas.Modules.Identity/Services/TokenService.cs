using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ErpSaas.Modules.Identity.Services;

public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    private readonly string _secret = configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured");
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "shopearth-erp";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "shopearth-erp-clients";
    private readonly int _accessTokenMinutes = int.Parse(configuration["Jwt:AccessTokenMinutes"] ?? "15");

    public TokenPair GenerateTokenPair(
        long userId, long shopId, string displayName, string? email,
        IEnumerable<string> permissionCodes, IEnumerable<string> featureCodes,
        bool isPlatformAdmin = false)
    {
        var perms = string.Join(",", permissionCodes);
        var feats = string.Join(",", featureCodes);
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_accessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new("shop_id", shopId.ToString()),
            new(JwtRegisteredClaimNames.Name, displayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (!string.IsNullOrEmpty(email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));

        if (!string.IsNullOrEmpty(perms))
            claims.Add(new Claim("perms", perms));

        if (!string.IsNullOrEmpty(feats))
            claims.Add(new Claim("feats", feats));

        if (isPlatformAdmin)
            claims.Add(new Claim("is_platform_admin", "true"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        return new TokenPair(accessToken, refreshToken, expires);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
