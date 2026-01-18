using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataLabelingSupportSystem.UI.Pages.Annotator
{
    [Authorize(Roles = "Annotator")]
    public class IndexModel : PageModel
    {
        public void OnGet() { }
    }
}
