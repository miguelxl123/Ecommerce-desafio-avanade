using Ecommerce.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Estoque.Data;

public class EstoqueDbContext : DbContext
{
    public EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurações para Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Description)
                .HasMaxLength(1000);
            
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            
            entity.Property(e => e.StockQuantity)
                .IsRequired();
            
            entity.Property(e => e.Category)
                .HasMaxLength(50);
            
            entity.Property(e => e.Sku)
                .HasMaxLength(20);
            
            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
            
            entity.Property(e => e.CreatedAt)
                .IsRequired();
        });

        // Índices para performance
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Name)
            .HasDatabaseName("IX_Products_Name");
        
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique()
            .HasDatabaseName("IX_Products_Sku");
        
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Category)
            .HasDatabaseName("IX_Products_Category");
        
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_Products_IsActive");

        // Seed data para desenvolvimento
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var products = new[]
        {
            new Product
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Smartphone Samsung Galaxy S23",
                Description = "Smartphone top de linha com 256GB de armazenamento",
                Price = 2499.99m,
                StockQuantity = 50,
                Category = "Eletrônicos",
                Sku = "SAMS23-256",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Notebook Lenovo ThinkPad",
                Description = "Notebook profissional com Intel i7 e 16GB RAM",
                Price = 4299.99m,
                StockQuantity = 25,
                Category = "Informática",
                Sku = "LEN-TP-I7",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Fone de Ouvido Sony WH-1000XM5",
                Description = "Fone com cancelamento de ruído ativo",
                Price = 1599.99m,
                StockQuantity = 100,
                Category = "Áudio",
                Sku = "SONY-WH1000",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Smart TV 55\" 4K Samsung",
                Description = "Smart TV com resolução 4K e HDR",
                Price = 3299.99m,
                StockQuantity = 15,
                Category = "TV e Home Theater",
                Sku = "SAMS-TV55-4K",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Name = "Console PlayStation 5",
                Description = "Console de videogame de última geração",
                Price = 4499.99m,
                StockQuantity = 8,
                Category = "Games",
                Sku = "SONY-PS5",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        modelBuilder.Entity<Product>().HasData(products);
    }
}