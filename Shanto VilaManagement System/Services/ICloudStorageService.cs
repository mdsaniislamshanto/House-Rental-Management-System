using Microsoft.AspNetCore.Http;

namespace Shanto_VilaManagement_System.Services
{
    public interface ICloudStorageService
    {
        bool IsConfigured { get; }
        Task<string?> SaveFileAsync(IFormFile? file, string subFolder);
        Task DeleteFileAsync(string? fileUrlOrPath);
    }
}
