using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

public class CloudinaryFileUploadService : IFileUploadService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryFileUploadService(IOptions<CloudinarySettings> config)
    {
        var acc = new Account(
            config.Value.CloudName,
            config.Value.ApiKey,
            config.Value.ApiSecret
        );
        _cloudinary = new Cloudinary(acc);
    }

    public async Task<string> UploadFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return null;

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "your_project_folder", // optional: Cloudinary ichida papka nomi
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
            return uploadResult.SecureUrl.ToString();

        return null;
    }

    public async Task DeleteFileAsync(string publicIdOrUrl)
    {
        if (string.IsNullOrEmpty(publicIdOrUrl))
            return;

        // Agar public_id emas, to‘liq URL bo‘lsa, uni ajratib olish kerak
        string publicId = ExtractPublicId(publicIdOrUrl);

        var deletionParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deletionParams);
    }

    // Cloudinary public_id olish (URL dan)
    private string ExtractPublicId(string urlOrPublicId)
    {
        // Agar bu URL bo‘lsa, oxirgi "/" dan keyingi qismni va kengaytmani olib tashla
        if (urlOrPublicId.StartsWith("http"))
        {
            var uri = new Uri(urlOrPublicId);
            var parts = uri.AbsolutePath.Split('/');
            var fileName = parts.Last();
            var dotIdx = fileName.LastIndexOf(".");
            if (dotIdx > 0)
                fileName = fileName.Substring(0, dotIdx);

            // papka bo‘lsa: uploads/filename → uploads/filename
            if (parts.Length > 2)
                return string.Join("/", parts.Skip(parts.Length - 2)).Replace(Path.GetExtension(fileName), "");
            return fileName;
        }
        return urlOrPublicId;
    }
}