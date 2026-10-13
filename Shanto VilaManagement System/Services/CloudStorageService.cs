using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shanto_VilaManagement_System.Helpers;

namespace Shanto_VilaManagement_System.Services
{
    public class CloudStorageService : ICloudStorageService
    {
        private readonly Cloudinary? _cloudinary;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CloudStorageService> _logger;

        public bool IsConfigured => _cloudinary != null;

        public CloudStorageService(IConfiguration config, IWebHostEnvironment env, ILogger<CloudStorageService> logger)
        {
            _env = env;
            _logger = logger;

            // 1. Try CLOUDINARY_URL environment variable (cloudinary://apiKey:apiSecret@cloudName)
            var cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL") ?? config["CLOUDINARY_URL"];
            if (!string.IsNullOrWhiteSpace(cloudinaryUrl))
            {
                try
                {
                    _cloudinary = new Cloudinary(cloudinaryUrl.Trim());
                    _cloudinary.Api.Secure = true;
                    _logger.LogInformation("Cloudinary initialized successfully via CLOUDINARY_URL.");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize Cloudinary via CLOUDINARY_URL.");
                }
            }

            // 2. Try individual parameters from appsettings.json or environment variables
            var cloudName = config["Cloudinary:CloudName"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME");
            var apiKey = config["Cloudinary:ApiKey"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY");
            var apiSecret = config["Cloudinary:ApiSecret"] ?? Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET");

            if (!string.IsNullOrWhiteSpace(cloudName) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiSecret))
            {
                try
                {
                    var account = new Account(cloudName.Trim(), apiKey.Trim(), apiSecret.Trim());
                    _cloudinary = new Cloudinary(account);
                    _cloudinary.Api.Secure = true;
                    _logger.LogInformation("Cloudinary initialized successfully with CloudName: {CloudName}", cloudName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize Cloudinary with Account credentials.");
                }
            }
        }

        public async Task<string?> SaveFileAsync(IFormFile? file, string subFolder)
        {
            if (file == null || file.Length == 0) return null;

            if (_cloudinary != null)
            {
                try
                {
                    var uniqueId = $"{Guid.NewGuid():N}";
                    using var stream = file.OpenReadStream();

                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = $"shanto_vila/{subFolder}",
                        PublicId = uniqueId,
                        Overwrite = true
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    if (uploadResult.Error != null)
                    {
                        _logger.LogError("Cloudinary upload failed: {Error}. Falling back to local storage.", uploadResult.Error.Message);
                        return await FileHelper.SaveFileAsync(file, subFolder, _env);
                    }

                    return uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception during Cloudinary upload. Falling back to local storage.");
                    return await FileHelper.SaveFileAsync(file, subFolder, _env);
                }
            }

            // Fallback to local storage if Cloudinary is not configured
            return await FileHelper.SaveFileAsync(file, subFolder, _env);
        }

        public async Task DeleteFileAsync(string? fileUrlOrPath)
        {
            if (string.IsNullOrWhiteSpace(fileUrlOrPath)) return;

            // If it's a Cloudinary URL
            if (fileUrlOrPath.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase))
            {
                if (_cloudinary != null)
                {
                    try
                    {
                        var publicId = ExtractPublicIdFromCloudinaryUrl(fileUrlOrPath);
                        if (!string.IsNullOrEmpty(publicId))
                        {
                            var delParams = new DeletionParams(publicId);
                            var result = await _cloudinary.DestroyAsync(delParams);
                            _logger.LogInformation("Cloudinary deletion for {PublicId}: {Result}", publicId, result.Result);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to delete image from Cloudinary: {Url}", fileUrlOrPath);
                    }
                }
                return;
            }

            // Local file deletion
            FileHelper.DeleteFile(fileUrlOrPath, _env);
        }

        private static string? ExtractPublicIdFromCloudinaryUrl(string url)
        {
            // Example: https://res.cloudinary.com/cloudname/image/upload/v1234567/shanto_vila/residents/photos/abc123.jpg
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var uploadIndex = path.IndexOf("/upload/", StringComparison.OrdinalIgnoreCase);
                if (uploadIndex == -1) return null;

                var afterUpload = path.Substring(uploadIndex + "/upload/".Length);
                var segments = afterUpload.Split('/');
                int startIndex = (segments.Length > 0 && segments[0].StartsWith('v') && long.TryParse(segments[0].Substring(1), out _)) ? 1 : 0;

                var publicIdWithExt = string.Join('/', segments.Skip(startIndex));
                var dotIndex = publicIdWithExt.LastIndexOf('.');
                return dotIndex > 0 ? publicIdWithExt.Substring(0, dotIndex) : publicIdWithExt;
            }
            catch
            {
                return null;
            }
        }
    }
}
