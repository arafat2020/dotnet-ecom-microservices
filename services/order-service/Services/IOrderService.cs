using order_service.Model;

namespace order_service.Services;

/// <summary>One requested line of a new order.</summary>
public record OrderItemInput(Guid ProductId, Guid? VariantId, int Quantity);

/// <summary>
/// Service for creating and reading orders.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Creates a Pending order. Names and prices are read from the Product Service.
    /// </summary>
    /// <exception cref="ArgumentException">The request is invalid (unknown product, bad quantity, no stock...).</exception>
    Task<Order> CreateAsync(string userId, IReadOnlyList<OrderItemInput> items, string shippingAddress, CancellationToken cancellationToken = default);

    Task<Order?> GetAsync(Guid orderId);

    Task<(List<Order> Orders, long TotalCount)> ListByUserAsync(string userId, int page, int pageSize);

    /// <summary>
    /// Marks a Pending order as Paid.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The order does not exist.</exception>
    /// <exception cref="InvalidOperationException">The order is not Pending.</exception>
    Task<Order> MarkPaidAsync(Guid orderId, string paymentId);
}
