namespace Ecommerce.Shared.Events;

public abstract class BaseEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
}

public class VendaCriadaEvent : BaseEvent
{
    public VendaCriadaEvent()
    {
        EventType = nameof(VendaCriadaEvent);
    }
    
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<VendaItemEvent> Items { get; set; } = new();
}

public class VendaItemEvent
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class EstoqueBaixaEvent : BaseEvent
{
    public EstoqueBaixaEvent()
    {
        EventType = nameof(EstoqueBaixaEvent);
    }
    
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = "Venda realizada";
}