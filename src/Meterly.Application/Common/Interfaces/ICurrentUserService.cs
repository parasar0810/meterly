namespace Meterly.Application.Common.Interfaces;

/// <summary>
/// Provides access to the currently authenticated user's ID and Tenant ID.
/// This abstraction is critical: the Application layer doesn't know about HTTP Contexts
/// or JWTs. It just knows it needs a Tenant ID.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? TenantId { get; }
}
