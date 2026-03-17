using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly IAnnotationService _annotationService;

        public DetailsModel(IProjectService projectService, ILabelService labelService, IDataItemService dataItemService, ITaskService taskService, IUserService userService, IAnnotationService annotationService)
        {
            _projectService = projectService;
            _labelService = labelService;
            _dataItemService = dataItemService;
            _taskService = taskService;
            _userService = userService;
            _annotationService = annotationService;
        }

        public ProjectViewDto Project { get; set; } = null!;
        public List<LabelViewDto> Labels { get; set; } = new();
        public List<string> ImageUrls { get; set; } = new();
        public List<DataItemDto> ProjectDataItems { get; set; } = new();
        public List<TaskViewDto> Tasks { get; set; } = new();
        public List<DataLabelingSupportSystem.DTOs.UserDto> Annotators { get; set; } = new();
        public ProjectStatsDto? Stats { get; set; }

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
            Annotators = await _userService.GetUsersAsync(roleId: 3);
            Stats = await _projectService.GetProjectStatsAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAddLabelAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(id);
                return Page();
            }

            NewLabel.Name = (NewLabel.Name ?? "").Trim();

            if (string.IsNullOrWhiteSpace(NewLabel.Name))
            {
                ModelState.AddModelError(string.Empty, "Label name is required.");
                await OnGetAsync(id);
                return Page();
            }

            var existing = await _labelService.GetLabelsByProjectIdAsync(id);
            var dup = existing.Any(l => string.Equals(l.Name?.Trim(), NewLabel.Name, StringComparison.OrdinalIgnoreCase));
            if (dup)
            {
                ModelState.AddModelError(string.Empty, $"Label '{NewLabel.Name}' already exists in this project.");
                await OnGetAsync(id);
                return Page();
            }

            try
            {
                await _labelService.AddLabelAsync(NewLabel, id);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Cannot add label. It may already exist. ({ex.Message})");
                await OnGetAsync(id);
                return Page();
            }

            return RedirectToPage("Details", new { id });
        }

        public async Task<IActionResult> OnPostUpdateLabelAsync(int projectId, int labelId, string labelName, string labelColor)
        {
            var updateDto = new CreateLabelDto
            {
                Name = labelName,
                Color = labelColor
            };

            await _labelService.UpdateLabelAsync(labelId, updateDto);
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

        public async Task<IActionResult> OnGetExportYoloAsync(int id)
        {
            var export = await _annotationService.GetProjectYoloExportAsync(id);
            if (export == null || !export.Items.Any())
            {
                TempData["ErrorMessage"] = "No approved data to export.";
                return RedirectToPage("Details", new { id });
            }

            // Shuffle and Split: 80% Train, 20% Val
            var rnd = new Random();
            var shuffledItems = export.Items.OrderBy(x => rnd.Next()).ToList();
            int valCount = (int)Math.Max(1, shuffledItems.Count * 0.2);
            if (shuffledItems.Count < 2) valCount = 0; // If only 1 item, put in train

            var valItems = shuffledItems.Take(valCount).ToList();
            var trainItems = shuffledItems.Skip(valCount).ToList();

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    // 1. Create data.yaml
                    var yamlFile = archive.CreateEntry("data.yaml");
                    using (var entryStream = yamlFile.Open())
                    using (var streamWriter = new StreamWriter(entryStream))
                    {
                        await streamWriter.WriteLineAsync("path: ./");
                        await streamWriter.WriteLineAsync("train: images/train");
                        await streamWriter.WriteLineAsync("val: images/val");
                        await streamWriter.WriteLineAsync("");
                        await streamWriter.WriteLineAsync($"nc: {export.LabelNamesByOrder.Count}");
                        await streamWriter.WriteLineAsync("names:");
                        for (int i = 0; i < export.LabelNamesByOrder.Count; i++)
                        {
                            await streamWriter.WriteLineAsync($"  {i}: {export.LabelNamesByOrder[i]}");
                        }
                    }

                    using (var httpClient = new HttpClient())
                    {
                        // Helper to add items to subfolder
                        async Task AddItemsToArchive(List<YoloItemDto> items, string splitName)
                        {
                            foreach (var item in items)
                            {
                                var imgExt = Path.GetExtension(item.ImagePath) ?? ".jpg";
                                var baseName = Path.GetFileNameWithoutExtension(item.FileName);
                                var imgFileName = baseName + imgExt;

                                // A) Add Label
                                var labelEntry = archive.CreateEntry($"labels/{splitName}/{item.FileName}");
                                using (var entryStream = labelEntry.Open())
                                using (var streamWriter = new StreamWriter(entryStream))
                                {
                                    foreach (var line in item.YoloLines)
                                    {
                                        await streamWriter.WriteLineAsync(line);
                                    }
                                }

                                // B) Add Image
                                try
                                {
                                    var imgData = await httpClient.GetByteArrayAsync(item.ImagePath);
                                    var imgEntry = archive.CreateEntry($"images/{splitName}/{imgFileName}");
                                    using (var entryStream = imgEntry.Open())
                                    {
                                        await entryStream.WriteAsync(imgData, 0, imgData.Length);
                                    }
                                }
                                catch { /* Skip if image download fails */ }
                            }
                        }

                        // 2. Process Train
                        await AddItemsToArchive(trainItems, "train");

                        // 3. Process Val
                        await AddItemsToArchive(valItems, "val");
                    }
                }

                return File(memoryStream.ToArray(), "application/zip", $"YOLO_Dataset_{export.ProjectName}_{DateTime.Now:yyyyMMdd_HHmm}.zip");
            }
        }
    }
}