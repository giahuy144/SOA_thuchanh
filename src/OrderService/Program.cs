using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrderService.Data;
using OrderService.Services;
using SOA.Shared;
using SOA.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
          ?? throw new InvalidOperationException("Thiếu cấu hình Jwt trong appsettings.json.");
if (string.IsNullOrWhiteSpace(jwt.Key))
    throw new InvalidOperationException("Jwt:Key chưa được cấu hình.");

var connectionString = builder.Configuration.GetConnectionString("OrderDb") ?? "Data Source=orders.db";
builder.Services.AddDbContext<OrderDbContext>(options => options.UseSqlite(connectionString));

var productServiceUrl = builder.Configuration["Services:ProductServiceUrl"] ?? "http://localhost:5103/";
if (!Uri.TryCreate(productServiceUrl, UriKind.Absolute, out var productServiceUri))
    throw new InvalidOperationException("Services:ProductServiceUrl phải là URL tuyệt đối hợp lệ.");
builder.Services.AddHttpClient<IProductCatalogClient, ProductCatalogClient>(client =>
{
    client.BaseAddress = productServiceUri;
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
builder.Services.AddScoped<OrderAppService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Order Service",
        Version = "v1",
        Description = "Bài thực hành số 4 - dịch vụ quản lý đơn hàng độc lập, SQLite riêng, xác thực JWT và kiểm tra sản phẩm qua ProductService."
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
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service v1");
    options.DocumentTitle = "Order Service - SOA Lab 4";
});
app.UseCors("Lab");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new
{
    service = "OrderService",
    status = "ok",
    database = "SQLite (orders.db)",
    productService = productServiceUri.ToString(),
    swagger = "/swagger",
    timeUtc = DateTime.UtcNow
}));

app.Run();
