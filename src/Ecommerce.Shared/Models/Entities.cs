using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Shared.Models;

/// <summary>
/// Classe base para todas as entidades do domínio
/// Fornece propriedades comuns de auditoria
/// </summary>
public class BaseEntity
{
    /// <summary>
    /// Identificador único da entidade
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Data e hora de criação da entidade
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Data e hora da última atualização da entidade
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Entidade que representa um produto no catálogo da loja
/// </summary>
public class Product : BaseEntity
{
    /// <summary>
    /// Nome do produto (obrigatório, até 200 caracteres)
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Descrição detalhada do produto (opcional, até 1000 caracteres)
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Preço unitário do produto (deve ser maior que zero)
    /// </summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
    
    /// <summary>
    /// Quantidade atual em estoque (não pode ser negativa)
    /// </summary>
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
    public int StockQuantity { get; set; }
    
    /// <summary>
    /// Categoria do produto para organização (opcional, até 50 caracteres)
    /// </summary>
    [StringLength(50)]
    public string? Category { get; set; }
    
    [StringLength(20)]
    public string? Sku { get; set; }
    
    public bool IsActive { get; set; } = true;
}

public class Order : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string UserEmail { get; set; } = string.Empty;
    
    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalAmount { get; set; }
    
    public List<OrderItem> Items { get; set; } = new();
    
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
}

public class OrderItem : BaseEntity
{
    [Required]
    public Guid OrderId { get; set; }
    
    [Required]
    public Guid ProductId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal UnitPrice { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal TotalPrice { get; set; }
}

public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Processing = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6
}