using Microsoft.EntityFrameworkCore;
using OrderService.Contracts;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Services;

public sealed class ProductServiceUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class OrderAppService(
    OrderDbContext db,
    IProductCatalogClient catalog,
    ILogger<OrderAppService> logger)
{
    public async Task<IReadOnlyList<OrderResponse>> GetAllAsync(CancellationToken ct) =>
        (await db.Orders.AsNoTracking().Include(order => order.Items).OrderByDescending(order => order.CreatedAt)
            .ToListAsync(ct)).Select(ToResponse).ToList();

    public async Task<OrderResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == id, ct);
        return order is null ? null : ToResponse(order);
    }

    public async Task<(OrderResponse? Order, string? Error)> CreateAsync(
        CreateOrderRequest request, string? bearerToken, CancellationToken ct)
    {
        // Merge duplicate product lines so stock is checked against the total requested quantity.
        var quantities = request.Items.GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) }).ToList();
        var items = new List<OrderItem>();

        foreach (var requested in quantities)
        {
            ProductSnapshot? product;
            try
            {
                product = await catalog.GetProductAsync(requested.ProductId, bearerToken, ct);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Không thể gọi ProductService khi tạo đơn hàng.");
                throw new ProductServiceUnavailableException("Không thể kết nối ProductService để kiểm tra sản phẩm.", ex);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                throw new ProductServiceUnavailableException("ProductService phản hồi quá thời gian cho phép.", ex);
            }

            if (product is null) return (null, $"Không tìm thấy sản phẩm id={requested.ProductId}.");
            if (product.Quantity < requested.Quantity)
                return (null, $"Sản phẩm '{product.Name}' (id={product.Id}) chỉ còn {product.Quantity}, không đủ số lượng {requested.Quantity}.");

            var lineTotal = decimal.Round(product.Price * requested.Quantity, 2, MidpointRounding.AwayFromZero);
            items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = requested.Quantity,
                LineTotal = lineTotal
            });
        }

        var now = DateTime.UtcNow;
        var order = new Order
        {
            CustomerName = request.CustomerName.Trim(),
            Status = OrderStatuses.Pending,
            Items = items,
            TotalAmount = items.Sum(item => item.LineTotal),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Đã tạo đơn hàng Id={OrderId} cho {CustomerName}", order.Id, order.CustomerName);
        return (ToResponse(order), null);
    }

    public async Task<OrderResponse?> UpdateStatusAsync(int id, string status, CancellationToken ct)
    {
        var order = await db.Orders.Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == id, ct);
        if (order is null) return null;

        order.Status = OrderStatuses.All.First(value => value.Equals(status, StringComparison.OrdinalIgnoreCase));
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Cập nhật trạng thái đơn hàng Id={OrderId}, Status={Status}", id, order.Status);
        return ToResponse(order);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(item => item.Id == id, ct);
        if (order is null) return false;
        db.Orders.Remove(order);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static OrderResponse ToResponse(Order order) => new(
        order.Id,
        order.CustomerName,
        order.Status,
        order.TotalAmount,
        DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc),
        DateTime.SpecifyKind(order.UpdatedAt, DateTimeKind.Utc),
        order.Items.OrderBy(item => item.Id).Select(item => new OrderItemResponse(
            item.ProductId, item.ProductName, item.UnitPrice, item.Quantity, item.LineTotal)).ToList());
}
