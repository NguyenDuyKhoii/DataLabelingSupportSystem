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
    public class DataItemRepository : IDataItemRepository
    {
        private readonly AppDbContext _context;
        public DataItemRepository(AppDbContext context) { _context = context; }
        public async  Task AddRangeAsync(List<DataItem> items)
        {
            await _context.DataItems.AddRangeAsync(items);
            await _context.SaveChangesAsync();
        }

        public async  Task<List<DataItem>> GetByProjectIdAsync(int projectId)
        {
            return await _context.DataItems
                             .Where(d => d.ProjectId == projectId)
                             .ToListAsync();
        }
    }
}
