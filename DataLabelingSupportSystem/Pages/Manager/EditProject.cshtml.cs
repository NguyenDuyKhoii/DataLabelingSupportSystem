using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataLabelingSupportSystem.UI.Pages.Manager
{
    [Authorize(Roles = "Manager")]
    public class EditProjectModel : PageModel
    {
        private readonly IProjectService _projectService;

        public EditProjectModel(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [BindProperty]
        public UpdateProjectDto Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var p = await _projectService.GetProjectByIdAsync(id);
            if (p == null) return NotFound();

            // Đổ dữ liệu cũ vào Form
            Input = new UpdateProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            await _projectService.UpdateProjectAsync(Input);

            return RedirectToPage("/Manager/Index");
        }
    }
}