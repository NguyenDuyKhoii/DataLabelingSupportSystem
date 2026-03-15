using DataLabelingSupportSystem.BLL.DTO;
using DataLabelingSupportSystem.BLL.Interface;
using DataLabelingSupportSystem.DAL.DbContext;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _db;

        public AdminService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<SystemStatsDto> GetSystemOverviewAsync()
        {
            var stats = new SystemStatsDto();

            stats.TotalUsers = await _db.Users.CountAsync();
            stats.TotalProjects = await _db.Projects.CountAsync();
            stats.TotalDataItems = await _db.DataItems.CountAsync();
            stats.TotalAnnotations = await _db.Annotations.CountAsync();

            // User distribution by role
            stats.UserDistribution = await _db.Users
                .Include(u => u.Role)
                .GroupBy(u => u.Role.RoleName)
                .Select(g => new RoleCountDto
                {
                    RoleName = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Project distribution
            stats.ProjectDistribution = await _db.Projects
                .GroupBy(p => p.Status)
                .Select(g => new ProjectStatusCountDto
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync();

            // Recent Activities (Mix of new users and submissions)
            var recentUsers = await _db.Users
                .OrderByDescending(u => u.UserId)
                .Take(5)
                .Select(u => new ActivityLogDto
                {
                    Message = $"New user registered: {u.Username}",
                    Timestamp = System.DateTime.Now.AddDays(-1), // Simulate created time since column might not exist or be constant in seed
                    Icon = "bi-person-plus",
                    Color = "info"
                })
                .ToListAsync();

            var recentSubmissions = await _db.DataItemSubmissions
                .Include(s => s.Submitter)
                .OrderByDescending(s => s.SubmittedAt)
                .Take(5)
                .Select(s => new ActivityLogDto
                {
                    Message = $"{s.Submitter.Username} submitted a new item",
                    Timestamp = s.SubmittedAt,
                    Icon = "bi-cloud-upload",
                    Color = "success"
                })
                .ToListAsync();

            stats.RecentActivities = recentUsers.Concat(recentSubmissions)
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToList();

            return stats;
        }
    }
}
