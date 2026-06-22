using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using payment_service.Services;
using Shared.Protos.Payment;

namespace payment_service.GrpcServices
{
    /// <summary>
    /// gRPC Service for Payment processing and billing invoice retrieval.
    /// </summary>
    public class PaymentGrpcService : Shared.Protos.Payment.PaymentService.PaymentServiceBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<PaymentGrpcService> _logger;

        public PaymentGrpcService(
            IPaymentService paymentService,
            IInvoiceService invoiceService,
            ILogger<PaymentGrpcService> logger)
        {
            _paymentService = paymentService;
            _invoiceService = invoiceService;
            _logger = logger;
        }

        public override async Task<ProcessPaymentResponse> ProcessPayment(ProcessPaymentRequest request, ServerCallContext context)
        {
            try
            {
                var record = await _paymentService.ProcessPaymentAsync(
                    request.OrderId,
                    (decimal)request.Amount,
                    request.PaymentMethod,
                    request.PaymentMethodId,
                    request.CustomerEmail,
                    request.CustomerName,
                    request.BillingAddress
                );

                return new ProcessPaymentResponse
                {
                    Success = record.Status == "Completed",
                    Message = record.Status == "Completed" ? "Payment processed successfully." : $"Payment status: {record.Status}",
                    PaymentId = record.PaymentId,
                    Status = record.Status,
                    StripePaymentIntentId = record.StripePaymentIntentId ?? string.Empty
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in ProcessPayment: {Message}", ex.Message);
                throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment via gRPC for Order {OrderId}", request.OrderId);
                throw new RpcException(new Status(StatusCode.Internal, $"Payment processing failed: {ex.Message}"));
            }
        }

        public override async Task<GetPaymentDetailsResponse> GetPaymentDetails(GetPaymentDetailsRequest request, ServerCallContext context)
        {
            try
            {
                var record = await _paymentService.GetPaymentDetailsAsync(request.PaymentId);
                if (record == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, $"Payment record with ID {request.PaymentId} was not found."));
                }

                return new GetPaymentDetailsResponse
                {
                    PaymentId = record.PaymentId,
                    OrderId = record.OrderId,
                    Amount = (double)record.Amount,
                    PaymentMethod = record.PaymentMethod,
                    Status = record.Status,
                    StripePaymentIntentId = record.StripePaymentIntentId ?? string.Empty,
                    CreatedAt = record.CreatedAt.ToString("o")
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment details via gRPC for PaymentId {PaymentId}", request.PaymentId);
                throw new RpcException(new Status(StatusCode.Internal, $"Retrieving payment details failed: {ex.Message}"));
            }
        }

        public override async Task<GetInvoiceResponse> GetInvoice(GetInvoiceRequest request, ServerCallContext context)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByPaymentIdAsync(request.PaymentId);
                if (invoice == null)
                {
                    throw new RpcException(new Status(StatusCode.NotFound, $"Invoice for Payment ID {request.PaymentId} was not found."));
                }

                return new GetInvoiceResponse
                {
                    InvoiceId = invoice.InvoiceId,
                    PaymentId = invoice.PaymentRecordId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    CustomerEmail = invoice.CustomerEmail,
                    CustomerName = invoice.CustomerName,
                    BillingAddress = invoice.BillingAddress,
                    Amount = (double)invoice.Amount,
                    InvoiceBody = invoice.InvoiceBody,
                    CreatedAt = invoice.CreatedAt.ToString("o")
                };
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice via gRPC for PaymentId {PaymentId}", request.PaymentId);
                throw new RpcException(new Status(StatusCode.Internal, $"Retrieving invoice failed: {ex.Message}"));
            }
        }
    }
}
