namespace ErpSaas.Modules.Identity.Services;

public record TokenPair(string AccessToken, string RefreshToken, DateTime AccessExpiresAtUtc);

public interface ITokenService
{
    TokenPair GenerateTokenPair(long userId, long shopId, string displayName, string? email,
        IEnumerable<string> permissionCodes, IEnumerable<string> featureCodes);

    string GenerateRefreshToken();
    string HashToken(string rawToken);
}
