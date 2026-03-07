using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using DataLabelingSupportSystem.DAL.Interfaces;
using DataLabelingSupportSystem.DAL.Models;

namespace DataLabelingSupportSystem.BLL.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;

        public TaskService(ITaskRepository taskRepository)
        {
            _taskRepository = taskRepository;
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
                ImageCount = t.TaskItems.Count
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
                Status = task.Status,
                Items = task.TaskItems.Select(ti =>
                {
                    // Find latest submission (excluding those with sentinel date if we only want "real" submissions)
                    // But for Annotator UI, they want to see if it's "Rejected" or "Approved" based on latest submission.
                    var latestSub = ti.Submissions
                                    .OrderByDescending(s => s.SubmittedAt)
                                    .FirstOrDefault();

                    return new DataItemDto
                    {
                        TaskItemId = ti.TaskItemId,
                        DataItemId = ti.DataItemId,
                        ImagePath = ti.DataItem.ImagePath,
                        LatestStatus = latestSub?.Status,
                        ReviewComment = latestSub?.Review?.Comment
                    };
                }).ToList()
            };
        }
    }
}
