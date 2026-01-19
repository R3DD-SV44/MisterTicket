namespace MisterTicket.Server.Services
{
    public interface IFileService
    {
        Task<string> SaveImageAsync(IFormFile file);
        void DeleteImage(string imageUrl);
    }
}
