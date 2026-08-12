using System.Security.Cryptography;
using FluentValidation;
using MediatR;
using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Entities;

namespace Meterly.Application.Webhooks.Commands.CreateWebhookEndpoint;

public record CreateWebhookEndpointCommand : IRequest<Guid>
{
    public string Url { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public class CreateWebhookEndpointCommandValidator : AbstractValidator<CreateWebhookEndpointCommand>
{
    public CreateWebhookEndpointCommandValidator()
    {
        RuleFor(v => v.Url)
            .NotEmpty().WithMessage("Webhook URL is required.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var outUri) && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Webhook URL must be a valid HTTP or HTTPS URL.")
            .MaximumLength(2000);

        RuleFor(v => v.Description)
            .MaximumLength(500);
    }
}

public class CreateWebhookEndpointCommandHandler : IRequestHandler<CreateWebhookEndpointCommand, Guid>
{
    private readonly IMeterlyDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateWebhookEndpointCommandHandler(IMeterlyDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateWebhookEndpointCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId
            ?? throw new UnauthorizedException("User is not associated with a tenant.");

        // Generate a random 256-bit secret (32 bytes -> 64 hex chars or base64)
        var secretBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(secretBytes);
        }
        var secret = Convert.ToBase64String(secretBytes);

        var endpoint = new WebhookEndpoint
        {
            TenantId = tenantId,
            Url = request.Url,
            Description = request.Description,
            Secret = secret,
            IsActive = true
        };

        _context.WebhookEndpoints.Add(endpoint);
        await _context.SaveChangesAsync(cancellationToken);

        return endpoint.Id;
    }
}
