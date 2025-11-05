using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Shared.Models;

public class CreateOrderRequest
{
    [Required]
    public List<OrderItemRequest> Items { get; set; } = new();
    
    [StringLength(500)]
    public string? Notes { get; set; }
}

public class OrderItemRequest
{
    [Required]
    public Guid ProductId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}

public class CreateProductRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
    
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
    public int StockQuantity { get; set; }
    
    [StringLength(50)]
    public string? Category { get; set; }
    
    [StringLength(20)]
    public string? Sku { get; set; }
}

public class UpdateProductRequest
{
    [StringLength(200)]
    public string? Name { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal? Price { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
    public int? StockQuantity { get; set; }
    
    [StringLength(50)]
    public string? Category { get; set; }
    
    [StringLength(20)]
    public string? Sku { get; set; }
    
    public bool? IsActive { get; set; }
}

public class ProductAvailabilityRequest
{
    [Required]
    public Guid ProductId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class ProductAvailabilityResponse
{
    public Guid ProductId { get; set; }
    public bool IsAvailable { get; set; }
    public int AvailableQuantity { get; set; }
    public string? Message { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    
    public static ApiResponse<T> SuccessResult(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }
    
    public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}
