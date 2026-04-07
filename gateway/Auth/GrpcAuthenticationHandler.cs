using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Shared.Protos.Auth;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace gateway.Auth;

/// <summary>
/// Custom authentication handler that validates tokens via the Auth Service gRPC endpoint.
/// This allows the Gateway to be highly decoupled and scalable.
/// </summary>
public class GrpcAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AuthService.AuthServiceClient _authServiceClient;
    private readonly ILogger<GrpcAuthenticationHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcAuthenticationHandler"/> class.
    /// </summary>
    public GrpcAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        AuthService.AuthServiceClient authServiceClient)
        : base(options, loggerFactory, encoder)
    {
        _authServiceClient = authServiceClient;
        _logger = loggerFactory.CreateLogger<GrpcAuthenticationHandler>();
    }

    /// <summary>
    /// Handles the authentication process by calling the gRPC Auth Service.
    /// </summary>
    /// <returns>An <see cref="AuthenticateResult"/> indicating success or failure.</returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.NoResult();
        }

        string authorizationHeader = Request.Headers["Authorization"]!;
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        string token = authorizationHeader.Substring("Bearer ".Length).Trim();

        try
        {
            var request = new ValidateTokenRequest { Token = token };
            var response = await _authServiceClient.ValidateTokenAsync(request);

            if (!response.IsValid)
            {
                return AuthenticateResult.Fail("Invalid token provided.");
            }

            // Create claims from the gRPC response
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, response.UserId)
            };

            // Add roles from the response
            foreach (var role in response.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during gRPC token validation.");
            return AuthenticateResult.Fail("Auth Service communication error.");
        }
    }
}
