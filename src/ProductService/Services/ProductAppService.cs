using Microsoft.EntityFrameworkCore;
using ProductService.Contracts;
using ProductService.Data;
using ProductService.Models;

namespace ProductService.Services;

public sealed class ProductAppService(ProductDbContext db, ILogger<ProductAppService> logger) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var products = await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync(ct);

        return products.Select(ToResponse).ToList();
    }

    public async Task<ProductResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return product is null ? null : ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(ProductRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            Quantity = request.Quantity,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Đã tạo sản phẩm mới Id={Id}, Name={Name}", product.Id, product.Name);
        return ToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(int id, ProductRequest request, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return null;

        product.Name = request.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Price = request.Price;
        product.Quantity = request.Quantity;
        product.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Đã cập nhật sản phẩm Id={Id}", product.Id);
        return ToResponse(product);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return false;

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Đã xóa sản phẩm Id={Id}", id);
        return true;
    }

    /// <summary>
    /// SQLite lưu DateTime dạng không có Kind, nên gắn lại Utc để JSON trả về đúng chuẩn ISO-8601 có hậu tố Z.
    /// </summary>
    private static ProductResponse ToResponse(Product p) => new(
        p.Id,
        p.Name,
        p.Description,
        p.Price,
        p.Quantity,
        DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc),
        DateTime.SpecifyKind(p.UpdatedAt, DateTimeKind.Utc));
}
