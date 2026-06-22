using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using payment_service.db;
using payment_service.Model;

namespace payment_service.Services
{
    /// <summary>
    /// Service implementation for invoice generation and management.
    /// </summary>
    public class InvoiceService : IInvoiceService
    {
        private readonly PaymentDbContext _dbContext;

        public InvoiceService(PaymentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Invoice> GenerateInvoiceAsync(PaymentRecord paymentRecord, string customerEmail, string customerName, string billingAddress)
        {
            var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            
            var invoiceBody = GenerateInvoiceHtmlBody(invoiceNumber, paymentRecord, customerEmail, customerName, billingAddress);

            var invoice = new Invoice
            {
                InvoiceId = Guid.NewGuid().ToString(),
                PaymentRecordId = paymentRecord.PaymentId,
                InvoiceNumber = invoiceNumber,
                CustomerEmail = customerEmail,
                CustomerName = customerName,
                BillingAddress = billingAddress,
                Amount = paymentRecord.Amount,
                InvoiceBody = invoiceBody,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Invoices.Add(invoice);
            await _dbContext.SaveChangesAsync();

            return invoice;
        }

        public async Task<Invoice?> GetInvoiceByPaymentIdAsync(string paymentId)
        {
            return await _dbContext.Invoices
                .FirstOrDefaultAsync(i => i.PaymentRecordId == paymentId);
        }

        private string GenerateInvoiceHtmlBody(string invoiceNumber, PaymentRecord paymentRecord, string customerEmail, string customerName, string billingAddress)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333; margin: 40px; }");
            sb.AppendLine(".invoice-container { max-width: 800px; margin: auto; padding: 20px; border: 1px solid #eee; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1); }");
            sb.AppendLine(".header { display: flex; justify-content: space-between; border-bottom: 2px solid #5a67d8; padding-bottom: 20px; }");
            sb.AppendLine(".title { font-size: 28px; font-weight: bold; color: #5a67d8; }");
            sb.AppendLine(".details { margin-top: 30px; display: flex; justify-content: space-between; }");
            sb.AppendLine(".details-column { width: 45%; }");
            sb.AppendLine(".table-items { width: 100%; border-collapse: collapse; margin-top: 30px; }");
            sb.AppendLine(".table-items th { background-color: #5a67d8; color: white; padding: 10px; text-align: left; }");
            sb.AppendLine(".table-items td { padding: 10px; border-bottom: 1px solid #eee; }");
            sb.AppendLine(".total-row { font-size: 18px; font-weight: bold; text-align: right; margin-top: 20px; padding-top: 10px; border-top: 2px solid #eee; }");
            sb.AppendLine(".footer { margin-top: 50px; font-size: 12px; color: #777; text-align: center; border-top: 1px solid #eee; padding-top: 20px; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='invoice-container'>");
            sb.AppendLine("  <div class='header'>");
            sb.AppendLine("    <div>");
            sb.AppendLine("      <div class='title'>INVOICE</div>");
            sb.AppendLine($"      <div>Invoice #: {invoiceNumber}</div>");
            sb.AppendLine($"      <div>Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div style='text-align: right;'>");
            sb.AppendLine("      <strong>Betopia E-Commerce Ltd.</strong><br/>");
            sb.AppendLine("      100 Tech Center Blvd<br/>");
            sb.AppendLine("      finance@betopia.com");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <div class='details'>");
            sb.AppendLine("    <div class='details-column'>");
            sb.AppendLine("      <strong>Bill To:</strong><br/>");
            sb.AppendLine($"      {customerName}<br/>");
            sb.AppendLine($"      {customerEmail}<br/>");
            sb.AppendLine($"      {billingAddress}");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <div class='details-column' style='text-align: right;'>");
            sb.AppendLine("      <strong>Payment Details:</strong><br/>");
            sb.AppendLine($"      Payment ID: {paymentRecord.PaymentId}<br/>");
            sb.AppendLine($"      Method: {paymentRecord.PaymentMethod.ToUpper()}<br/>");
            sb.AppendLine($"      Status: {paymentRecord.Status}<br/>");
            if (!string.IsNullOrEmpty(paymentRecord.StripePaymentIntentId))
            {
                sb.AppendLine($"      Stripe ID: {paymentRecord.StripePaymentIntentId}<br/>");
            }
            sb.AppendLine("    </div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <table class='table-items'>");
            sb.AppendLine("    <thead>");
            sb.AppendLine("      <tr>");
            sb.AppendLine("        <th>Description</th>");
            sb.AppendLine("        <th style='text-align: right;'>Amount</th>");
            sb.AppendLine("      </tr>");
            sb.AppendLine("    </thead>");
            sb.AppendLine("    <tbody>");
            sb.AppendLine("      <tr>");
            sb.AppendLine($"        <td>E-Commerce Purchase (Order: {paymentRecord.OrderId})</td>");
            sb.AppendLine($"        <td style='text-align: right;'>${paymentRecord.Amount:F2}</td>");
            sb.AppendLine("      </tr>");
            sb.AppendLine("    </tbody>");
            sb.AppendLine("  </table>");
            sb.AppendLine($"  <div class='total-row'>Total Paid: ${paymentRecord.Amount:F2}</div>");
            sb.AppendLine("  <div class='footer'>");
            sb.AppendLine("    Thank you for your business!<br/>");
            sb.AppendLine("    If you have any questions regarding this invoice, please contact support@betopia.com");
            sb.AppendLine("  </div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }
    }
}
