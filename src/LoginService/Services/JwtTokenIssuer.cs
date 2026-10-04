using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LoginService.Models;
using Microsoft.Extensions.Options;
using SOA.Shared;
using SOA.Shared.Security;

namespace LoginService.Services;

public interface IJwtTokenIssuer
{
    (string Token, DateTime ExpiresAtUtc) Issue(UserAccount user);
}

public sealed class JwtTokenIssuer : IJwtTokenIssuer
{
    private readonly JwtSettings _settings;

    public JwtTokenIssuer(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public (string Token, DateTime ExpiresAtUtc) Issue(UserAccount user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpireMinutes);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("fullName", user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return (JwtTokenHelper.CreateToken(_settings, claims, expiresAt), expiresAt);
    }
}
