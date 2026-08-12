using Meterly.Domain.Common;

namespace Meterly.Application.Common.Interfaces;

public interface IWebhookDispatcher
{
    Task DispatchAsync(Guid tenantId, WebhookEvent webhookEvent, CancellationToken cancellationToken);
}
