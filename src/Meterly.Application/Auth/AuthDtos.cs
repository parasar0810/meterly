namespace Meterly.Application.Auth;

// DTOs for the Authentication flow
public record RegisterRequest(
    string TenantName,
    string FirstName,
    string LastName,
    string Email,
    string Password
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    Guid UserId,
    Guid TenantId,
    string Email,
    string FullName
);
