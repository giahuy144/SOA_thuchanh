using System.ComponentModel.DataAnnotations;

namespace ReportService.Contracts;

public sealed class SetProductCostRequest
{
    [Range(0, 99999999.99)]
    public decimal UnitCost { get; set; }
}

public sealed class ReportQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public sealed record ProductReportRow(
    int ProductId,
    string ProductName,
    int StockOnHand,
    decimal UnitSalePrice,
    decimal? UnitCost,
    int UnitsSold,
    decimal Revenue,
    decimal CostOfGoodsSold,
    decimal? Profit);

public sealed record OrderReportRow(
    int OrderId,
    string CustomerName,
    string Status,
    DateTime CreatedAt,
    int TotalUnits,
    decimal Revenue,
    decimal CostOfGoodsSold,
    decimal? Profit);

public sealed record ReportSummary(
    DateTime GeneratedAt,
    DateTime? From,
    DateTime? To,
    int ProductCount,
    int TotalStockOnHand,
    int UnitsSold,
    decimal Revenue,
    decimal CostOfGoodsSold,
    decimal? Profit,
    int ProductsMissingCost,
    string SalesStatusRule);

public sealed record ProductReportResponse(ReportSummary Summary, IReadOnlyList<ProductReportRow> Products);

public sealed record OrderReportResponse(ReportSummary Summary, IReadOnlyList<OrderReportRow> Orders);

public sealed record SavedReportResponse(int Id, string ReportType, DateTime GeneratedAt, object Report);
