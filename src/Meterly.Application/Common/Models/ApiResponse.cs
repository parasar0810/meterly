namespace Meterly.Application.Common.Models;

/// <summary>
/// Standardized API response envelope.
///
/// WHY A RESPONSE WRAPPER?
/// Without a wrapper, every endpoint returns a different shape:
///   GET /customers → [{ id, name }]
///   POST /customers → { id, name }
///   GET /customers/bad-id → { title, status, detail }
///   
/// This inconsistency makes frontend development painful. The consumer
/// never knows what shape to expect on success vs error.
///
/// WITH a wrapper, every response looks like:
///   { "succeeded": true, "data": {...} }
///   { "succeeded": false, "message": "...", "errors": {...} }
///
/// This is a deliberate design choice — some APIs prefer unwrapped responses
/// (closer to pure REST). We wrap because it makes learning DI and error
/// handling patterns clearer. In an interview, you should be able to explain
/// this tradeoff.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Succeeded { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    private ApiResponse() { }

    /// <summary>
    /// Creates a successful response with data.
    /// Usage: return Ok(ApiResponse.Success(myData));
    /// </summary>
    public static ApiResponse<T> Success(T data, string? message = null) =>
        new() { Succeeded = true, Data = data, Message = message };

    /// <summary>
    /// Creates a failure response with a message.
    /// Usage: return BadRequest(ApiResponse.Failure("Email already exists"));
    /// </summary>
    public static ApiResponse<T> Failure(string message, IReadOnlyDictionary<string, string[]>? errors = null) =>
        new() { Succeeded = false, Message = message, Errors = errors };
}

/// <summary>
/// Non-generic version for responses with no data payload.
/// Usage: return Ok(ApiResponse.Success("Customer deleted."));
/// </summary>
public static class ApiResponse
{
    public static ApiResponse<object?> Success(string? message = null) =>
        ApiResponse<object?>.Success(null, message);

    public static ApiResponse<object?> Failure(string message,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        ApiResponse<object?>.Failure(message, errors);
}

/// <summary>
/// A paginated list wrapper for list endpoints.
/// We'll use this in Phase 4+ when listing customers, invoices, etc.
///
/// WHY PAGINATION?
/// Returning all records is fine for 50 records. When a tenant has
/// 50,000 customers, it crashes the API and the database. Always
/// paginate list endpoints in production APIs.
/// </summary>
public sealed class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
