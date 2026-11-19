using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Models;

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { Pending, Confirmed, Shipped, Delivered, Cancelled };
}

[Table("orders")]
public sealed class Order
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(255), Column("customer_name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(50), Column("status")]
    public string Status { get; set; } = OrderStatuses.Pending;

    [Column("total_amount", TypeName = "decimal(12,2)")]
    public decimal TotalAmount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}

[Table("order_items")]
public sealed class OrderItem
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Column("order_id")]
    public int OrderId { get; set; }

    // ProductId is a reference by API identifier only; there is intentionally no cross-service DB FK.
    [Column("product_id")]
    public int ProductId { get; set; }

    [Required, MaxLength(255), Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [Column("unit_price", TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("line_total", TypeName = "decimal(12,2)")]
    public decimal LineTotal { get; set; }

    public Order Order { get; set; } = null!;
}
