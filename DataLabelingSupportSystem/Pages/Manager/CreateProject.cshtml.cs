using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace DataLabelingSupportSystem.UI.Pages.Manager
{
    [Authorize(Roles = "Manager")]
    public class CreateProjectModel : PageModel
    {
        private readonly IProjectService _projectService;

        public CreateProjectModel(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [BindProperty]
        public CreateProjectDto Input { get; set; } = new();

        public void OnGet()
        {
            
        }

        public async Task<IActionResult> OnPostAsync()
        {
           
            if (!ModelState.IsValid)
            {
                return Page();
            }

           
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdStr))
            {
                
                return RedirectToPage("/Account/Login");
            }

            int managerId = int.Parse(userIdStr);

            
            await _projectService.CreateProjectAsync(Input, managerId);

            
            return RedirectToPage("/Manager/Index");
        }
    }
}
