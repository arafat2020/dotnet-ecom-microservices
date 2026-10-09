using Shared.Protos.Auth;

namespace gateway.Pages.Admin;

public class ProductsModel : AdminPageModel
{
    public ProductsModel(AuthService.AuthServiceClient authServiceClient) : base(authServiceClient)
    {
    }

    public void OnGet()
    {
        ViewData["Title"] = "Product Explorer";
        ViewData["ActivePage"] = "Products";
    }
}
