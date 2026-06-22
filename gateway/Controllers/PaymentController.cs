using gateway.Models;
using Grpc.Core;
using GrpcStatusCode = Grpc.Core.StatusCode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Protos.Payment;

namespace gateway.Controllers
{
    /// <summary>
    /// Controller for payment operations and billing records management.
    /// Acts as a REST-to-gRPC proxy for the Payment Service.
    /// </summary>
    [Route("api/payments")]
    public class PaymentController : ApiControllerBase
    {
        private readonly PaymentService.PaymentServiceClient _paymentServiceClient;
        private readonly ILogger<PaymentController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentController"/> class.
        /// </summary>
        /// <param name="paymentServiceClient">The gRPC client for the Payment Service.</param>
        /// <param name="logger">The logger instance.</param>
        public PaymentController(PaymentService.PaymentServiceClient paymentServiceClient, ILogger<PaymentController> logger)
        {
            _paymentServiceClient = paymentServiceClient;
            _logger = logger;
        }

        /// <summary>
        /// Processes a payment transaction (via Stripe or Cash In) and creates a billing record.
        /// Automatically generates an invoice on successful processing.
        /// </summary>
        /// <param name="request">The payment processing parameters.</param>
        /// <returns>A response indicating payment outcome and ID.</returns>
        /// <response code="200">Payment successfully processed.</response>
        /// <response code="400">Request validation error or unsupported parameters.</response>
        /// <response code="500">Internal communication error with the Payment Service.</response>
        [HttpPost("process")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<ProcessPaymentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Process([FromBody] ProcessPaymentRequestDto request)
        {
            try
            {
                if (request.PaymentMethod.ToLowerInvariant() == "stripe" && string.IsNullOrEmpty(request.PaymentMethodId))
                {
                    return ApiBadRequest("PaymentMethodId (Stripe Token/PaymentMethod) is required when PaymentMethod is 'stripe'.");
                }

                var grpcRequest = new ProcessPaymentRequest
                {
                    OrderId = request.OrderId,
                    Amount = (double)request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    PaymentMethodId = request.PaymentMethodId ?? string.Empty,
                    CustomerEmail = request.CustomerEmail,
                    CustomerName = request.CustomerName,
                    BillingAddress = request.BillingAddress
                };

                var response = await _paymentServiceClient.ProcessPaymentAsync(grpcRequest);

                var resultDto = new ProcessPaymentResponseDto
                {
                    Success = response.Success,
                    Message = response.Message,
                    PaymentId = response.PaymentId,
                    Status = response.Status,
                    StripePaymentIntentId = response.StripePaymentIntentId
                };

                if (response.Success)
                {
                    return ApiOk(resultDto, "Payment processed and invoice generated successfully.");
                }
                else
                {
                    return ApiOk(resultDto, $"Payment processing failed with status: {response.Status}");
                }
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.InvalidArgument)
            {
                _logger.LogWarning(ex, "gRPC invalid argument error in payment process.");
                return ApiBadRequest(ex.Status.Detail);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC communication error during payment processing.");
                return ApiInternalError($"Payment service unavailable: {ex.Status.Detail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in payment process endpoint.");
                return ApiInternalError("An unexpected error occurred while processing the payment.");
            }
        }

        /// <summary>
        /// Retrieves the billing details and record for a specific payment ID.
        /// </summary>
        /// <param name="id">The unique payment ID.</param>
        /// <returns>A response enclosing the billing record details.</returns>
        /// <response code="200">Payment record successfully retrieved.</response>
        /// <response code="404">Payment record not found.</response>
        /// <response code="500">Internal communication error.</response>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<PaymentRecordDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDetails([FromRoute] string id)
        {
            try
            {
                var grpcRequest = new GetPaymentDetailsRequest { PaymentId = id };
                var response = await _paymentServiceClient.GetPaymentDetailsAsync(grpcRequest);

                var dto = new PaymentRecordDto
                {
                    PaymentId = response.PaymentId,
                    OrderId = response.OrderId,
                    Amount = (decimal)response.Amount,
                    PaymentMethod = response.PaymentMethod,
                    Status = response.Status,
                    StripePaymentIntentId = response.StripePaymentIntentId,
                    CreatedAt = response.CreatedAt
                };

                return ApiOk(dto, "Payment billing record retrieved successfully.");
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.NotFound)
            {
                _logger.LogWarning("Payment record {Id} not found.", id);
                return ApiNotFound($"Payment record with ID '{id}' was not found.");
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC communication error retrieving payment details.");
                return ApiInternalError($"Payment service unavailable: {ex.Status.Detail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in payment details retrieval endpoint.");
                return ApiInternalError("An unexpected error occurred while retrieving payment details.");
            }
        }

        /// <summary>
        /// Retrieves the automatically generated invoice details and HTML body for a specific payment ID.
        /// </summary>
        /// <param name="id">The unique payment ID.</param>
        /// <returns>A response enclosing the invoice details.</returns>
        /// <response code="200">Invoice successfully retrieved.</response>
        /// <response code="404">Invoice not found for the given payment.</response>
        /// <response code="500">Internal communication error.</response>
        [HttpGet("{id}/invoice")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<InvoiceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetInvoice([FromRoute] string id)
        {
            try
            {
                var grpcRequest = new GetInvoiceRequest { PaymentId = id };
                var response = await _paymentServiceClient.GetInvoiceAsync(grpcRequest);

                var dto = new InvoiceDto
                {
                    InvoiceId = response.InvoiceId,
                    PaymentId = response.PaymentId,
                    InvoiceNumber = response.InvoiceNumber,
                    CustomerEmail = response.CustomerEmail,
                    CustomerName = response.CustomerName,
                    BillingAddress = response.BillingAddress,
                    Amount = (decimal)response.Amount,
                    InvoiceBody = response.InvoiceBody,
                    CreatedAt = response.CreatedAt
                };

                return ApiOk(dto, "Invoice details retrieved successfully.");
            }
            catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.NotFound)
            {
                _logger.LogWarning("Invoice for Payment ID {Id} not found.", id);
                return ApiNotFound($"Invoice for payment ID '{id}' was not found.");
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC communication error retrieving invoice.");
                return ApiInternalError($"Payment service unavailable: {ex.Status.Detail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in invoice retrieval endpoint.");
                return ApiInternalError("An unexpected error occurred while retrieving the invoice.");
            }
        }
    }
}
