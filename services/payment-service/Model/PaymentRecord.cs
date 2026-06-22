using System;

namespace payment_service.Model
{
    /// <summary>
    /// Represents the database record of a payment transaction (Cash In or Stripe).
    /// </summary>
    public class PaymentRecord
    {
        /// <summary>
        /// Unique identifier for the payment.
        /// </summary>
        public string PaymentId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The ID of the associated order.
        /// </summary>
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// The payment amount.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// The payment method used, e.g. "cash_in" or "stripe".
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// The current status of the payment ("Pending", "Completed", "Failed").
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>
        /// Stripe PaymentIntent ID if paid via Stripe.
        /// </summary>
        public string? StripePaymentIntentId { get; set; }

        /// <summary>
        /// Timestamp when the payment record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the payment record was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
