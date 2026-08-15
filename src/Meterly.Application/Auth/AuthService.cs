using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IMeterlyDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly FluentValidation.IValidator<RegisterRequest> _registerValidator;

    public AuthService(
        IMeterlyDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        FluentValidation.IValidator<RegisterRequest> registerValidator)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _registerValidator = registerValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                .ToDictionary(g => g.Key, g => g.ToArray());
            throw new ValidationException(errors);
        }

        // 1. Check if user already exists across the whole system
        if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        // 2. Create the Tenant (since they are registering a new company)
        var tenant = new Tenant
        {
            Name = request.TenantName
        };
        _dbContext.Tenants.Add(tenant);

        // 3. Create the User and link to Tenant
        var user = new User
        {
            Tenant = tenant,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password)
        };
        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 4. Generate Token
        var token = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponse(
            Token: token,
            UserId: user.Id,
            TenantId: tenant.Id,
            Email: user.Email,
            FullName: $"{user.FirstName} {user.LastName}"
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Find user by email
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null)
        {
            // We use Unauthorized access for both wrong email and wrong password 
            // to prevent email enumeration attacks.
            throw new UnauthorizedException("Invalid email or password.");
        }

        // 2. Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        // 3. Generate Token
        var token = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResponse(
            Token: token,
            UserId: user.Id,
            TenantId: user.TenantId,
            Email: user.Email,
            FullName: $"{user.FirstName} {user.LastName}"
        );
    }
}
