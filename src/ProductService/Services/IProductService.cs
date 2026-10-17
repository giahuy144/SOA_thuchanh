using ProductService.Contracts;

namespace ProductService.Services;

/// <summary>
/// Nghiệp vụ quản lý sản phẩm. Controller chỉ lo HTTP, mọi xử lý nằm ở đây.
/// </summary>
public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default);

    Task<ProductResponse?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<ProductResponse> CreateAsync(ProductRequest request, CancellationToken ct = default);

    /// <summary>Trả về null nếu không tìm thấy sản phẩm.</summary>
    Task<ProductResponse?> UpdateAsync(int id, ProductRequest request, CancellationToken ct = default);

    /// <summary>Trả về false nếu không tìm thấy sản phẩm.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
