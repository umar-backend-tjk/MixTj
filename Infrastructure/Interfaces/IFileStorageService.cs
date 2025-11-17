using Microsoft.AspNetCore.Http;

namespace Infrastructure.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string relativePath);
    Task DeleteFileAsync(string relativePath);
}