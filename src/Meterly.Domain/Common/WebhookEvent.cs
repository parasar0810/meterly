using System.Text.Json;

namespace Meterly.Domain.Common;

/// <summary>
/// A standardized structure for webhooks sent to tenants.
/// </summary>
public record WebhookEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string EventType { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public JsonElement Data { get; init; }
}
