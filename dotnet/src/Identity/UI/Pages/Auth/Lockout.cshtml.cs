using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AQ.Identity.UI.Pages.Auth;

public class LockoutModel : PageModel
{
    public DateTimeOffset? LockoutEnd { get; set; }

    public void OnGet(long? until)
    {
        if (until.HasValue)
        {
            LockoutEnd = new DateTimeOffset(until.Value, TimeSpan.Zero);
        }
    }
}
