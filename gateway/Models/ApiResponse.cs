namespace gateway.Models;

/// <summary>
/// Standardised envelope returned by every Gateway endpoint.
/// </summary>
/// <typeparam name="T">Type of the <see cref="Data"/> payload.</typeparam>
public class ApiResponse<T>
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>HTTP status code mirrored in the body.</summary>
    public int StatusCode { get; init; }

    /// <summary>Human-readable message describing the result.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>UTC timestamp of when the response was produced.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Unique identifier for correlating this request in logs.</summary>
    public string RequestId { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>The operation payload; <c>null</c> for error responses.</summary>
    public T? Data { get; init; }

    /// <summary>Validation or domain errors, if any.</summary>
    public IReadOnlyList<string>? Errors { get; init; }

    // -----------------------------------------------------------------
    // Factory helpers
    // -----------------------------------------------------------------

    /// <summary>Creates a 200-OK success response.</summary>
    public static ApiResponse<T> Ok(T data, string message = "Success") =>
        new()
        {
            Success = true,
            StatusCode = 200,
            Message = message,
            Data = data
        };

    /// <summary>Creates a 201-Created success response.</summary>
    public static ApiResponse<T> Created(T data, string message = "Resource created successfully.") =>
        new()
        {
            Success = true,
            StatusCode = 201,
            Message = message,
            Data = data
        };

    /// <summary>Creates an error response with the given HTTP status code.</summary>
    public static ApiResponse<T> Fail(int statusCode, string message, IReadOnlyList<string>? errors = null) =>
        new()
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors
        };

    // Convenience overloads for common error codes
    public static ApiResponse<T> BadRequest(string message, IReadOnlyList<string>? errors = null)  => Fail(400, message, errors);
    public static ApiResponse<T> Unauthorized(string message)  => Fail(401, message);
    public static ApiResponse<T> Forbidden(string message)     => Fail(403, message);
    public static ApiResponse<T> NotFound(string message)      => Fail(404, message);
    public static ApiResponse<T> Conflict(string message)      => Fail(409, message);
    public static ApiResponse<T> InternalError(string message) => Fail(500, message);
}

/// <summary>Non-generic convenience alias for responses that carry no payload.</summary>
public sealed class ApiResponse : ApiResponse<object?>
{
    public static ApiResponse OkEmpty(string message = "Success") =>
        new() { Success = true, StatusCode = 200, Message = message };

    public new static ApiResponse Fail(int statusCode, string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, StatusCode = statusCode, Message = message, Errors = errors };
}
