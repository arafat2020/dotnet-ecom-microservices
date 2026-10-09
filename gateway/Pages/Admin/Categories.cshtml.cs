using Shared.Protos.Auth;

namespace gateway.Pages.Admin;

public class CategoriesModel : AdminPageModel
{
    public CategoriesModel(AuthService.AuthServiceClient authServiceClient) : base(authServiceClient)
    {
    }

    public void OnGet()
    {
        ViewData["Title"] = "Category Manager";
        ViewData["ActivePage"] = "Categories";
    }
}
