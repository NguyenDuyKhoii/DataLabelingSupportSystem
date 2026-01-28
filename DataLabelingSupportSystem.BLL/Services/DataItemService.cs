using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DataLabelingSupportSystem.BLL.Interface;
using DataLabelingSupportSystem.DAL.Interfaces;
using DataLabelingSupportSystem.DAL.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLabelingSupportSystem.BLL.Services
{
    public class DataItemService : IDataItemService
    {
        private readonly IDataItemRepository _repo;
        private readonly Cloudinary _cloudinary;

        public DataItemService(IDataItemRepository repo, IConfiguration config)
        {
            _repo = repo;

          
            var cloudName = config["Cloudinary:CloudName"];
            var apiKey = config["Cloudinary:ApiKey"];
            var apiSecret = config["Cloudinary:ApiSecret"];

           
            Account account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }


        public async Task<List<string>> GetImagesByProjectIdAsync(int projectId)
        {
            var items = await _repo.GetByProjectIdAsync(projectId);

           
            return items.Select(x => x.ImagePath).ToList();
        }

        public async  Task UploadImagesAsync(List<IFormFile> files, int projectId)
        {
            var dataItems = new List<DataItem>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    // 1. Upload lên Cloudinary
                    using var stream = file.OpenReadStream();
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = "DataLabeling_Project_" + projectId 
                    };
                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    string ImagePath = uploadResult.SecureUrl.ToString();

                    
                    dataItems.Add(new DataItem
                    {
                        ImagePath = ImagePath,
                        ProjectId = projectId
                    });
                }
            }

            
            if (dataItems.Count > 0)
            {
                await _repo.AddRangeAsync(dataItems);
            }
        }
    }
}
