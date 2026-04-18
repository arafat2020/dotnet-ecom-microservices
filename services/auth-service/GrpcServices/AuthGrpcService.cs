using Grpc.Core;
using auth_service.interfaces;
using auth_service.Interfaces;
using auth_service.DTOs;
using Shared.Protos.Auth;
using System.Security.Claims;

namespace auth_service.GrpcServices;

/// <summary>
/// gRPC Service for Authentication operations.
/// Handles token validation, user registration, and user login.
/// </summary>
public class AuthGrpcService : AuthService.AuthServiceBase
{
    private readonly IAuth _authService;
    private readonly IAuthservice _userAuthService;
    private readonly ILogger<AuthGrpcService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthGrpcService"/> class.
    /// </summary>
    /// <param name="authService">The core authentication service for verifying tokens.</param>
    /// <param name="userAuthService">The application-level auth service for registration and login.</param>
    /// <param name="logger">The logger instance.</param>
    public AuthGrpcService(IAuth authService, IAuthservice userAuthService, ILogger<AuthGrpcService> logger)
    {
        _authService = authService;
        _userAuthService = userAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Validates a JWT token and returns the user's ID and Roles.
    /// </summary>
    /// <param name="request">The validation request containing the token.</param>
    /// <param name="context">The gRPC call context.</param>
    /// <returns>A response indicating validity and containing user metadata.</returns>
    public override Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, ServerCallContext context)
    {
        try
        {
            var principal = _authService.VerifyToken(request.Token);

            if (principal == null)
            {
                return Task.FromResult(new ValidateTokenResponse
                {
                    IsValid = false
                });
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal.FindFirst("sub")?.Value
                        ?? "unknown";

            var response = new ValidateTokenResponse
            {
                IsValid = true,
                UserId = userId
            };

            // Extract roles from claims
            var roles = principal.FindAll(ClaimTypes.Role)
                                 .Select(c => new Shared.Protos.Role { Name = c.Value });

            foreach (var role in roles)
            {
                response.Roles.Add(role);
            }

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token via gRPC.");
            return Task.FromResult(new ValidateTokenResponse
            {
                IsValid = false
            });
        }
    }

    /// <summary>
    /// Registers a new user and returns a JWT token on success.
    /// </summary>
    /// <param name="request">The registration request containing username, email, and password.</param>
    /// <param name="context">The gRPC call context.</param>
    /// <returns>An <see cref="AuthTokenResponse"/> with a JWT and its expiration.</returns>
    public override async Task<AuthTokenResponse> Register(RegisterRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new RegisterDto
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password
            };

            var result = await _userAuthService.RegisterAsync(dto);

            return new AuthTokenResponse
            {
                Token = result.Token,
                Expiration = result.Expiration.ToString("O") // ISO 8601
            };
        }
        catch (InvalidOperationException ex)
        {
            // Username or email already exists
            _logger.LogWarning(ex, "Registration conflict: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.AlreadyExists, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during gRPC Register.");
            throw new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred during registration."));
        }
    }

    /// <summary>
    /// Logs in an existing user and returns a JWT token on success.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <param name="context">The gRPC call context.</param>
    /// <returns>An <see cref="AuthTokenResponse"/> with a JWT and its expiration.</returns>
    public override async Task<AuthTokenResponse> Login(LoginRequest request, ServerCallContext context)
    {
        try
        {
            var dto = new LoginDto
            {
                Email = request.Email,
                Password = request.Password
            };

            var result = await _userAuthService.LoginAsync(dto);

            return new AuthTokenResponse
            {
                Token = result.Token,
                Expiration = result.Expiration.ToString("O") // ISO 8601
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login failed: {Message}", ex.Message);
            throw new RpcException(new Status(StatusCode.Unauthenticated, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during gRPC Login.");
            throw new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred during login."));
        }
    }
}
