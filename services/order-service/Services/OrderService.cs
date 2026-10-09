using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using order_service.db;
using order_service.Model;
using Shared.Protos.Product;

namespace order_service.Services;

/// <summary>
/// Service implementation for order creation and retrieval.
/// </summary>
public class OrderService : IOrderService
{
    private const int MaxQuantityPerItem = 1000;

    private readonly OrderDbContext _dbContext;
    private readonly ProductService.ProductServiceClient _productClient;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderDbContext dbContext,
        ProductService.ProductServiceClient productClient,
        ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _productClient = productClient;
        _logger = logger;
    }

    public async Task<Order> CreateAsync(string userId, IReadOnlyList<OrderItemInput> items, string shippingAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.");
        if (string.IsNullOrWhiteSpace(shippingAddress))
            throw new ArgumentException("Shipping address is required.");
        if (items.Count == 0)
            throw new ArgumentException("An order needs at least one item.");

        var order = new Order
        {
            UserId = userId,
            ShippingAddress = shippingAddress.Trim()
        };

        foreach (var item in items)
        {
            if (item.Quantity < 1 || item.Quantity > MaxQuantityPerItem)
                throw new ArgumentException($"Quantity must be between 1 and {MaxQuantityPerItem}.");

            var product = await GetProductAsync(item.ProductId, cancellationToken);
            if (!product.IsActive)
                throw new ArgumentException($"Product '{product.Name}' is not available.");

            // The price always comes from the Product Service, never from the caller
            decimal unitPrice;
            string? variantName = null;

            if (item.VariantId is { } variantId)
            {
                var variant = product.Variants.FirstOrDefault(v => v.Id == variantId.ToString())
                    ?? throw new ArgumentException($"Variant {variantId} does not belong to product '{product.Name}'.");

                if (variant.Stock < item.Quantity)
                    throw new ArgumentException($"Not enough stock for '{product.Name} - {variant.Name}'.");

                unitPrice = ToMoney(variant.Price);
                variantName = variant.Name;
            }
            else
            {
                if (product.Variants.Count > 0)
                    throw new ArgumentException($"Product '{product.Name}' has variants; a variantId is required.");

                unitPrice = ToMoney(product.BasePrice);
            }

            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                ProductName = product.Name,
                VariantName = variantName,
                UnitPrice = unitPrice,
                Quantity = item.Quantity,
                LineTotal = unitPrice * item.Quantity
            });
        }

        order.Total = order.Items.Sum(i => i.LineTotal);

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created order {OrderId} for user {UserId} with total {Total}", order.Id, userId, order.Total);
        return order;
    }

    public async Task<Order?> GetAsync(Guid orderId)
    {
        return await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<(List<Order> Orders, long TotalCount)> ListByUserAsync(string userId, int page, int pageSize)
    {
        var query = _dbContext.Orders.Where(o => o.UserId == userId);

        var totalCount = await query.LongCountAsync();

        var orders = await query
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (orders, totalCount);
    }

    public async Task<Order> MarkPaidAsync(Guid orderId, string paymentId)
    {
        var order = await GetAsync(orderId)
            ?? throw new KeyNotFoundException($"Order {orderId} was not found.");

        // Repeating the same call is harmless
        if (order.Status == OrderStatus.Paid && order.PaymentId == paymentId)
            return order;

        if (order.Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Order {orderId} is {order.Status} and cannot be marked as paid.");

        order.Status = OrderStatus.Paid;
        order.PaymentId = paymentId;
        order.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} marked as paid by payment {PaymentId}", orderId, paymentId);
        return order;
    }

    private async Task<ProductDetailResponse> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        try
        {
            return await _productClient.GetProductAsync(
                new GetProductRequest { Id = productId.ToString() },
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new ArgumentException($"Product {productId} was not found.");
        }
    }

    private static decimal ToMoney(double value) => Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
}
