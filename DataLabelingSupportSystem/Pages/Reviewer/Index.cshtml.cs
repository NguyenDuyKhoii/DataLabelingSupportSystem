using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataLabelingSupportSystem.UI.Pages.Reviewer
{
    [Authorize(Roles = "Reviewer")]
    public class IndexModel : PageModel
    {
        public void OnGet() { }
    }
}
