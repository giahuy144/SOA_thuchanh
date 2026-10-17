using System.ComponentModel.DataAnnotations;

namespace ProductService.Contracts;

/// <summary>
/// Dữ liệu client gửi lên khi tạo / cập nhật sản phẩm.
/// Client KHÔNG được tự đặt id, created_at, updated_at - service tự sinh.
/// </summary>
public sealed class ProductRequest
{
    [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Tên sản phẩm phải từ 1 đến 255 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Mô tả tối đa 2000 ký tự.")]
    public string? Description { get; set; }

    [Range(0, 99999999.99, ErrorMessage = "Giá phải nằm trong khoảng 0 - 99.999.999,99.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được âm.")]
    public int Quantity { get; set; }
}

/// <summary>
/// Dữ liệu service trả về cho client (không lộ chi tiết entity).
/// </summary>
public sealed record ProductResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Quantity,
    DateTime CreatedAt,
    DateTime UpdatedAt);
