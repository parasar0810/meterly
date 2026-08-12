using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Meterly.Application;

/// <summary>
/// Registers all Application layer services into the DI container.
///
/// DEPENDENCY INJECTION PATTERN:
/// This static class is called "DI extension method" pattern. Instead of
/// registering everything in Program.cs (which would become 500 lines),
/// each project layer registers its own services in a DependencyInjection.cs.
///
/// In Program.cs, you just call:
///   builder.Services.AddApplication();
///   builder.Services.AddInfrastructure(config);
///
/// This keeps Program.cs clean and each layer self-contained.
///
/// WHAT GETS REGISTERED HERE:
/// 1. MediatR — scans this assembly for all IRequestHandler implementations
/// 2. FluentValidation — scans this assembly for all AbstractValidator implementations
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // MediatR looks at this assembly and automatically registers every class
        // that implements IRequestHandler<TRequest, TResponse>.
        // This is called "assembly scanning" — we don't register handlers one by one.
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            
            // Add ValidationBehavior to the MediatR pipeline
            config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(Meterly.Application.Common.Behaviors.ValidationBehavior<,>));
        });

        // FluentValidation scans for all AbstractValidator<T> classes
        // and registers them as IValidator<T> in the DI container.
        services.AddValidatorsFromAssembly(assembly);

        // Phase 3: Register application services
        services.AddScoped<Meterly.Application.Auth.IAuthService, Meterly.Application.Auth.AuthService>();

        return services;
    }
}
