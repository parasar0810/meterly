using Meterly.Domain.Common;

namespace Meterly.Domain.Entities;

/// <summary>
/// Represents a company or organization that signs up to use Meterly.
/// This is the root of our multi-tenant architecture. Every other entity
/// (Customer, Plan, Subscription) will belong to a Tenant.
///
/// WHY THESE SPECIFIC FIELDS?
/// - Name: The display name of the company.
/// - Status: Allows suspending an entire tenant (e.g., for non-payment)
///   without deleting their data.
/// - IsDeleted: We use ISoftDeletable so that if a tenant cancels, we keep
///   their data for legal/accounting audits but hide it from normal queries.
/// </summary>
public class Tenant : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    
    // Status can be Active, Suspended, etc. For now we use a string,
    // but in later phases, this could be strongly typed if needed.
    public string Status { get; set; } = "Active";

    // Implementing ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Plan> Plans { get; set; } = new List<Plan>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<WebhookEndpoint> WebhookEndpoints { get; set; } = new List<WebhookEndpoint>();
}
