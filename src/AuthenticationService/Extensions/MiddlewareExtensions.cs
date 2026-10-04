using AuthenticationService.Middleware;

namespace AuthenticationService.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();

    public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder app) =>
        app.UseMiddleware<JwtAuthenticationMiddleware>();
}
