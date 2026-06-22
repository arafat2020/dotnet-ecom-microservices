using System;

namespace payment_service.Model
{
    /// <summary>
    /// Represents the database record of an automatically generated invoice.
    /// </summary>
    public class Invoice
    {
        /// <summary>
        /// Unique identifier for the invoice.
        /// </summary>
        public string InvoiceId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Foreign key linking to the associated PaymentRecord.
        /// </summary>
        public string PaymentRecordId { get; set; } = string.Empty;

        /// <summary>
        /// A user-friendly, unique invoice number, e.g. INV-2026-XXXXXX.
        /// </summary>
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Email of the customer being billed.
        /// </summary>
        public string CustomerEmail { get; set; } = string.Empty;

        /// <summary>
        /// Name of the customer being billed.
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Billing address of the customer.
        /// </summary>
        public string BillingAddress { get; set; } = string.Empty;

        /// <summary>
        /// Total amount invoiced.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// The main invoice text, HTML, or structured content body.
        /// </summary>
        public string InvoiceBody { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the invoice was generated.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
