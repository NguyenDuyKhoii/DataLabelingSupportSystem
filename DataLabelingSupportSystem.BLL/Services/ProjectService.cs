using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using DataLabelingSupportSystem.DAL.Interfaces;
using DataLabelingSupportSystem.DAL.Models;
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

        public ProjectService(IProjectRepository repository)
        {
            _repository = repository;
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

        public async Task<List<ProjectViewDto>> GetProjectByManagerIdAsync(int managerId)
        {
            var projects = await _repository.GetByManagerIdAsync(managerId);

            // Map từ Entity sang DTO
            return projects.Select(p => new ProjectViewDto
            {
                Id = p.ProjectId,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status.ToString(),
                CreatedDate = p.CreatedDate
            }).ToList();
        }
    }
}
