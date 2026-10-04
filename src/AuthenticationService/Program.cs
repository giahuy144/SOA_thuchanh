using AuthenticationService.Extensions;
using Microsoft.OpenApi.Models;
using SOA.Shared;
using SOA.Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Authentication Service",
        Version = "v1",
        Description = "Dịch vụ xác thực người dùng: middleware JWT, phân quyền, endpoint được bảo vệ."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddCors(options => options.AddPolicy("Lab", policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Lab");
app.UseRequestLogging();
app.UseJwtAuthentication();
app.MapControllers();

var auth = app.MapGroup("/api/auth").WithTags("Auth");
auth.MapGet("/health", () => Results.Ok(new { service = "AuthenticationService", status = "ok" }));
auth.MapGet("/validate", (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        message = "Token hợp lệ.",
        userName = context.User.Identity.Name,
        role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
    });
});

app.MapGet("/health", () => Results.Ok(new ApiMessage("AuthenticationService is running.")));

app.Run();
