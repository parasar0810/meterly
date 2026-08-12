using Meterly.Domain.Common;

namespace Meterly.Domain.Entities;

/// <summary>
/// Represents a product or service offered by a Tenant (e.g., "Standard SaaS Platform", "Premium Support").
/// </summary>
public class Product : BaseEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Plan> Plans { get; set; } = new List<Plan>();

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
