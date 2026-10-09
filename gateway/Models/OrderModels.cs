using System.ComponentModel.DataAnnotations;

namespace gateway.Models;

/// <summary>Request body for creating an order.</summary>
public class CreateOrderRequestDto
{
    /// <summary>The products to order. Prices are taken from the Product Service.</summary>
    [Required]
    [MinLength(1, ErrorMessage = "An order needs at least one item.")]
    public List<OrderItemRequestDto> Items { get; set; } = new();

    /// <summary>Where the order is shipped.</summary>
    [Required]
    [MaxLength(500)]
    public string ShippingAddress { get; set; } = null!;
}

/// <summary>One line of an order being created.</summary>
public class OrderItemRequestDto
{
    /// <summary>ID of the product.</summary>
    [Required]
    public string ProductId { get; set; } = null!;

    /// <summary>ID of the variant. Required when the product has variants.</summary>
    public string? VariantId { get; set; }

    /// <summary>Number of units.</summary>
    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;
}

/// <summary>Details of an order.</summary>
public class OrderDto
{
    /// <summary>Unique order ID. Pass it to the payment endpoint to pay the order.</summary>
    public string Id { get; set; } = null!;
    /// <summary>ID of the user who owns the order.</summary>
    public string UserId { get; set; } = null!;
    /// <summary>"Pending", "Paid" or "Cancelled".</summary>
    public string Status { get; set; } = null!;
    /// <summary>Sum of all line totals.</summary>
    public decimal Total { get; set; }
    /// <summary>Where the order is shipped.</summary>
    public string ShippingAddress { get; set; } = null!;
    /// <summary>ID of the payment that paid the order, if any.</summary>
    public string? PaymentId { get; set; }
    /// <summary>Creation timestamp (ISO 8601 string).</summary>
    public string CreatedAt { get; set; } = null!;
    /// <summary>The order lines.</summary>
    public List<OrderItemDto> Items { get; set; } = new();
}

/// <summary>One line of an order.</summary>
public class OrderItemDto
{
    /// <summary>ID of the product.</summary>
    public string ProductId { get; set; } = null!;
    /// <summary>ID of the variant, if any.</summary>
    public string? VariantId { get; set; }
    /// <summary>Product name when the order was created.</summary>
    public string ProductName { get; set; } = null!;
    /// <summary>Variant name when the order was created, if any.</summary>
    public string? VariantName { get; set; }
    /// <summary>Price of one unit when the order was created.</summary>
    public decimal UnitPrice { get; set; }
    /// <summary>Number of units.</summary>
    public int Quantity { get; set; }
    /// <summary>UnitPrice multiplied by Quantity.</summary>
    public decimal LineTotal { get; set; }
}

/// <summary>A page of orders.</summary>
public class OrderListDto
{
    /// <summary>The orders on this page, newest first.</summary>
    public List<OrderDto> Orders { get; set; } = new();
    /// <summary>Total number of orders of the user.</summary>
    public long TotalCount { get; set; }
}
