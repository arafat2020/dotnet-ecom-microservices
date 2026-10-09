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
        /// The amount is read from the order; the order must be Pending and belong to <paramref name="userId"/>.
        /// A declined payment is returned as a record with status "Failed".
        /// </summary>
        /// <exception cref="ArgumentException">The payment method is not supported.</exception>
        /// <exception cref="KeyNotFoundException">The order does not exist.</exception>
        /// <exception cref="UnauthorizedAccessException">The order belongs to another user.</exception>
        /// <exception cref="InvalidOperationException">The order is not Pending or is already paid.</exception>
        Task<PaymentRecord> ProcessPaymentAsync(
            string orderId, 
            string userId, 
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
