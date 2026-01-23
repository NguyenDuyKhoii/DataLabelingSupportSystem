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

namespace DataLabelingSupportSystem.BLL.Services
{
    public class LabelService : ILabelService
    {
        private readonly ILabelRepository _repo;
        public LabelService (ILabelRepository repo)
        {
            _repo = repo;
        }
        public async Task AddLabelAsync(CreateLabelDto dto, int ProjectId)
        {
            var label = new Label
            {
                Name = dto.Name,
                Color = dto.Color,
                ProjectId = ProjectId 
            };

            await _repo.AddAsync(label);
        }

        public async Task DeleteLabelAsync(int labelId)
        {
            await _repo.DeleteAsync(labelId);
        }

        public async Task<List<LabelViewDto>> GetLabelsByProjectIdAsync(int projectId)
        {
            var labels = await _repo.GetByProjectIdAsync(projectId);
            return labels.Select(l => new LabelViewDto
            {
                Id = l.LabelId,
                Name = l.Name,
                Color = l.Color,
                ProjectId = l.ProjectId
            }).ToList();
        }

        public async Task UpdateLabelAsync(int labelId, CreateLabelDto labelDto)
        {
            var label = await _repo.GetByIdAsync(labelId);

            if (label == null)
            {
                throw new Exception($"Không tìm thấy label với ID {labelId}");
            }

            label.Name = labelDto.Name;
            label.Color = labelDto.Color;
            //label.UpdatedDate = DateTime.Now;
            await _repo.UpdateAsync(label);
        }
    }
}
