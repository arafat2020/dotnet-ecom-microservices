using gateway.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Protos.Order;

namespace gateway.Controllers;

/// <summary>
/// Controller for Order operations.
/// Acts as a REST-to-gRPC proxy for the Order Service.
/// All endpoints require an authenticated user; users only see their own orders.
/// </summary>
[Route("api/orders")]
[Authorize]
public class OrderController : ApiControllerBase
{
    private readonly OrderService.OrderServiceClient _orderServiceClient;
    private readonly ILogger<OrderController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderController"/> class.
    /// </summary>
    /// <param name="orderServiceClient">The gRPC client for the Order Service.</param>
    /// <param name="logger">The logger instance.</param>
    public OrderController(OrderService.OrderServiceClient orderServiceClient, ILogger<OrderController> logger)
    {
        _orderServiceClient = orderServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a Pending order for the authenticated user. Prices are read from the Product Service.
    /// </summary>
    /// <param name="request">The items and shipping address.</param>
    /// <response code="201">Returns the created order, including its total.</response>
    /// <response code="400">Unknown product or variant, bad quantity, or not enough stock.</response>
    /// <response code="401">No valid token was provided.</response>
    /// <response code="500">Order Service is unavailable.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequestDto request)
    {
        try
        {
            var grpcRequest = new CreateOrderRequest
            {
                UserId = CurrentUserId,
                ShippingAddress = request.ShippingAddress
            };

            grpcRequest.Items.AddRange(request.Items.Select(i => new OrderItemRequest
            {
                ProductId = i.ProductId,
                VariantId = i.VariantId ?? string.Empty,
                Quantity = i.Quantity
            }));

            var response = await _orderServiceClient.CreateOrderAsync(grpcRequest);

            return ApiCreated(ToDto(response), "Order created successfully.");
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC error creating order: {Status}", ex.StatusCode);
            return ApiRpcError(ex, "Order Service");
        }
    }

    /// <summary>
    /// Lists the orders of the authenticated user, newest first.
    /// </summary>
    /// <response code="200">Returns a page of orders.</response>
    /// <response code="401">No valid token was provided.</response>
    /// <response code="500">Order Service is unavailable.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<OrderListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var response = await _orderServiceClient.ListOrdersAsync(new ListOrdersRequest
            {
                UserId = CurrentUserId,
                Page = page,
                PageSize = pageSize
            });

            return ApiOk(new OrderListDto
            {
                Orders = response.Orders.Select(ToDto).ToList(),
                TotalCount = response.TotalCount
            }, "Orders retrieved successfully.");
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC error listing orders: {Status}", ex.StatusCode);
            return ApiRpcError(ex, "Order Service");
        }
    }

    /// <summary>
    /// Retrieves one order. Users can read their own orders; Admins can read any order.
    /// </summary>
    /// <param name="id">The unique order ID.</param>
    /// <response code="200">Returns the order.</response>
    /// <response code="401">No valid token was provided.</response>
    /// <response code="403">The order belongs to another user.</response>
    /// <response code="404">Order not found.</response>
    /// <response code="500">Order Service is unavailable.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        try
        {
            var response = await _orderServiceClient.GetOrderAsync(new GetOrderRequest { OrderId = id.ToString() });

            if (!IsAdmin && response.UserId != CurrentUserId)
            {
                return ApiForbidden("You do not have access to this order.");
            }

            return ApiOk(ToDto(response), "Order retrieved successfully.");
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC error retrieving order {Id}: {Status}", id, ex.StatusCode);
            return ApiRpcError(ex, "Order Service");
        }
    }

    private static OrderDto ToDto(OrderResponse order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        Status = order.Status,
        Total = (decimal)order.Total,
        ShippingAddress = order.ShippingAddress,
        PaymentId = string.IsNullOrEmpty(order.PaymentId) ? null : order.PaymentId,
        CreatedAt = order.CreatedAt,
        Items = order.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            VariantId = string.IsNullOrEmpty(i.VariantId) ? null : i.VariantId,
            ProductName = i.ProductName,
            VariantName = string.IsNullOrEmpty(i.VariantName) ? null : i.VariantName,
            UnitPrice = (decimal)i.UnitPrice,
            Quantity = i.Quantity,
            LineTotal = (decimal)i.LineTotal
        }).ToList()
    };
}
