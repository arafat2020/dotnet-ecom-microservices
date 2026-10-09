using Shared.Protos.Auth;

namespace gateway.Pages.Admin;

public class PaymentsModel : AdminPageModel
{
    public PaymentsModel(AuthService.AuthServiceClient authServiceClient) : base(authServiceClient)
    {
    }

    public void OnGet()
    {
        ViewData["Title"] = "Payment Console";
        ViewData["ActivePage"] = "Payments";
    }
}
