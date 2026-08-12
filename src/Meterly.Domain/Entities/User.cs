using Meterly.Domain.Common;

namespace Meterly.Domain.Entities;

/// <summary>
/// Represents a user who can log into the Meterly platform.
/// Every user belongs to exactly one Tenant.
/// </summary>
public class User : BaseEntity, ISoftDeletable
{
    // The link to the organization this user belongs to
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // We store the hashed password, never the plaintext password
    public string PasswordHash { get; set; } = string.Empty;

    // Implementing ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
