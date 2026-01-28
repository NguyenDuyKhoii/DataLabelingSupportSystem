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
    public class LabelRepository : ILabelRepository
    {
        private readonly AppDbContext _context;

        public LabelRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(Label label)
        {
            _context.Labels.Add(label);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int labelId)
        {
            var label = await _context.Labels.FindAsync(labelId);
            if (label != null) 
            {
                _context.Labels.Remove(label);
                await _context.SaveChangesAsync();
            }

        }

        public async Task<Label> GetByIdAsync(int labelId)
        {
            return await _context.Labels.FindAsync(labelId);
        }

        public async Task<List<Label>> GetByProjectIdAsync(int projectId)
        {
            return await _context.Labels
                                 .Where(l => l.ProjectId == projectId)
                                 .ToListAsync();
        }

        public async Task UpdateAsync(Label label)
        {
            
            _context.Labels.Update(label);
            await _context.SaveChangesAsync();
        }
    }
}
