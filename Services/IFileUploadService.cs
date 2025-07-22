public interface IFileUploadService
{
    Task<string> UploadFileAsync(IFormFile file);
    Task DeleteFileAsync(string publicIdOrUrl);
}