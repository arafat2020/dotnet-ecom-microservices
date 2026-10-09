using gateway.Models;
using Grpc.Core;
using GrpcStatusCode = Grpc.Core.StatusCode;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace gateway.Controllers;

/// <summary>
/// Base controller that provides helper methods for returning
/// standardised <see cref="ApiResponse{T}"/> envelopes.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>ID of the authenticated user, or an empty string for anonymous requests.</summary>
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    /// <summary>Whether the authenticated user has the Admin or SuperAdmin role.</summary>
    protected bool IsAdmin => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");

    /// <summary>Returns HTTP 200 with a success envelope.</summary>
    protected IActionResult ApiOk<T>(T data, string message = "Success") =>
        Ok(ApiResponse<T>.Ok(data, message));

    /// <summary>Returns HTTP 201 with a created envelope.</summary>
    protected IActionResult ApiCreated<T>(T data, string message = "Resource created successfully.") =>
        StatusCode(201, ApiResponse<T>.Created(data, message));

    /// <summary>Returns HTTP 400 with an error envelope.</summary>
    protected IActionResult ApiBadRequest(string message, IReadOnlyList<string>? errors = null) =>
        BadRequest(ApiResponse<object?>.BadRequest(message, errors));

    /// <summary>Returns HTTP 401 with an error envelope.</summary>
    protected IActionResult ApiUnauthorized(string message) =>
        Unauthorized(ApiResponse<object?>.Unauthorized(message));

    /// <summary>Returns HTTP 403 with an error envelope.</summary>
    protected IActionResult ApiForbidden(string message) =>
        StatusCode(403, ApiResponse<object?>.Forbidden(message));

    /// <summary>Returns HTTP 404 with an error envelope.</summary>
    protected IActionResult ApiNotFound(string message) =>
        NotFound(ApiResponse<object?>.NotFound(message));

    /// <summary>Returns HTTP 409 with an error envelope.</summary>
    protected IActionResult ApiConflict(string message) =>
        Conflict(ApiResponse<object?>.Conflict(message));

    /// <summary>Returns HTTP 500 with an error envelope.</summary>
    protected IActionResult ApiInternalError(string message) =>
        StatusCode(500, ApiResponse<object?>.InternalError(message));

    /// <summary>Maps a failed gRPC call to the HTTP error envelope matching its status code.</summary>
    protected IActionResult ApiRpcError(RpcException ex, string serviceName) => ex.StatusCode switch
    {
        GrpcStatusCode.InvalidArgument => ApiBadRequest(ex.Status.Detail),
        GrpcStatusCode.NotFound => ApiNotFound(ex.Status.Detail),
        GrpcStatusCode.PermissionDenied => ApiForbidden(ex.Status.Detail),
        GrpcStatusCode.FailedPrecondition or GrpcStatusCode.AlreadyExists => ApiConflict(ex.Status.Detail),
        _ => ApiInternalError($"{serviceName} communication error.")
    };
}
