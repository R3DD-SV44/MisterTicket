using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace MisterTicket.Server.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveImageAsync(IFormFile file)
    {
        var folderPath = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(folderPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return "/uploads/" + fileName;
    }

    public void DeleteImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return;

        var path = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}