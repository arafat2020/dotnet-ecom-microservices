using Microsoft.EntityFrameworkCore;
using payment_service.Model;

namespace payment_service.db
{
    /// <summary>
    /// Database context for the payment service.
    /// </summary>
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

        public DbSet<PaymentRecord> PaymentRecords { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PaymentRecord>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                entity.Property(e => e.OrderId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PaymentMethod).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.StripePaymentIntentId).HasMaxLength(100);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.InvoiceId);
                entity.Property(e => e.PaymentRecordId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(256);
                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.BillingAddress).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.InvoiceBody).IsRequired();
            });
        }
    }
}
