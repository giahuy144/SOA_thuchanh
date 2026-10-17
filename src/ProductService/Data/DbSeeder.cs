using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data;

/// <summary>
/// Tạo dữ liệu mẫu để kiểm thử nhanh các API (chỉ chạy khi bảng products còn rỗng).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(ProductDbContext db, CancellationToken ct = default)
    {
        if (await db.Products.AnyAsync(ct)) return;

        var now = DateTime.UtcNow;
        db.Products.AddRange(
            new Product
            {
                Name = "Laptop Dell XPS 13",
                Description = "Laptop 13 inch, Intel Core i7, RAM 16GB, SSD 512GB",
                Price = 28990000m,
                Quantity = 10,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                Name = "Chuột không dây Logitech MX Master 3",
                Description = "Chuột công thái học, kết nối Bluetooth/USB receiver",
                Price = 2490000m,
                Quantity = 35,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Product
            {
                Name = "Bàn phím cơ Keychron K2",
                Description = "Bàn phím cơ 75%, switch Gateron Brown, có LED nền",
                Price = 1990000m,
                Quantity = 20,
                CreatedAt = now,
                UpdatedAt = now
            });

        await db.SaveChangesAsync(ct);
    }
}
