using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shared.Protos.Auth;

namespace gateway.Pages.Admin;

/// <summary>
/// Base class for all protected Admin Dashboard pages.
/// Validates the JWT token stored in Session and checks for the "SuperAdmin" role.
/// </summary>
public abstract class AdminPageModel : PageModel
{
    protected readonly AuthService.AuthServiceClient AuthServiceClient;

    protected AdminPageModel(AuthService.AuthServiceClient authServiceClient)
    {
        AuthServiceClient = authServiceClient;
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var token = HttpContext.Session.GetString("AdminJwtToken");

        if (string.IsNullOrWhiteSpace(token))
        {
            context.Result = RedirectToPage("/Admin/Login");
            return;
        }

        try
        {
            var validateRequest = new ValidateTokenRequest { Token = token };
            var response = await AuthServiceClient.ValidateTokenAsync(validateRequest);

            if (!response.IsValid || !response.Roles.Any(r => r.Name.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)))
            {
                // Not a SuperAdmin or invalid token
                HttpContext.Session.Remove("AdminJwtToken");
                context.Result = RedirectToPage("/Admin/Login", new { error = "SuperAdmin access required." });
                return;
            }
        }
        catch (Grpc.Core.RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.Unavailable)
        {
            context.Result = RedirectToPage("/Admin/Login", new { error = "Auth Service is currently offline. Please ensure all backend microservices are running." });
            return;
        }
        catch
        {
            // If the communication failed, redirect to login with error
            context.Result = RedirectToPage("/Admin/Login", new { error = "Auth Service is unavailable." });
            return;
        }

        await next();
    }
}
