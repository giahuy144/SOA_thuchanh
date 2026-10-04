using Microsoft.Extensions.Options;
using SOA.Shared;
using SOA.Shared.Contracts;
using SOA.Shared.Security;

namespace AuthenticationService.Middleware;

public sealed class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JwtSettings _jwt;

    public JwtAuthenticationMiddleware(RequestDelegate next, IOptions<JwtSettings> jwt)
    {
        _next = next;
        _jwt = jwt.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsAnonymous(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ApiMessage("Thiếu JWT. Gửi header Authorization: Bearer {token}."));
            return;
        }

        var token = header["Bearer ".Length..].Trim();
        var principal = JwtTokenHelper.ValidateToken(token, _jwt);
        if (principal is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ApiMessage("JWT không hợp lệ hoặc đã hết hạn."));
            return;
        }

        context.User = principal;
        await _next(context);
    }

    private static bool IsAnonymous(PathString path) =>
        path.StartsWithSegments("/swagger")
        || path.StartsWithSegments("/health")
        || path.Equals("/api/auth/health");
}
