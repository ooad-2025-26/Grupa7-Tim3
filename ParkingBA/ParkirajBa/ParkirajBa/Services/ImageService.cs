namespace ParkirajBa.Services
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class ImageService
    {
        private readonly IWebHostEnvironment _env;

        public ImageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveImageToServerAsync(IFormFile Image, string TargetFolder, string name)
        {
            if (Image == null || Image.Length == 0)
                return null;

            string absoluteFolderPath = Path.Combine(_env.WebRootPath, TargetFolder);

            if (!Directory.Exists(absoluteFolderPath))
            {
                Directory.CreateDirectory(absoluteFolderPath);
            }

            string sanitizedName = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
            sanitizedName = sanitizedName.Replace(" ", "-").ToLower();

            string extension = Path.GetExtension(Image.FileName);
            string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
            string newFileName = $"{sanitizedName}_{uniqueId}{extension}";

            string physicalPath = Path.Combine(absoluteFolderPath, newFileName);

            using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await Image.CopyToAsync(stream);
            }

            return Path.Combine("/", TargetFolder, newFileName).Replace("\\", "/");
        }
    }
}
