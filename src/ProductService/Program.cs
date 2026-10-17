using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using ProductService.Data;
using ProductService.Services;
using SOA.Shared;
using SOA.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1) Cấu hình đọc từ appsettings.json
// ---------------------------------------------------------------------------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
          ?? throw new InvalidOperationException("Thiếu cấu hình Jwt trong appsettings.json.");

if (string.IsNullOrWhiteSpace(jwt.Key))
{
    throw new InvalidOperationException(
        "Jwt:Key chưa được cấu hình. Phải dùng ĐÚNG key của LoginService/AuthenticationService thì token mới xác thực được.");
}

// ---------------------------------------------------------------------------
// 2) Database riêng của ProductService (SQLite) - service tự quản lý dữ liệu
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("ProductDb")
                       ?? "Data Source=products.db";

builder.Services.AddDbContext<ProductDbContext>(options => options.UseSqlite(connectionString));

// ---------------------------------------------------------------------------
// 3) Xác thực: chấp nhận JWT do LoginService (bài thực hành số 2) phát hành
//    -> dùng chung JwtSettings + JwtTokenHelper trong SOA.Shared
// ---------------------------------------------------------------------------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = JwtTokenHelper.CreateValidationParameters(jwt);

        // Trả lỗi JSON thống nhất thay vì body rỗng
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Chưa xác thực. Gửi header 'Authorization: Bearer {token}' với token lấy từ LoginService."
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Token hợp lệ nhưng không đủ quyền thực hiện thao tác này."
                });
            }
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// 4) Nghiệp vụ + MVC + Swagger
// ---------------------------------------------------------------------------
builder.Services.AddScoped<IProductService, ProductAppService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product Service",
        Version = "v1",
        Description = "Bài thực hành số 3 - Dịch vụ quản lý sản phẩm (CRUD) hoạt động độc lập, "
                      + "có database riêng và xác thực bằng JWT do LoginService cấp."
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

// ---------------------------------------------------------------------------
// 5) Tự tạo database + bảng products và seed dữ liệu mẫu ở lần chạy đầu
// ---------------------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAsync(db);
}

// ---------------------------------------------------------------------------
// 6) Pipeline
// ---------------------------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service v1");
    options.DocumentTitle = "Product Service - SOA Lab 3";
});

app.UseCors("Lab");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Endpoint kiểm tra service đang chạy (không cần token)
app.MapGet("/health", () => Results.Ok(new
{
    service = "ProductService",
    status = "ok",
    database = "SQLite (products.db)",
    timeUtc = DateTime.UtcNow
}));

app.Run();
