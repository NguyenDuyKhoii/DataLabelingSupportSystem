using DataLabelingSupportSystem.DAL.DbContext;
using DataLabelingSupportSystem.DAL.Interfaces;
using DataLabelingSupportSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.DAL.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly AppDbContext _context;

        public ProjectRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Project project)
        {
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Project>> GetByManagerIdAsync(int managerId)
        {
           return await _context.Projects
                .Where(p =>p.ManagerId == managerId)
                .OrderByDescending(p => p.ProjectId)
                .ToListAsync();
        }
    }
}
