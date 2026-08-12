namespace Meterly.Application.Common.Exceptions;

/// <summary>
/// Custom exception hierarchy for Meterly.
///
/// WHY CUSTOM EXCEPTIONS?
/// ASP.NET Core has no built-in understanding of "this customer doesn't exist"
/// vs "this state transition is invalid" vs "you don't have permission". 
/// By creating specific exception types, our global error handler can map
/// each exception to the right HTTP status code automatically.
///
/// PATTERN: throw a semantic exception in application/domain code,
/// catch and map it in the middleware layer. Controllers stay clean.
/// </summary>

/// <summary>
/// Base for all Meterly application exceptions.
/// Carries an HTTP status code so the middleware can respond correctly.
/// </summary>
public abstract class MeterlyException : Exception
{
    public int StatusCode { get; }

    protected MeterlyException(string message, int statusCode = 500)
        : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Thrown when a requested resource does not exist.
/// Maps to HTTP 404 Not Found.
///
/// Example: GET /api/customers/{id} where id doesn't exist.
/// </summary>
public sealed class NotFoundException : MeterlyException
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with key '{key}' was not found.", 404)
    {
    }
}

/// <summary>
/// Thrown when user input fails validation.
/// Maps to HTTP 400 Bad Request.
///
/// Example: Creating a customer without an email.
/// </summary>
public sealed class ValidationException : MeterlyException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.", 400)
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
}

/// <summary>
/// Thrown when an operation is forbidden by business rules.
/// Maps to HTTP 422 Unprocessable Entity.
///
/// Example: Trying to transition a subscription from CANCELED → ACTIVE
/// (invalid state machine transition).
///
/// WHY 422 NOT 400?
/// 400 = the request syntax is wrong (invalid JSON, missing required fields).
/// 422 = the request is syntactically valid, but semantically wrong.
///       "Your request was understood but cannot be processed in this state."
/// </summary>
public sealed class BusinessRuleException : MeterlyException
{
    public BusinessRuleException(string message)
        : base(message, 422)
    {
    }
}

/// <summary>
/// Thrown when the authenticated user tries to access another tenant's data.
/// Maps to HTTP 403 Forbidden.
///
/// This should rarely be thrown if our tenant filtering is implemented
/// correctly — but it's a safety net.
/// </summary>
public sealed class TenantAccessException : MeterlyException
{
    public TenantAccessException()
        : base("You do not have access to this resource.", 403)
    {
    }
}

/// <summary>
/// Thrown when the user is not authenticated.
/// Maps to HTTP 401 Unauthorized.
/// </summary>
public sealed class UnauthorizedException : MeterlyException
{
    public UnauthorizedException(string message = "Authentication is required.")
        : base(message, 401)
    {
    }
}

/// <summary>
/// Thrown when a request conflicts with an existing resource.
/// Maps to HTTP 409 Conflict.
///
/// Example: Registering with an email that already exists.
/// </summary>
public sealed class ConflictException : MeterlyException
{
    public ConflictException(string message)
        : base(message, 409)
    {
    }
}
