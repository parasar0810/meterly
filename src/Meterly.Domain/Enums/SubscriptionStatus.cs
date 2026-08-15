namespace Meterly.Domain.Enums;

public enum SubscriptionStatus
{
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    Canceled = 4,
    /// <summary>
    /// Set when all dunning retries have been exhausted. The subscription
    /// is paused — customer cannot access the service until payment is resolved.
    /// </summary>
    Suspended = 5
}
