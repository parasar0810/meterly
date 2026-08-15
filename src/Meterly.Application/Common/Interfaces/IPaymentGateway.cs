namespace Meterly.Application.Common.Interfaces;

/// <summary>
/// Represents the result of a payment attempt from the gateway.
/// 
/// WHY A RECORD?
/// Records are immutable value objects in C#. A PaymentResult shouldn't be 
/// mutated after the gateway returns it — it's a snapshot of what happened.
/// </summary>
public record PaymentResult(
    bool IsSuccess,
    string? GatewayReference,
    string? FailureReason
);

/// <summary>
/// Abstracts the payment processing gateway.
/// 
/// WHY AN INTERFACE?
/// Our application layer has zero knowledge of HOW payments are processed.
/// This means we can:
///   - Use MockPaymentGateway in development (no real money moves)
///   - Use StripeGateway in production
///   - Use a TestPaymentGateway in unit tests that always succeeds
/// 
/// Swapping the gateway requires zero changes to business logic — 
/// only the DI registration changes. This is the Dependency Inversion Principle.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<PaymentResult> RefundAsync(
        string gatewayReference,
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
