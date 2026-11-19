using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReportService.Models;

[Table("product_costs")]
public sealed class ProductCost
{
    [Key, Column("product_id")]
    public int ProductId { get; set; }

    [Column("unit_cost", TypeName = "decimal(10,2)")]
    public decimal UnitCost { get; set; }

    [Required, MaxLength(255), Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

[Table("report_snapshots")]
public sealed class ReportSnapshot
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Required, MaxLength(50), Column("report_type")]
    public string ReportType { get; set; } = string.Empty;

    [Column("generated_at")]
    public DateTime GeneratedAt { get; set; }

    [Required, Column("payload_json")]
    public string PayloadJson { get; set; } = string.Empty;
}
