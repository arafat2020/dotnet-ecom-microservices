using Grpc.Core;
using auth_service.interfaces;
using Shared.Protos.Auth;
using System.Security.Claims;

namespace auth_service.GrpcServices;

/// <summary>
/// gRPC Service for Authentication operations.
/// This service is used by the API Gateway to validate tokens and extract user roles.
/// </summary>
public class AuthGrpcService : AuthService.AuthServiceBase
{
    private readonly IAuth _authService;
    private readonly ILogger<AuthGrpcService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthGrpcService"/> class.
    /// </summary>
    /// <param name="authService">The core authentication service for verifying tokens.</param>
    /// <param name="logger">The logger instance.</param>
    public AuthGrpcService(IAuth authService, ILogger<AuthGrpcService> logger)
    {
        _authService = authService;
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
}
