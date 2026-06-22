using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using payment_service.db;
using payment_service.Model;
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

        public PaymentService(
            PaymentDbContext dbContext,
            IInvoiceService invoiceService,
            IConfiguration configuration,
            ILogger<PaymentService> logger)
        {
            _dbContext = dbContext;
            _invoiceService = invoiceService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PaymentRecord> ProcessPaymentAsync(
            string orderId, 
            decimal amount, 
            string paymentMethod, 
            string paymentMethodId, 
            string customerEmail, 
            string customerName, 
            string billingAddress)
        {
            _logger.LogInformation("Processing {Method} payment of {Amount} for Order {OrderId}", paymentMethod, amount, orderId);

            var paymentRecord = new PaymentRecord
            {
                PaymentId = Guid.NewGuid().ToString(),
                OrderId = orderId,
                Amount = amount,
                PaymentMethod = paymentMethod.ToLowerInvariant(),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                if (paymentRecord.PaymentMethod == "cash_in")
                {
                    // Cash in (Cash on delivery / Cash payment) is completed immediately for billing records
                    paymentRecord.Status = "Completed";
                    _dbContext.PaymentRecords.Add(paymentRecord);
                    await _dbContext.SaveChangesAsync();

                    // Generate invoice automatically
                    await _invoiceService.GenerateInvoiceAsync(paymentRecord, customerEmail, customerName, billingAddress);
                }
                else if (paymentRecord.PaymentMethod == "stripe")
                {
                    bool useMock = _configuration.GetValue<bool>("Stripe:UseMock", true);
                    string? secretKey = _configuration["Stripe:SecretKey"];

                    if (useMock || string.IsNullOrEmpty(secretKey))
                    {
                        _logger.LogWarning("Using Mock Stripe Processing because UseMock is true or SecretKey is empty.");
                        paymentRecord.Status = "Completed";
                        paymentRecord.StripePaymentIntentId = $"pi_mock_{Guid.NewGuid().ToString()[..12]}";
                        _dbContext.PaymentRecords.Add(paymentRecord);
                        await _dbContext.SaveChangesAsync();

                        // Generate invoice automatically
                        await _invoiceService.GenerateInvoiceAsync(paymentRecord, customerEmail, customerName, billingAddress);
                    }
                    else
                    {
                        // Real Stripe Integration
                        StripeConfiguration.ApiKey = secretKey;
                        var options = new PaymentIntentCreateOptions
                        {
                            Amount = (long)(amount * 100), // Stripe expects amounts in cents
                            Currency = "usd",
                            PaymentMethod = paymentMethodId,
                            Confirm = true,
                            // Automatic payment methods required for API version stability
                            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                            {
                                Enabled = true,
                                AllowRedirects = "never" // Prevents requiring 3D secure redirects in a backend-to-backend flow
                            },
                            Description = $"Payment for Order {orderId}",
                            ReceiptEmail = customerEmail
                        };

                        var service = new PaymentIntentService();
                        PaymentIntent intent = await service.CreateAsync(options);

                        paymentRecord.StripePaymentIntentId = intent.Id;

                        if (intent.Status == "succeeded")
                        {
                            paymentRecord.Status = "Completed";
                            _dbContext.PaymentRecords.Add(paymentRecord);
                            await _dbContext.SaveChangesAsync();

                            // Generate invoice automatically
                            await _invoiceService.GenerateInvoiceAsync(paymentRecord, customerEmail, customerName, billingAddress);
                        }
                        else
                        {
                            paymentRecord.Status = intent.Status;
                            _dbContext.PaymentRecords.Add(paymentRecord);
                            await _dbContext.SaveChangesAsync();
                            throw new Exception($"Stripe payment did not succeed. Status: {intent.Status}");
                        }
                    }
                }
                else
                {
                    throw new ArgumentException($"Unsupported payment method: {paymentMethod}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process payment for Order {OrderId}", orderId);
                paymentRecord.Status = "Failed";
                
                // If the record was not yet saved, save it with Failed status
                if (_dbContext.Entry(paymentRecord).State == EntityState.Detached)
                {
                    _dbContext.PaymentRecords.Add(paymentRecord);
                    await _dbContext.SaveChangesAsync();
                }
                else
                {
                    await _dbContext.SaveChangesAsync();
                }

                throw;
            }

            return paymentRecord;
        }

        public async Task<PaymentRecord?> GetPaymentDetailsAsync(string paymentId)
        {
            return await _dbContext.PaymentRecords
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }
    }
}
