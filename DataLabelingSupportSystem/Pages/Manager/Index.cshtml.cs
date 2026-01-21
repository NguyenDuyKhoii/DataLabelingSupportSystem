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

        // Danh sách dự án để hiển thị ra màn hình
        public List<ProjectViewDto> Projects { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Lấy ID Manager từ cookie
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Account/Login");

            int managerId = int.Parse(userIdStr);

            // 2. Gọi Service lấy danh sách
            Projects = await _projectService.GetProjectByManagerIdAsync(managerId);

            return Page();
        }
    }
}
