using gateway.Models;
using Grpc.Core;
using GrpcStatusCode = Grpc.Core.StatusCode;
using Microsoft.AspNetCore.Mvc;
using Shared.Protos.Auth;

namespace gateway.Controllers;

/// <summary>
/// Controller for Authentication operations.
/// Acts as a REST-to-gRPC proxy for the Auth Service.
/// All responses are wrapped in a standardized <see cref="ApiResponse{T}"/> envelope.
/// </summary>
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly AuthService.AuthServiceClient _authServiceClient;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="authServiceClient">The gRPC client for the Auth Service.</param>
    /// <param name="logger">The logger instance.</param>
    public AuthController(AuthService.AuthServiceClient authServiceClient, ILogger<AuthController> logger)
    {
        _authServiceClient = authServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">The registration details (username, email, password).</param>
    /// <returns>A standardized response containing a JWT token and expiration on success.</returns>
    /// <response code="200">Registration successful — returns the token envelope.</response>
    /// <response code="400">Request body failed validation.</response>
    /// <response code="409">Username or email is already taken.</response>
    /// <response code="500">Auth Service is unavailable.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var grpcRequest = new RegisterRequest
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password
            };

            var response = await _authServiceClient.RegisterAsync(grpcRequest);

            return ApiOk(new AuthTokenDto
            {
                Token = response.Token,
                Expiration = response.Expiration
            }, "User registered successfully.");
        }
        catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.AlreadyExists)
        {
            _logger.LogWarning("Registration conflict: {Detail}", ex.Status.Detail);
            return ApiConflict(ex.Status.Detail);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error during Register.");
            return ApiInternalError("Auth Service communication error.");
        }
    }

    /// <summary>
    /// Logs in an existing user.
    /// </summary>
    /// <param name="request">The login credentials (email and password).</param>
    /// <returns>A standardized response containing a JWT token and expiration on success.</returns>
    /// <response code="200">Login successful — returns the token envelope.</response>
    /// <response code="400">Request body failed validation.</response>
    /// <response code="401">Invalid email or password.</response>
    /// <response code="500">Auth Service is unavailable.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var grpcRequest = new LoginRequest
            {
                Email = request.Email,
                Password = request.Password
            };

            var response = await _authServiceClient.LoginAsync(grpcRequest);

            return ApiOk(new AuthTokenDto
            {
                Token = response.Token,
                Expiration = response.Expiration
            }, "Login successful.");
        }
        catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.Unauthenticated)
        {
            _logger.LogWarning("Login failed: {Detail}", ex.Status.Detail);
            return ApiUnauthorized(ex.Status.Detail);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error during Login.");
            return ApiInternalError("Auth Service communication error.");
        }
    }
}
