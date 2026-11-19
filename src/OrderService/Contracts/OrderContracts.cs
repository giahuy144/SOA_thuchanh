using System.ComponentModel.DataAnnotations;

namespace OrderService.Contracts;

public sealed class CreateOrderRequest
{
    [Required, StringLength(255, MinimumLength = 1)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MinLength(1, ErrorMessage = "Đơn hàng phải có ít nhất một sản phẩm.")]
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public sealed class CreateOrderItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 100000, ErrorMessage = "Số lượng đặt phải lớn hơn 0.")]
    public int Quantity { get; set; }
}

public sealed class UpdateOrderStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

public sealed record OrderItemResponse(int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal);

public sealed record OrderResponse(
    int Id,
    string CustomerName,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<OrderItemResponse> Items);
