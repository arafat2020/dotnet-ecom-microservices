using Microsoft.AspNetCore.Mvc;
using Shared.Protos.Auth;

namespace gateway.Pages.Admin;

public class AuthModel : AdminPageModel
{
    public AuthModel(AuthService.AuthServiceClient authServiceClient) : base(authServiceClient)
    {
    }

    public void OnGet()
    {
        ViewData["Title"] = "Auth Service";
        ViewData["ActivePage"] = "Auth";
    }
}
