using Microsoft.EntityFrameworkCore;
using ReportService.Models;

namespace ReportService.Data;

public sealed class ReportDbContext(DbContextOptions<ReportDbContext> options) : DbContext(options)
{
    public DbSet<ProductCost> ProductCosts => Set<ProductCost>();
    public DbSet<ReportSnapshot> ReportSnapshots => Set<ReportSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductCost>(entity =>
        {
            entity.ToTable("product_costs");
            entity.HasKey(cost => cost.ProductId);
            entity.Property(cost => cost.ProductId).HasColumnName("product_id");
            entity.Property(cost => cost.UnitCost).HasColumnName("unit_cost").HasColumnType("decimal(10,2)");
            entity.Property(cost => cost.ProductName).HasColumnName("product_name").HasMaxLength(255).IsRequired();
            entity.Property(cost => cost.UpdatedAt).HasColumnName("updated_at").IsRequired();
        });

        modelBuilder.Entity<ReportSnapshot>(entity =>
        {
            entity.ToTable("report_snapshots");
            entity.HasKey(snapshot => snapshot.Id);
            entity.Property(snapshot => snapshot.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(snapshot => snapshot.ReportType).HasColumnName("report_type").HasMaxLength(50).IsRequired();
            entity.Property(snapshot => snapshot.GeneratedAt).HasColumnName("generated_at").IsRequired();
            entity.Property(snapshot => snapshot.PayloadJson).HasColumnName("payload_json").IsRequired();
        });
    }
}
