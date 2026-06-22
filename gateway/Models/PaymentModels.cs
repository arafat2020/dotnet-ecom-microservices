using System.ComponentModel.DataAnnotations;

namespace gateway.Models
{
    /// <summary>
    /// Request DTO for processing a payment transaction.
    /// </summary>
    public class ProcessPaymentRequestDto
    {
        /// <summary>
        /// The ID of the associated order.
        /// </summary>
        [Required]
        public string OrderId { get; set; } = null!;

        /// <summary>
        /// The payment amount. Must be greater than zero.
        /// </summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        /// <summary>
        /// The payment method. Allowed values: "cash_in" or "stripe".
        /// </summary>
        [Required]
        [RegularExpression("^(cash_in|stripe)$", ErrorMessage = "PaymentMethod must be 'cash_in' or 'stripe'.")]
        public string PaymentMethod { get; set; } = null!;

        /// <summary>
        /// Required if PaymentMethod is 'stripe'. Optional/unused if 'cash_in'.
        /// </summary>
        public string? PaymentMethodId { get; set; }

        /// <summary>
        /// Email of the customer.
        /// </summary>
        [Required]
        [EmailAddress]
        public string CustomerEmail { get; set; } = null!;

        /// <summary>
        /// Full name of the customer.
        /// </summary>
        [Required]
        public string CustomerName { get; set; } = null!;

        /// <summary>
        /// Customer's billing address.
        /// </summary>
        [Required]
        public string BillingAddress { get; set; } = null!;
    }

    /// <summary>
    /// Response DTO from processing a payment transaction.
    /// </summary>
    public class ProcessPaymentResponseDto
    {
        /// <summary>
        /// Indicates if the payment succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Descriptive message about the processing outcome.
        /// </summary>
        public string Message { get; set; } = null!;

        /// <summary>
        /// Unique payment identifier.
        /// </summary>
        public string PaymentId { get; set; } = null!;

        /// <summary>
        /// The transaction status, e.g. "Completed", "Pending", "Failed".
        /// </summary>
        public string Status { get; set; } = null!;

        /// <summary>
        /// Stripe PaymentIntent ID if paid via Stripe.
        /// </summary>
        public string StripePaymentIntentId { get; set; } = null!;
    }

    /// <summary>
    /// Details of a payment billing record.
    /// </summary>
    public class PaymentRecordDto
    {
        /// <summary>Unique payment record ID.</summary>
        public string PaymentId { get; set; } = null!;
        /// <summary>Associated order ID.</summary>
        public string OrderId { get; set; } = null!;
        /// <summary>Amount charged.</summary>
        public decimal Amount { get; set; }
        /// <summary>Payment method used ("stripe" or "cash_in").</summary>
        public string PaymentMethod { get; set; } = null!;
        /// <summary>Transaction status.</summary>
        public string Status { get; set; } = null!;
        /// <summary>Stripe PaymentIntent ID if applicable.</summary>
        public string StripePaymentIntentId { get; set; } = null!;
        /// <summary>Creation timestamp (ISO 8601 string).</summary>
        public string CreatedAt { get; set; } = null!;
    }

    /// <summary>
    /// Details of a generated invoice.
    /// </summary>
    public class InvoiceDto
    {
        /// <summary>Unique invoice record ID.</summary>
        public string InvoiceId { get; set; } = null!;
        /// <summary>Associated payment record ID.</summary>
        public string PaymentId { get; set; } = null!;
        /// <summary>Unique invoice number.</summary>
        public string InvoiceNumber { get; set; } = null!;
        /// <summary>Email of the billed customer.</summary>
        public string CustomerEmail { get; set; } = null!;
        /// <summary>Name of the billed customer.</summary>
        public string CustomerName { get; set; } = null!;
        /// <summary>Billing address of the customer.</summary>
        public string BillingAddress { get; set; } = null!;
        /// <summary>Billed amount.</summary>
        public decimal Amount { get; set; }
        /// <summary>The invoice body (HTML or formatted text).</summary>
        public string InvoiceBody { get; set; } = null!;
        /// <summary>Generation timestamp (ISO 8601 string).</summary>
        public string CreatedAt { get; set; } = null!;
    }
}
