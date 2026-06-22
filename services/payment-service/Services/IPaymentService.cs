using System.Threading.Tasks;
using payment_service.Model;

namespace payment_service.Services
{
    /// <summary>
    /// Service for processing payments and managing payment history records.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Processes a payment (via Cash In or Stripe) and registers the billing record.
        /// </summary>
        Task<PaymentRecord> ProcessPaymentAsync(
            string orderId, 
            decimal amount, 
            string paymentMethod, 
            string paymentMethodId, 
            string customerEmail, 
            string customerName, 
            string billingAddress);

        /// <summary>
        /// Retrieves a billing payment record by its unique identifier.
        /// </summary>
        Task<PaymentRecord?> GetPaymentDetailsAsync(string paymentId);
    }
}
