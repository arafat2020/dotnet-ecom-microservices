using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shared.Protos.Auth;
using System.ComponentModel.DataAnnotations;
using Grpc.Core;

namespace gateway.Pages.Admin;

public class LoginModel : PageModel
{
    private readonly AuthService.AuthServiceClient _authServiceClient;

    public LoginModel(AuthService.AuthServiceClient authServiceClient)
    {
        _authServiceClient = authServiceClient;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public void OnGet(string? error)
    {
        if (!string.IsNullOrEmpty(error))
        {
            ErrorMessage = error;
        }
        ViewData["Title"] = "SuperAdmin Login";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var loginRequest = new LoginRequest
            {
                Email = Input.Email,
                Password = Input.Password
            };
            var loginResponse = await _authServiceClient.LoginAsync(loginRequest);

            if (string.IsNullOrEmpty(loginResponse.Token))
            {
                ErrorMessage = "Invalid credentials.";
                return Page();
            }

            var validateRequest = new ValidateTokenRequest { Token = loginResponse.Token };
            var validateResponse = await _authServiceClient.ValidateTokenAsync(validateRequest);

            if (!validateResponse.IsValid || !validateResponse.Roles.Any(r => r.Name.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)))
            {
                ErrorMessage = "Access Denied: SuperAdmin role is required to access the dashboard.";
                return Page();
            }

            HttpContext.Session.SetString("AdminJwtToken", loginResponse.Token);
            return RedirectToPage("/Admin/Index");
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unauthenticated)
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unavailable)
        {
            ErrorMessage = "The Authentication microservice is currently offline or unreachable. Please verify that the backend services are running.";
            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Authentication error: {ex.Message}";
            return Page();
        }
    }
}
