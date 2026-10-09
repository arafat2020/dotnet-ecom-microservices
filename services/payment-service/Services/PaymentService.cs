using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using payment_service.db;
using Grpc.Core;
using payment_service.Model;
using Shared.Protos.Order;
using Stripe;

namespace payment_service.Services
{
    /// <summary>
    /// Service implementation for payment processing.
    /// Supports Stripe (actual/mock) and Cash In methods.
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly PaymentDbContext _dbContext;
        private readonly IInvoiceService _invoiceService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly OrderService.OrderServiceClient _orderClient;

        public PaymentService(
            PaymentDbContext dbContext,
            IInvoiceService invoiceService,
            IConfiguration configuration,
            ILogger<PaymentService> logger,
            OrderService.OrderServiceClient orderClient)
        {
            _orderClient = orderClient;
            _dbContext = dbContext;
            _invoiceService = invoiceService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PaymentRecord> ProcessPaymentAsync(
            string orderId, 
            string userId, 
            string paymentMethod, 
            string paymentMethodId, 
            string customerEmail, 
            string customerName, 
            string billingAddress)
        {
            var method = paymentMethod.ToLowerInvariant();
            if (method != "cash_in" && method != "stripe")
            {
                throw new ArgumentException($"Unsupported payment method: {paymentMethod}");
            }

            // The amount comes from the order, never from the caller
            var order = await GetOrderAsync(orderId);

            if (order.UserId != userId)
            {
                throw new UnauthorizedAccessException("This order belongs to another user.");
            }

            if (order.Status != "Pending")
            {
                throw new InvalidOperationException($"Order {orderId} is {order.Status} and cannot be paid.");
            }

            bool alreadyPaid = await _dbContext.PaymentRecords
                .AnyAsync(p => p.OrderId == orderId && p.Status == "Completed");
            if (alreadyPaid)
            {
                throw new InvalidOperationException($"Order {orderId} has already been paid.");
            }

            decimal amount = (decimal)order.Total;

            _logger.LogInformation("Processing {Method} payment of {Amount} for Order {OrderId}", method, amount, orderId);

            var paymentRecord = new PaymentRecord
            {
                PaymentId = Guid.NewGuid().ToString(),
                OrderId = orderId,
                UserId = userId,
                Amount = amount,
                PaymentMethod = method,
                CreatedAt = DateTime.UtcNow
            };

            if (method == "cash_in")
            {
                // Cash in (Cash on delivery / Cash payment) is completed immediately for billing records
                paymentRecord.Status = "Completed";
            }
            else
            {
                bool useMock = _configuration.GetValue<bool>("Stripe:UseMock", true);
                string? secretKey = _configuration["Stripe:SecretKey"];

                if (useMock || string.IsNullOrEmpty(secretKey))
                {
                    _logger.LogWarning("Using Mock Stripe Processing because UseMock is true or SecretKey is empty.");
                    paymentRecord.Status = "Completed";
                    paymentRecord.StripePaymentIntentId = $"pi_mock_{Guid.NewGuid().ToString()[..12]}";
                }
                else
                {
                    await ChargeWithStripeAsync(paymentRecord, secretKey, paymentMethodId, customerEmail);
                }
            }

            _dbContext.PaymentRecords.Add(paymentRecord);
            await _dbContext.SaveChangesAsync();

            if (paymentRecord.Status != "Completed")
            {
                return paymentRecord;
            }

            // The money has been taken at this point: a failure below must not change the payment status
            try
            {
                await _invoiceService.GenerateInvoiceAsync(paymentRecord, customerEmail, customerName, billingAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment {PaymentId} completed but the invoice could not be generated.", paymentRecord.PaymentId);
            }

            try
            {
                await _orderClient.MarkOrderPaidAsync(new MarkOrderPaidRequest
                {
                    OrderId = orderId,
                    PaymentId = paymentRecord.PaymentId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment {PaymentId} completed but Order {OrderId} could not be marked as paid.", paymentRecord.PaymentId, orderId);
            }

            return paymentRecord;
        }

        private async Task<OrderResponse> GetOrderAsync(string orderId)
        {
            try
            {
                return await _orderClient.GetOrderAsync(new GetOrderRequest { OrderId = orderId });
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                throw new KeyNotFoundException($"Order {orderId} was not found.");
            }
        }

        /// <summary>
        /// Charges the order through Stripe and sets the status of the record to "Completed" or "Failed".
        /// </summary>
        private async Task ChargeWithStripeAsync(PaymentRecord paymentRecord, string secretKey, string paymentMethodId, string customerEmail)
        {
            StripeConfiguration.ApiKey = secretKey;
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)Math.Round(paymentRecord.Amount * 100), // Stripe expects amounts in cents
                Currency = "usd",
                PaymentMethod = paymentMethodId,
                Confirm = true,
                // Automatic payment methods required for API version stability
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never" // Prevents requiring 3D secure redirects in a backend-to-backend flow
                },
                Description = $"Payment for Order {paymentRecord.OrderId}",
                ReceiptEmail = customerEmail
            };

            try
            {
                var service = new PaymentIntentService();
                PaymentIntent intent = await service.CreateAsync(options);

                paymentRecord.StripePaymentIntentId = intent.Id;
                paymentRecord.Status = intent.Status == "succeeded" ? "Completed" : "Failed";

                if (paymentRecord.Status == "Failed")
                {
                    _logger.LogWarning("Stripe payment for Order {OrderId} did not succeed. Status: {Status}", paymentRecord.OrderId, intent.Status);
                }
            }
            catch (StripeException ex)
            {
                // Declined card, invalid payment method, etc.
                _logger.LogWarning(ex, "Stripe rejected the payment for Order {OrderId}", paymentRecord.OrderId);
                paymentRecord.StripePaymentIntentId = ex.StripeError?.PaymentIntent?.Id;
                paymentRecord.Status = "Failed";
            }
        }

        public async Task<PaymentRecord?> GetPaymentDetailsAsync(string paymentId)
        {
            return await _dbContext.PaymentRecords
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }
    }
}
