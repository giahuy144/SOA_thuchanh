using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ReportService.Data;
using ReportService.Services;
using SOA.Shared;
using SOA.Shared.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
          ?? throw new InvalidOperationException("Thiếu cấu hình Jwt trong appsettings.json.");
if (string.IsNullOrWhiteSpace(jwt.Key)) throw new InvalidOperationException("Jwt:Key chưa được cấu hình.");

var connectionString = builder.Configuration.GetConnectionString("ReportDb") ?? "Data Source=reports.db";
builder.Services.AddDbContext<ReportDbContext>(options => options.UseSqlite(connectionString));
RegisterServiceClient("ProductService", "Services:ProductServiceUrl", "http://localhost:5103/");
RegisterServiceClient("OrderService", "Services:OrderServiceUrl", "http://localhost:5104/");
builder.Services.AddScoped<IReportSourceClient, ReportSourceClient>();
builder.Services.AddScoped<ReportAppService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = JwtTokenHelper.CreateValidationParameters(jwt);
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new { message = "Chưa xác thực. Gửi header Authorization: Bearer {token}." });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new { message = "Token hợp lệ nhưng không đủ quyền thực hiện thao tác này." });
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Report Service",
        Version = "v1",
        Description = "Bài thực hành số 5 - báo cáo tồn kho, hàng bán, doanh thu, giá vốn và lợi nhuận; dữ liệu lấy qua API SOA và snapshot/giá vốn lưu trong SQLite riêng."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Dán JWT lấy từ POST http://localhost:5101/api/login vào đây.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme
            { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
          Array.Empty<string>() }
    });
});
builder.Services.AddCors(options => options.AddPolicy("Lab", policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReportDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Report Service v1");
    options.DocumentTitle = "Report Service - SOA Lab 5";
});
app.UseCors("Lab");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new
{
    service = "ReportService",
    status = "ok",
    database = "SQLite (reports.db)",
    productService = builder.Configuration["Services:ProductServiceUrl"],
    orderService = builder.Configuration["Services:OrderServiceUrl"],
    swagger = "/swagger",
    timeUtc = DateTime.UtcNow
}));
app.Run();

void RegisterServiceClient(string name, string configurationKey, string defaultUrl)
{
    var rawUrl = builder.Configuration[configurationKey] ?? defaultUrl;
    if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var baseAddress))
        throw new InvalidOperationException($"{configurationKey} phải là URL tuyệt đối hợp lệ.");
    builder.Services.AddHttpClient(name, client =>
    {
        client.BaseAddress = baseAddress;
        client.Timeout = TimeSpan.FromSeconds(10);
    });
}
