namespace Shanto_VilaManagement_System.Helpers
{
    public static class FileHelper
    {
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".pdf"];

        public static async Task<string?> SaveFileAsync(IFormFile? file, string subFolder, IWebHostEnvironment env)
        {
            if (file == null || file.Length == 0)
                return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                throw new InvalidOperationException("শুধুমাত্র JPG, JPEG, PNG, WEBP অথবা PDF ফাইল আপলোড করা যাবে।");
            }

            // Path to wwwroot/uploads/...
            var uploadsRoot = Path.Combine(env.WebRootPath, "uploads", subFolder);
            if (!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(uploadsRoot);
            }

            var uniqueFileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/{subFolder}/{uniqueFileName}";
        }

        public static void DeleteFile(string? relativePath, IWebHostEnvironment env)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            try
            {
                var cleanPath = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(env.WebRootPath, cleanPath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch
            {
                // Silently ignore if file deletion fails
            }
        }
    }
}
