using Meterly.Application.Common.Interfaces;

namespace Meterly.Infrastructure.Services;

/// <summary>
/// Simulates a payment gateway for development and testing.
/// 
/// DESIGN PATTERN: This is a Stub — it returns pre-configured results 
/// instead of making real network calls.
/// 
/// In production, this would be replaced with StripeGateway, BraintreeGateway, etc.
/// The swap requires zero changes to business logic — only the DI registration 
/// in DependencyInjection.cs changes. That's the power of the Dependency 
/// Inversion Principle.
/// 
/// SUCCESS RATE: 80% — mirrors realistic payment failure rates, and lets us 
/// exercise the failure paths without needing a real card to decline.
/// </summary>
public class MockPaymentGateway : IPaymentGateway
{
    private static readonly Random _random = new();

    private static readonly string[] FailureReasons =
    {
        "insufficient_funds",
        "card_declined",
        "expired_card",
        "incorrect_cvc",
        "processing_error"
    };

    public Task<PaymentResult> ChargeAsync(
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        // Simulate a short network delay (50-200ms) as a real gateway would have
        // (In tests we'd mock this away — no real sleep needed)
        var isSuccess = _random.NextDouble() > 0.20; // 80% success rate

        PaymentResult result;

        if (isSuccess)
        {
            // Generate a fake gateway transaction reference (mimics what Stripe returns)
            var gatewayRef = $"mock_ch_{Guid.NewGuid():N}";
            result = new PaymentResult(
                IsSuccess: true,
                GatewayReference: gatewayRef,
                FailureReason: null
            );
        }
        else
        {
            var reason = FailureReasons[_random.Next(FailureReasons.Length)];
            result = new PaymentResult(
                IsSuccess: false,
                GatewayReference: null,
                FailureReason: reason
            );
        }

        return Task.FromResult(result);
    }
}
