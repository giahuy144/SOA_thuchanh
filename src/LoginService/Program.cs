using System.Text;
using LoginService.Data;
using LoginService.Models;
using LoginService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SOA.Shared;
using SOA.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
          ?? throw new InvalidOperationException("Thiếu cấu hình Jwt trong appsettings.json.");

builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenIssuer, JwtTokenIssuer>();
builder.Services.AddSingleton<ILoginAppService, LoginAppService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Login Service",
        Version = "v1",
        Description = "Dịch vụ đăng nhập: đăng ký, login và phát hành JWT."
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = JwtTokenHelper.CreateValidationParameters(jwt));
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddPolicy("Lab", policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

SeedDemoUsers(app.Services);

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Lab");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.File(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "index.html"), "text/html"));
app.MapGet("/health", () => Results.Ok(new { service = "LoginService", status = "ok" }));

app.Run();

static void SeedDemoUsers(IServiceProvider services)
{
    var store = services.GetRequiredService<IUserStore>();
    var hasher = services.GetRequiredService<IPasswordHasher>();
    store.Add(new UserAccount
    {
        UserName = "admin",
        FullName = "Quản trị viên",
        PasswordHash = hasher.Hash("Admin@123"),
        Role = "Admin"
    });
    store.Add(new UserAccount
    {
        UserName = "user",
        FullName = "Người dùng",
        PasswordHash = hasher.Hash("User@123"),
        Role = "User"
    });
}
