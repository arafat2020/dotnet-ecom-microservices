using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace gateway.Pages.Admin;

/// <summary>
/// Dedicated endpoint for saving the admin JWT token to session.
/// Called via fetch() from the sidebar on any admin page.
/// </summary>
[IgnoreAntiforgeryToken]
public class SaveTokenModel : PageModel
{
    public IActionResult OnPost([FromBody] SaveTokenRequest? request)
    {
        if (!string.IsNullOrWhiteSpace(request?.Token))
        {
            HttpContext.Session.SetString("AdminJwtToken", request.Token.Trim());
            return new OkResult();
        }
        return BadRequest();
    }
}

public class SaveTokenRequest
{
    public string? Token { get; set; }
}
