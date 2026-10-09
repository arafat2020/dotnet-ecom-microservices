namespace order_service.Model;

/// <summary>
/// An order placed by a user. Prices are copied from the Product Service when the order is created.
/// </summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>ID of the user (from the Auth Service) who owns the order.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>One of the <see cref="OrderStatus"/> values.</summary>
    public string Status { get; set; } = OrderStatus.Pending;

    /// <summary>Sum of all line totals.</summary>
    public decimal Total { get; set; }

    public string ShippingAddress { get; set; } = string.Empty;

    /// <summary>ID of the payment that paid this order, set by the Payment Service.</summary>
    public string? PaymentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}

public static class OrderStatus
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";
}
