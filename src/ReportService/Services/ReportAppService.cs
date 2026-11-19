using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReportService.Contracts;
using ReportService.Data;
using ReportService.Models;

namespace ReportService.Services;

public sealed class ReportAppService(ReportDbContext db, IReportSourceClient sources)
{
    private static readonly HashSet<string> SalesStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "Shipped", "Delivered" };

    public async Task<ProductReportResponse> GetProductsAsync(string token, ReportQuery query, CancellationToken ct)
    {
        ValidateRange(query);
        var (products, orders) = await GetSourcesAsync(token, ct);
        var costs = await db.ProductCosts.AsNoTracking().ToDictionaryAsync(cost => cost.ProductId, ct);
        var sales = AggregateSales(orders, query);
        var rows = products.OrderBy(product => product.Id).Select(product =>
        {
            sales.TryGetValue(product.Id, out var sold);
            costs.TryGetValue(product.Id, out var cost);
            var costOfGoods = cost is null ? 0 : cost.UnitCost * sold.Quantity;
            return new ProductReportRow(product.Id, product.Name, product.Quantity, product.Price,
                cost?.UnitCost, sold.Quantity, sold.Revenue, costOfGoods,
                sold.Quantity > 0 && cost is null ? null : sold.Revenue - costOfGoods);
        }).ToList();

        var summary = BuildSummary(rows.Sum(row => row.StockOnHand), rows.Sum(row => row.UnitsSold),
            rows.Sum(row => row.Revenue), rows.Sum(row => row.CostOfGoodsSold),
            rows.Count(row => row.UnitsSold > 0 && row.UnitCost is null),
            rows.Count, query);
        return new ProductReportResponse(summary, rows);
    }

    public async Task<ProductCost> SetProductCostAsync(int productId, decimal unitCost, string token, CancellationToken ct)
    {
        var products = await sources.GetProductsAsync(token, ct);
        var product = products.FirstOrDefault(item => item.Id == productId);
        if (product is null) throw new KeyNotFoundException($"Không tìm thấy sản phẩm id={productId} trong ProductService.");

        var cost = await db.ProductCosts.FirstOrDefaultAsync(item => item.ProductId == productId, ct);
        if (cost is null)
        {
            cost = new ProductCost { ProductId = productId };
            db.ProductCosts.Add(cost);
        }
        cost.ProductName = product.Name;
        cost.UnitCost = unitCost;
        cost.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return cost;
    }

    public async Task<IReadOnlyList<ProductCost>> GetProductCostsAsync(CancellationToken ct) =>
        await db.ProductCosts.AsNoTracking().OrderBy(item => item.ProductId).ToListAsync(ct);

    public async Task<OrderReportResponse> GetOrdersAsync(string token, ReportQuery query, CancellationToken ct)
    {
        ValidateRange(query);
        var (_, orders) = await GetSourcesAsync(token, ct);
        var costs = await db.ProductCosts.AsNoTracking().ToDictionaryAsync(cost => cost.ProductId, ct);
        var rows = orders.Where(order => IsInRange(order.CreatedAt, query))
            .OrderByDescending(order => order.CreatedAt).Select(order =>
            {
                var isSale = SalesStatuses.Contains(order.Status);
                var totalUnits = order.Items.Sum(item => item.Quantity);
                var revenue = isSale ? order.TotalAmount : 0m;
                var missingCost = isSale && order.Items.Any(item => !costs.ContainsKey(item.ProductId));
                var costOfGoods = isSale
                    ? order.Items.Where(item => costs.ContainsKey(item.ProductId))
                        .Sum(item => costs[item.ProductId].UnitCost * item.Quantity)
                    : 0m;
                return new OrderReportRow(order.Id, order.CustomerName, order.Status, order.CreatedAt,
                    totalUnits, revenue, costOfGoods, missingCost ? null : revenue - costOfGoods);
            }).ToList();

        var soldOrders = rows.Where(row => SalesStatuses.Contains(row.Status)).ToList();
        var missingProducts = orders.Where(order => SalesStatuses.Contains(order.Status) && IsInRange(order.CreatedAt, query))
            .SelectMany(order => order.Items).Where(item => !costs.ContainsKey(item.ProductId))
            .Select(item => item.ProductId).Distinct().Count();
        var summary = BuildSummary(0, soldOrders.Sum(row => row.TotalUnits), soldOrders.Sum(row => row.Revenue),
            soldOrders.Sum(row => row.CostOfGoodsSold), missingProducts,
            orders.Where(order => SalesStatuses.Contains(order.Status) && IsInRange(order.CreatedAt, query))
                .SelectMany(order => order.Items).Select(item => item.ProductId).Distinct().Count(), query);
        return new OrderReportResponse(summary, rows);
    }

    public async Task<SavedReportResponse> SaveProductReportAsync(ProductReportResponse report, CancellationToken ct) =>
        await SaveAsync("products", report, report.Summary.GeneratedAt, ct);

    public async Task<SavedReportResponse> SaveOrderReportAsync(OrderReportResponse report, CancellationToken ct) =>
        await SaveAsync("orders", report, report.Summary.GeneratedAt, ct);

    public async Task<IReadOnlyList<SavedReportResponse>> GetSavedReportsAsync(CancellationToken ct)
    {
        var snapshots = await db.ReportSnapshots.AsNoTracking().OrderByDescending(item => item.GeneratedAt).ToListAsync(ct);
        return snapshots.Select(item => new SavedReportResponse(item.Id, item.ReportType,
            DateTime.SpecifyKind(item.GeneratedAt, DateTimeKind.Utc), JsonDocument.Parse(item.PayloadJson).RootElement.Clone())).ToList();
    }

    private async Task<SavedReportResponse> SaveAsync(string type, object report, DateTime generatedAt, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(report);
        var snapshot = new ReportSnapshot { ReportType = type, GeneratedAt = generatedAt, PayloadJson = payload };
        db.ReportSnapshots.Add(snapshot);
        await db.SaveChangesAsync(ct);
        return new SavedReportResponse(snapshot.Id, snapshot.ReportType,
            DateTime.SpecifyKind(snapshot.GeneratedAt, DateTimeKind.Utc), report);
    }

    private async Task<(IReadOnlyList<ProductDto> Products, IReadOnlyList<OrderDto> Orders)> GetSourcesAsync(
        string token, CancellationToken ct)
    {
        // Query source services in parallel; neither shares the ReportService database.
        var productsTask = sources.GetProductsAsync(token, ct);
        var ordersTask = sources.GetOrdersAsync(token, ct);
        await Task.WhenAll(productsTask, ordersTask);
        return (await productsTask, await ordersTask);
    }

    private static Dictionary<int, (int Quantity, decimal Revenue)> AggregateSales(
        IReadOnlyList<OrderDto> orders, ReportQuery query)
    {
        var result = new Dictionary<int, (int Quantity, decimal Revenue)>();
        foreach (var order in orders.Where(order => SalesStatuses.Contains(order.Status) && IsInRange(order.CreatedAt, query)))
        foreach (var item in order.Items)
        {
            var current = result.GetValueOrDefault(item.ProductId);
            result[item.ProductId] = (current.Quantity + item.Quantity, current.Revenue + item.LineTotal);
        }
        return result;
    }

    private static ReportSummary BuildSummary(int stock, int sold, decimal revenue, decimal cost,
        int missingCost, int products, ReportQuery query) => new(
        DateTime.UtcNow, query.From, query.To, products, stock, sold, revenue, cost,
        missingCost > 0 ? null : revenue - cost, missingCost,
        "Đơn Shipped hoặc Delivered được tính là đã bán; Pending, Confirmed và Cancelled không tính doanh thu.");

    private static bool IsInRange(DateTime createdAt, ReportQuery query) =>
        (!query.From.HasValue || createdAt >= query.From.Value) &&
        (!query.To.HasValue || createdAt < query.To.Value);

    private static void ValidateRange(ReportQuery query)
    {
        if (query.From.HasValue && query.To.HasValue && query.From.Value >= query.To.Value)
            throw new ArgumentException("Thời điểm From phải nhỏ hơn To.");
    }
}
