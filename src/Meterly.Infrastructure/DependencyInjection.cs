using Meterly.Infrastructure.Data;
using Meterly.Infrastructure.Services;
using Meterly.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Meterly.Infrastructure;

/// <summary>
/// Registers all Infrastructure layer services into the DI container.
///
/// WHAT BELONGS IN INFRASTRUCTURE?
/// Anything that talks to the "outside world":
///   - Databases (EF Core + PostgreSQL)
///   - External APIs (payment gateways, email providers)
///   - File systems
///   - Caches
///   - Background job queues (Hangfire — added in Phase 12)
///
/// The key principle: if you could swap it out (e.g., replace PostgreSQL
/// with SQL Server, or Hangfire with a cloud queue), it belongs here.
/// Domain and Application layers should never care which database you use.
///
/// HOW CONFIGURATION WORKS:
/// ConnectionStrings:DefaultConnection is read from appsettings.json in
/// development, and from environment variables in Docker/production.
/// We never hard-code connection strings — that would be a security risk.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register EF Core with the Npgsql PostgreSQL provider.
        // The connection string is loaded from configuration.
        // UseNpgsql() tells EF Core to use PostgreSQL syntax when generating SQL.
        services.AddDbContext<MeterlyDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    // If a transient failure occurs (brief PostgreSQL unavailability),
                    // EF Core will automatically retry up to 3 times before failing.
                    // This is called "resilience/retry policy" — important in cloud environments.
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });

            // In development, log the SQL queries EF Core generates.
            // This helps you understand what queries your LINQ is producing.
            // We'll configure this properly via appsettings in a moment.
        });

        // Phase 3: Register Authentication Services
        // ICurrentUserService is Scoped because HttpContext is per-request
        services.AddScoped<Meterly.Application.Common.Interfaces.ICurrentUserService, Meterly.Infrastructure.Services.CurrentUserService>();
        
        // IPasswordHasher is Singleton because it has no state
        services.AddSingleton<Meterly.Application.Common.Interfaces.IPasswordHasher, Meterly.Infrastructure.Services.BcryptPasswordHasher>();
        
        // IJwtTokenGenerator is Singleton because it just reads config once and generates tokens statelessly
        services.AddSingleton<Meterly.Application.Common.Interfaces.IJwtTokenGenerator, Meterly.Infrastructure.Services.JwtTokenGenerator>();

        // Register IMeterlyDbContext to point to MeterlyDbContext (which is scoped by AddDbContext)
        services.AddScoped<IMeterlyDbContext>(provider => provider.GetRequiredService<MeterlyDbContext>());
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();
        services.AddScoped<ILedgerService, LedgerService>();

        // Register Webhook Dispatcher and its HttpClient
        services.AddHttpClient<IWebhookDispatcher, HttpWebhookDispatcher>();

        return services;
    }
}
