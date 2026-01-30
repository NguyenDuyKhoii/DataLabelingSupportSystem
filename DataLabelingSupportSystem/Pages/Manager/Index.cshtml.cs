using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace DataLabelingSupportSystem.UI.Pages.Manager
{
    [Authorize(Roles = "Manager")]
    public class IndexModel : PageModel
    {
        private readonly IProjectService _projectService;

        public IndexModel(IProjectService projectService)
        {
            _projectService = projectService;
        }

        
        public List<ProjectViewDto> Projects { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Account/Login");

            int managerId = int.Parse(userIdStr);

            
            Projects = await _projectService.GetProjectByManagerIdAsync(managerId);

            return Page();
        }
    }
}
