using Meterly.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Meterly.Api.Controllers;

/// <summary>
/// Health check endpoint.
///
/// WHY A HEALTH ENDPOINT?
/// Every production API needs a way to say "I'm alive and ready to serve traffic."
/// Docker, Kubernetes, and load balancers ping this endpoint to know whether to
/// route traffic to this instance.
///
/// GET /api/health → 200 OK → "everything is fine, send me requests"
/// GET /api/health → 500 / timeout → "I'm having problems, route to another instance"
///
/// In later phases, we'll make this endpoint also check the database connection,
/// giving a "degraded" vs "healthy" status.
///
/// CONTROLLER ANATOMY:
/// [ApiController] — enables:
///   - Automatic model validation (returns 400 if request binding fails)
///   - Automatic [FromBody] inference
///   - Problem Details response format
///
/// [Route("api/[controller]")] — sets the base route.
///   [controller] is replaced by the class name without "Controller" suffix.
///   So HealthController → api/health
///
/// ControllerBase — the base class for API controllers.
///   Use ControllerBase (not Controller) for APIs. Controller adds MVC View
///   support which APIs don't need.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly Meterly.Infrastructure.Data.MeterlyDbContext _dbContext;

    // DEPENDENCY INJECTION: The logger and dbContext are injected via the constructor.
    public HealthController(ILogger<HealthController> logger, Meterly.Infrastructure.Data.MeterlyDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns the current health status of the API and checks database connectivity.
    ///
    /// HTTP GET /api/health
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync()
    {
        _logger.LogDebug("Health check requested");

        bool dbConnected = false;
        try
        {
            dbConnected = await _dbContext.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection check failed.");
        }

        var response = new HealthResponse
        {
            Status = dbConnected ? "Healthy" : "Degraded (Database Unreachable)",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            DatabaseConnected = dbConnected
        };

        return Ok(response);
    }

    /// <summary>
    /// Demonstrates the exception handler by throwing intentionally.
    /// Only available in development — useful for testing that your
    /// error middleware is working.
    ///
    /// HTTP GET /api/health/error-test
    /// </summary>
    [HttpGet("error-test")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult ErrorTest([FromQuery] string type = "notfound")
    {
        throw type switch
        {
            "notfound" => new NotFoundException("Customer", Guid.NewGuid()),
            "validation" => new BusinessRuleException("Cannot transition subscription from CANCELED to ACTIVE."),
            "conflict" => new ConflictException("A customer with this email already exists."),
            _ => new NotFoundException("Resource", type)
        };
    }
}

/// <summary>
/// DTO for the health endpoint response.
///
/// WHY A SEPARATE DTO CLASS?
/// Controllers should return DTOs (Data Transfer Objects), never raw strings
/// or anonymous objects (in production code). DTOs are:
/// - Documented in Swagger automatically
/// - Serialized consistently
/// - Easy to version
/// </summary>
public sealed record HealthResponse
{
    public string Status { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Environment { get; init; } = string.Empty;
    public bool DatabaseConnected { get; init; }
}
