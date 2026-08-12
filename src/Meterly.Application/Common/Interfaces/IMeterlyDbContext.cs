using Meterly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Common.Interfaces;

/// <summary>
/// This interface allows the Application layer to query the database without
/// taking a dependency on the Infrastructure layer (EF Core).
/// </summary>
public interface IMeterlyDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Product> Products { get; }
    DbSet<Plan> Plans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<WebhookEndpoint> WebhookEndpoints { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
