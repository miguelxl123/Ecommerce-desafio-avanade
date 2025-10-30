using Ecommerce.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Vendas.Data;

public class VendasDbContext : DbContext
{
    public VendasDbContext(DbContextOptions<VendasDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurações para Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.UserEmail)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            entity.Property(e => e.Status)
                .HasConversion<int>()
                .IsRequired();
            
            entity.Property(e => e.Notes)
                .HasMaxLength(500);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
            
            // Configurar relacionamento com OrderItems
            entity.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações para OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Quantity)
                .IsRequired();
            
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            entity.Property(e => e.TotalPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
        });

        // Índices para performance
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.UserId)
            .HasDatabaseName("IX_Orders_UserId");
        
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.Status)
            .HasDatabaseName("IX_Orders_Status");
        
        modelBuilder.Entity<Order>()
            .HasIndex(o => o.CreatedAt)
            .HasDatabaseName("IX_Orders_CreatedAt");
        
        modelBuilder.Entity<OrderItem>()
            .HasIndex(oi => oi.ProductId)
            .HasDatabaseName("IX_OrderItems_ProductId");
    }
}