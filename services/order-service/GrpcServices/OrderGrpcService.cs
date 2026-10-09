using Grpc.Core;
using order_service.Model;
using order_service.Services;
using Shared.Protos.Order;

namespace order_service.GrpcServices;

/// <summary>
/// gRPC Service for creating orders, reading them, and marking them as paid.
/// </summary>
public class OrderGrpcService : Shared.Protos.Order.OrderService.OrderServiceBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderGrpcService> _logger;

    public OrderGrpcService(IOrderService orderService, ILogger<OrderGrpcService> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public override async Task<OrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        var items = new List<OrderItemInput>();
        foreach (var item in request.Items)
        {
            if (!Guid.TryParse(item.ProductId, out var productId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"ProductId '{item.ProductId}' is not a valid GUID."));

            Guid? variantId = null;
            if (!string.IsNullOrEmpty(item.VariantId))
            {
                if (!Guid.TryParse(item.VariantId, out var parsedVariantId))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, $"VariantId '{item.VariantId}' is not a valid GUID."));
                variantId = parsedVariantId;
            }

            items.Add(new OrderItemInput(productId, variantId, item.Quantity));
        }

        try
        {
            var order = await _orderService.CreateAsync(request.UserId, items, request.ShippingAddress, context.CancellationToken);
            return ToResponse(order);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid CreateOrder request: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order via gRPC for user {UserId}", request.UserId);
            throw new RpcException(new Status(StatusCode.Internal, "Creating the order failed."));
        }
    }

    public override async Task<OrderResponse> GetOrder(GetOrderRequest request, ServerCallContext context)
    {
        var order = await _orderService.GetAsync(ParseOrderId(request.OrderId))
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Order {request.OrderId} was not found."));

        return ToResponse(order);
    }

    public override async Task<ListOrdersResponse> ListOrders(ListOrdersRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "UserId is required."));

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 10;

        var (orders, totalCount) = await _orderService.ListByUserAsync(request.UserId, page, pageSize);

        var response = new ListOrdersResponse { TotalCount = totalCount };
        response.Orders.AddRange(orders.Select(ToResponse));
        return response;
    }

    public override async Task<OrderResponse> MarkOrderPaid(MarkOrderPaidRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "PaymentId is required."));

        try
        {
            var order = await _orderService.MarkPaidAsync(ParseOrderId(request.OrderId), request.PaymentId);
            return ToResponse(order);
        }
        catch (KeyNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    private static Guid ParseOrderId(string orderId)
    {
        // An ID that is not a GUID cannot belong to any order
        if (!Guid.TryParse(orderId, out var id))
            throw new RpcException(new Status(StatusCode.NotFound, $"Order {orderId} was not found."));
        return id;
    }

    private static OrderResponse ToResponse(Order order)
    {
        var response = new OrderResponse
        {
            Id = order.Id.ToString(),
            UserId = order.UserId,
            Status = order.Status,
            Total = (double)order.Total,
            ShippingAddress = order.ShippingAddress,
            PaymentId = order.PaymentId ?? string.Empty,
            CreatedAt = order.CreatedAt.ToString("o")
        };

        response.Items.AddRange(order.Items.Select(i => new OrderItemMessage
        {
            ProductId = i.ProductId.ToString(),
            VariantId = i.VariantId?.ToString() ?? string.Empty,
            ProductName = i.ProductName,
            VariantName = i.VariantName ?? string.Empty,
            UnitPrice = (double)i.UnitPrice,
            Quantity = i.Quantity,
            LineTotal = (double)i.LineTotal
        }));

        return response;
    }
}
