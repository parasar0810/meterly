using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Meterly.Infrastructure.Services;

public class HttpWebhookDispatcher : IWebhookDispatcher
{
    private readonly HttpClient _httpClient;
    private readonly IMeterlyDbContext _dbContext;
    private readonly ILogger<HttpWebhookDispatcher> _logger;

    public HttpWebhookDispatcher(
        HttpClient httpClient,
        IMeterlyDbContext dbContext,
        ILogger<HttpWebhookDispatcher> logger)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task DispatchAsync(Guid tenantId, WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        // Find all active endpoints for this tenant
        var endpoints = await _dbContext.WebhookEndpoints
            .IgnoreQueryFilters() // We might be running in a background context later without ICurrentUserService
            .Where(w => w.TenantId == tenantId && w.IsActive && !w.IsDeleted)
            .ToListAsync(cancellationToken);

        if (!endpoints.Any())
        {
            _logger.LogInformation("No active webhook endpoints found for Tenant {TenantId}.", tenantId);
            return;
        }

        var payload = JsonSerializer.Serialize(webhookEvent);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        foreach (var endpoint in endpoints)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint.Url);
                
                // Add the signature header
                var signature = GenerateSignature(payloadBytes, endpoint.Secret);
                request.Headers.Add("Meterly-Signature", signature);

                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Dispatching webhook {EventType} to {Url}", webhookEvent.EventType, endpoint.Url);
                
                var response = await _httpClient.SendAsync(request, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Webhook dispatch to {Url} failed with status {StatusCode}", endpoint.Url, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while dispatching webhook to {Url}", endpoint.Url);
            }
        }
    }

    private string GenerateSignature(byte[] payloadBytes, string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(secretBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
