using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models;

/// <summary>
/// Thực thể sản phẩm - ánh xạ 1-1 với bảng "products" trong database riêng của ProductService.
/// </summary>
[Table("products")]
public class Product
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(255), Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("price", TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
