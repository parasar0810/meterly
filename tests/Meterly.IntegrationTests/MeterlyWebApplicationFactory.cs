using Hangfire;
using Meterly.Application.Common.Interfaces;
using Meterly.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Meterly.IntegrationTests;

/// <summary>
/// A custom WebApplicationFactory that spins up the REAL Meterly.Api application
/// in-memory for integration testing.
///
/// WHAT DOES THIS DO?
/// WebApplicationFactory creates a real ASP.NET Core test server — controllers,
/// middleware, routing, validation, auth — all wired up just like production,
/// but running entirely in-memory with no network ports.
///
/// WE REPLACE:
///   - PostgreSQL → EF Core InMemory (no Docker required for tests)
///   - IPaymentGateway → Mock (no real money)
///   - IWebhookDispatcher → Mock (no real HTTP calls)
///   - Hangfire → InMemory (no PostgreSQL job storage)
///
/// WHY NOT USE THE REAL DB IN TESTS?
/// Integration tests should be fast and self-contained. Requiring a running
/// PostgreSQL instance makes CI/CD setups complex. For DB-level tests we use
/// a real PG via Docker only in staging/smoke tests.
/// </summary>
public class MeterlyWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Inject a dummy JWT secret for tests so the JWT auth middleware doesn't crash
            config.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Jwt:Secret", "this_is_a_very_long_super_secret_test_key_for_jwt_auth_123!")
            });
        });

        builder.ConfigureServices(services =>
        {
            // ─────────────────────────────────────────────────────────────────
            // 1. Replace PostgreSQL DbContext with InMemory
            // ─────────────────────────────────────────────────────────────────
            var dbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<MeterlyDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            // Shared in-memory database so all requests within one test see the same data
            services.AddDbContext<MeterlyDbContext>(opts =>
                opts.UseInMemoryDatabase("MeterlyTestDb"));

            // Re-register IMeterlyDbContext pointing to the test DbContext
            var iDbDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IMeterlyDbContext));
            if (iDbDescriptor != null)
                services.Remove(iDbDescriptor);
            services.AddScoped<IMeterlyDbContext>(sp => sp.GetRequiredService<MeterlyDbContext>());

            // ─────────────────────────────────────────────────────────────────
            // 2. Replace IPaymentGateway with a mock that always succeeds
            // ─────────────────────────────────────────────────────────────────
            var gatewayDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IPaymentGateway));
            if (gatewayDescriptor != null)
                services.Remove(gatewayDescriptor);

            var mockGateway = new Mock<IPaymentGateway>();
            mockGateway
                .Setup(g => g.ChargeAsync(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentResult(true, "test-gateway-ref", null));
            mockGateway
                .Setup(g => g.RefundAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentResult(true, "test-refund-ref", null));
            services.AddSingleton(mockGateway.Object);

            // ─────────────────────────────────────────────────────────────────
            // 3. Replace IWebhookDispatcher with a no-op mock
            // ─────────────────────────────────────────────────────────────────
            var webhookDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IWebhookDispatcher));
            if (webhookDescriptor != null)
                services.Remove(webhookDescriptor);

            var mockWebhook = new Mock<IWebhookDispatcher>();
            services.AddScoped(sp => mockWebhook.Object);

            // ─────────────────────────────────────────────────────────────────
            // 4. Replace Hangfire PostgreSQL storage with InMemory
            // ─────────────────────────────────────────────────────────────────
            // Remove the PostgreSQL-backed Hangfire storage registration
            var hangfireDescriptors = services.Where(d =>
                d.ServiceType.FullName?.Contains("Hangfire") == true ||
                d.ImplementationType?.FullName?.Contains("Hangfire") == true ||
                d.ImplementationType?.FullName?.Contains("PostgreSql") == true)
                .ToList();
            foreach (var d in hangfireDescriptors)
                services.Remove(d);

            services.AddHangfire(config => config.UseInMemoryStorage());
            services.AddHangfireServer();
        });
    }

    /// <summary>
    /// Creates a fresh HTTP client with no base address (uses the test server).
    /// </summary>
    public HttpClient CreateApiClient() => CreateClient();
}
