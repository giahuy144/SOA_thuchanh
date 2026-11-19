using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(order => order.CustomerName).HasColumnName("customer_name").HasMaxLength(255).IsRequired();
            entity.Property(order => order.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            entity.Property(order => order.TotalAmount).HasColumnName("total_amount").HasColumnType("decimal(12,2)");
            entity.Property(order => order.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(order => order.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.HasMany(order => order.Items).WithOne(item => item.Order).HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(item => item.OrderId).HasColumnName("order_id");
            entity.Property(item => item.ProductId).HasColumnName("product_id");
            entity.Property(item => item.ProductName).HasColumnName("product_name").HasMaxLength(255).IsRequired();
            entity.Property(item => item.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(10,2)");
            entity.Property(item => item.Quantity).HasColumnName("quantity");
            entity.Property(item => item.LineTotal).HasColumnName("line_total").HasColumnType("decimal(12,2)");
        });
    }
}
