using Microsoft.AspNetCore.Mvc;
using Shared.Protos.Auth;

namespace gateway.Pages.Admin;

[IgnoreAntiforgeryToken]
public class IndexModel : AdminPageModel
{
    public IndexModel(AuthService.AuthServiceClient authServiceClient) : base(authServiceClient)
    {
    }

    public void OnGet()
    {
        ViewData["Title"] = "Dashboard";
        ViewData["ActivePage"] = "Index";
    }
}
