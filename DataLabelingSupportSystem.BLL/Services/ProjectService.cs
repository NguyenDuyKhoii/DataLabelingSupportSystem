using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using DataLabelingSupportSystem.DAL.Interfaces;
using DataLabelingSupportSystem.DAL.Models;
using DataLabelingSupportSystem.DAL.DbContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataLabelingSupportSystem.DAL.Models.Enums;

namespace DataLabelingSupportSystem.BLL.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _repository;
        private readonly AppDbContext _db;

        public ProjectService(IProjectRepository repository, AppDbContext db)
        {
            _repository = repository;
            _db = db;
        }
        public async Task CreateProjectAsync(CreateProjectDto dto, int managerId)
        {
            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                ManagerId = managerId,
                CreatedDate = DateTime.Now,
                Status = ProjectStatus.New
            };
            await _repository.AddAsync(project);
            
        }

        public async Task<ProjectViewDto?> GetProjectByIdAsync(int projectId)
        {
            var p = await _repository.GetByIdAsync(projectId);
            if (p == null) return null;

            return new ProjectViewDto
            {
                Id = p.ProjectId,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status.ToString(),
                CreatedDate = p.CreatedDate
            };
        }

        public async Task<List<ProjectViewDto>> GetProjectByManagerIdAsync(int managerId)
        {
            var projects = await _repository.GetByManagerIdAsync(managerId);

            return projects.Select(p => new ProjectViewDto
            {
                Id = p.ProjectId,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status.ToString(),
                CreatedDate = p.CreatedDate
            }).ToList();
        }

        public async  Task UpdateProjectAsync(UpdateProjectDto dto)
        {
            var project = await _repository.GetByIdAsync(dto.Id);
            if (project != null)
            {
                
                project.Name = dto.Name;
                project.Description = dto.Description;
                //project.Status = dto.Status;
                



                await _repository.UpdateAsync(project);
            }
        }
        public async Task<ProjectStatsDto?> GetProjectStatsAsync(int projectId)
        {
            var project = await _db.Projects
                .Include(p => p.Labels)
                .Include(p => p.DataItems)
                    .ThenInclude(di => di.TaskItems)
                        .ThenInclude(ti => ti.Submissions)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null) return null;

            var stats = new ProjectStatsDto
            {
                ProjectId = project.ProjectId,
                ProjectName = project.Name,
                TotalImages = project.DataItems.Count
            };

            // Image statuses
            int approved = 0, rejected = 0, pending = 0, unassigned = 0;

            foreach (var di in project.DataItems)
            {
                if (!di.TaskItems.Any())
                {
                    unassigned++;
                    continue;
                }

                // Lấy submission mới nhất (loại bỏ sentinel draft)
                var latest = di.TaskItems.SelectMany(ti => ti.Submissions)
                    .Where(s => s.SubmittedAt.Year != 1900)
                    .OrderByDescending(s => s.DataItemSubmissionId)
                    .FirstOrDefault();

                if (latest == null) unassigned++; // Đã gán task nhưng chưa làm/save nháp? Thực tế là "Assigned"
                else if (latest.Status == SubmissionStatus.Approved) approved++;
                else if (latest.Status == SubmissionStatus.Rejected) rejected++;
                else pending++;
            }

            stats.ApprovedImages = approved;
            stats.RejectedImages = rejected;
            stats.PendingImages = pending;
            stats.UnassignedImages = unassigned;

            // Label Distribution (chỉ tính từ các bản Approved)
            stats.LabelDistribution = await _db.Annotations
                .Where(a => a.Submission.Status == SubmissionStatus.Approved && a.Label.ProjectId == projectId)
                .GroupBy(a => new { a.Label.Name, a.Label.Color })
                .Select(g => new LabelCountDto
                {
                    LabelName = g.Key.Name,
                    Color = g.Key.Color,
                    Count = g.Count()
                })
                .ToListAsync();

            // Daily Progress (Approved per day)
            stats.DailyProgress = await _db.DataItemReviews
                .Where(r => r.Decision == ReviewDecision.Approved && r.Submission.TaskItem.DataItem.ProjectId == projectId)
                .GroupBy(r => r.ReviewedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyProgressDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .Take(15) // Top 15 days
                .ToListAsync();

            return stats;
        }
    }
}
