using System.Threading.Tasks;
using payment_service.Model;

namespace payment_service.Services
{
    /// <summary>
    /// Service for generating and retrieving billing invoices.
    /// </summary>
    public interface IInvoiceService
    {
        /// <summary>
        /// Automatically generates a new invoice for a completed payment.
        /// </summary>
        Task<Invoice> GenerateInvoiceAsync(PaymentRecord paymentRecord, string customerEmail, string customerName, string billingAddress);

        /// <summary>
        /// Retrieves an invoice associated with a payment record ID.
        /// </summary>
        Task<Invoice?> GetInvoiceByPaymentIdAsync(string paymentId);
    }
}
