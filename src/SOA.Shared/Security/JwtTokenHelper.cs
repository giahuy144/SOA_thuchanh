using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SOA.Shared.Security;

public static class JwtTokenHelper
{
    public static string CreateToken(JwtSettings settings, IEnumerable<Claim> claims, DateTime expiresAtUtc)
    {
        var credentials = new SigningCredentials(CreateSigningKey(settings.Key), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static ClaimsPrincipal? ValidateToken(string token, JwtSettings settings)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            return handler.ValidateToken(token, CreateValidationParameters(settings), out _);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }

    public static TokenValidationParameters CreateValidationParameters(JwtSettings settings) => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = settings.Issuer,
        ValidAudience = settings.Audience,
        IssuerSigningKey = CreateSigningKey(settings.Key),
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    private static SymmetricSecurityKey CreateSigningKey(string key) =>
        new(Encoding.UTF8.GetBytes(key));
}
