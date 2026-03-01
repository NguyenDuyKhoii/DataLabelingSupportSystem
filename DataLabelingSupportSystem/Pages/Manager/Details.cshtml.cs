using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using DataLabelingSupportSystem.BLL.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DataLabelingSupportSystem.UI.Pages.Manager
{
    public class DetailsModel : PageModel
    {
        private readonly IProjectService _projectService;
        private readonly ILabelService _labelService;
        private readonly IDataItemService _dataItemService;
        private readonly ITaskService _taskService;
        private readonly IUserService _userService;

        public DetailsModel(IProjectService projectService, ILabelService labelService, IDataItemService dataItemService, ITaskService taskService, IUserService userService)
        {
            _projectService = projectService;
            _labelService = labelService;
            _dataItemService = dataItemService;
            _taskService = taskService;
            _userService = userService;
        }

        public ProjectViewDto Project { get; set; } = null!;
        public List<LabelViewDto> Labels { get; set; } = new();
        public List<string> ImageUrls { get; set; } = new();
        public List<DataItemDto> ProjectDataItems { get; set; } = new();
        public List<TaskViewDto> Tasks { get; set; } = new();
        public List<DataLabelingSupportSystem.DTOs.UserDto> Annotators { get; set; } = new();

        [BindProperty]
        public CreateLabelDto NewLabel { get; set; } = new();

        [BindProperty]
        public List<IFormFile> UploadFiles { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var p = await _projectService.GetProjectByIdAsync(id);
            if (p == null) return NotFound();
            Project = p;

            Labels = await _labelService.GetLabelsByProjectIdAsync(id);
            ImageUrls = await _dataItemService.GetImagesByProjectIdAsync(id);
            ProjectDataItems = await _dataItemService.GetDataItemsByProjectIdAsync(id);
            Tasks = await _taskService.GetTasksByProjectIdAsync(id);
            Annotators = await _userService.GetUsersAsync(roleId: 3); // 3 is Annotator using the seeded RoleId
            return Page();
        }

        public async Task<IActionResult> OnPostAddLabelAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(id);
                return Page();
            }

            await _labelService.AddLabelAsync(NewLabel, id);
            return RedirectToPage("Details", new { id = id });
        }

        // ✅ THÊM MỚI: Handler UPDATE Label
        public async Task<IActionResult> OnPostUpdateLabelAsync(int projectId, int labelId, string labelName, string labelColor)
        {
            // Tạo DTO để update
            var updateDto = new CreateLabelDto
            {
                Name = labelName,
                Color = labelColor
            };

            // Gọi service để update label
            await _labelService.UpdateLabelAsync(labelId, updateDto);

            // Redirect về trang Details
            return RedirectToPage("Details", new { id = projectId });
        }

        public async Task<IActionResult> OnPostDeleteLabelAsync(int labelId, int projectId)
        {
            await _labelService.DeleteLabelAsync(labelId);
            return RedirectToPage("Details", new { id = projectId });
        }

        public async Task<IActionResult> OnPostUploadImagesAsync(int id)
        {
            if (UploadFiles != null && UploadFiles.Count > 0)
            {
                await _dataItemService.UploadImagesAsync(UploadFiles, id);
            }
            return RedirectToPage("Details", new { id = id });
        }

        public async Task<IActionResult> OnPostCreateTaskAsync(int projectId, int annotatorId, List<int> dataItemIds)
        {
            if (dataItemIds == null || !dataItemIds.Any() || annotatorId == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select an Annotator and at least one Image.");
                return await OnGetAsync(projectId);
            }

            var dto = new CreateTaskDto
            {
                ProjectId = projectId,
                AnnotatorId = annotatorId,
                DataItemIds = dataItemIds
            };

            await _taskService.CreateTaskAsync(dto);
            return RedirectToPage("Details", new { id = projectId });
        }
    }
}