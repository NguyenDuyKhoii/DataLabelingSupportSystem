using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using DataLabelingSupportSystem.DAL.DbContext;
using DataLabelingSupportSystem.DAL.Interfaces;
using DataLabelingSupportSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DataLabelingSupportSystem.DAL.Models.Enums;

namespace DataLabelingSupportSystem.BLL.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly AppDbContext _context;

        public TaskService(ITaskRepository taskRepository, AppDbContext context)
        {
            _taskRepository = taskRepository;
            _context = context;
        }
        

        public async Task<TaskViewDto> CreateTaskAsync(CreateTaskDto dto)
        {
            var taskEntity = new TaskEntity
            {
                ProjectId = dto.ProjectId,
                AnnotatorId = dto.AnnotatorId,
                Status = Enums.TaskStatus.Assigned,
                TaskItems = dto.DataItemIds.Select(dataItemId => new TaskItem
                {
                    DataItemId = dataItemId
                }).ToList()
            };

            var createdTask = await _taskRepository.CreateTaskAsync(taskEntity);
            
            // Reload task to get Project and Annotator info for ViewDto
            var loadedTask = await _taskRepository.GetTaskByIdAsync(createdTask.TaskId);

            return new TaskViewDto
            {
                TaskId = loadedTask!.TaskId,
                ProjectId = loadedTask.ProjectId,
                ProjectName = loadedTask.Project.Name,
                AnnotatorId = loadedTask.AnnotatorId,
                AnnotatorName = loadedTask.Annotator.Name,
                Status = loadedTask.Status,
                ImageCount = loadedTask.TaskItems.Count
            };
        }

        public async Task<List<TaskViewDto>> GetTasksByProjectIdAsync(int projectId)
        {
            var tasks = await _taskRepository.GetTasksByProjectIdAsync(projectId);

            return tasks.Select(t => new TaskViewDto
            {
                TaskId = t.TaskId,
                ProjectId = t.ProjectId,
                ProjectName = t.Project.Name,
                AnnotatorId = t.AnnotatorId,
                AnnotatorName = t.Annotator.Name,
                Status = t.Status,
                ImageCount = t.TaskItems.Count,



                SubmittedCount = t.TaskItems.Count(ti => ti.Submissions.Any()),

                
                ApprovedCount = t.TaskItems.Count(ti => ti.Submissions.Any(s => s.Status == Enums.SubmissionStatus.Approved)),
                RejectedCount = t.TaskItems.Count(ti => ti.Submissions.Any(s => s.Status == Enums.SubmissionStatus.Rejected))
            }).ToList();
        }

        public async Task<List<TaskViewDto>> GetTasksByAnnotatorIdAsync(int annotatorId)
        {
            var tasks = await _taskRepository.GetTasksByAnnotatorIdAsync(annotatorId);

            return tasks.Select(t => new TaskViewDto
            {
                TaskId = t.TaskId,
                ProjectId = t.ProjectId,
                ProjectName = t.Project.Name,
                AnnotatorId = t.AnnotatorId,
                AnnotatorName = "", // Annotator doesn't need to see their own name
                Status = t.Status,
                ImageCount = t.TaskItems.Count
            }).ToList();
        }

        public async Task<TaskDetailDto?> GetTaskDetailsByIdAsync(int taskId)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId);
            if (task == null) return null;

            return new TaskDetailDto
            {
                TaskId = task.TaskId,
                ProjectName = task.Project.Name,
                AnnotatorName = task.Annotator.Name ?? task.Annotator.Username,
                Status = task.Status,
                Items = task.TaskItems.Select(ti =>
                {
                    var latestSub = ti.Submissions
                                    .Where(s => s.SubmittedAt.Year != 1900) // Exclude drafts if needed, or include them? Manager usually wants real submissions.
                                    .OrderByDescending(s => s.DataItemSubmissionId)
                                    .FirstOrDefault();

                    return new DataItemDto
                    {
                        TaskItemId = ti.TaskItemId,
                        DataItemId = ti.DataItemId,
                        ImagePath = ti.DataItem.ImagePath,
                        LatestStatus = latestSub?.Status,
                        LatestSubmissionId = latestSub?.DataItemSubmissionId,
                        ReviewComment = latestSub?.Review?.Comment
                    };
                }).ToList()
            };
        }


        public async Task<List<TaskProgressDTO>> GetTaskProgressesByProjectAsync(int projectId)
        {
            return await _context.Tasks
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId)
                .Select(t => new TaskProgressDTO
                {
                    TaskId = t.TaskId,
                    AnnotatorName = t.Annotator.Username,
                    TotalItems = t.TaskItems.Count,

                   
                    SubmittedCount = t.TaskItems.Count(ti => ti.Submissions.Any()),

                    // Approved: Lần nộp gần nhất có status là Approved
                    ApprovedCount = t.TaskItems.Count(ti =>
                        ti.Submissions.OrderByDescending(s => s.SubmittedAt)
                                      .FirstOrDefault().Status == SubmissionStatus.Approved),

                    // Rejected: Lần nộp gần nhất có status là Rejected
                    RejectedCount = t.TaskItems.Count(ti =>
                        ti.Submissions.OrderByDescending(s => s.SubmittedAt)
                                      .FirstOrDefault().Status == SubmissionStatus.Rejected)
                }).ToListAsync();
        }
    }
}
