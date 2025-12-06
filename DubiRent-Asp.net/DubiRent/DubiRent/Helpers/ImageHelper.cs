using Microsoft.AspNetCore.Hosting;

namespace DubiRent.Helpers
{
    public static class ImageHelper
    {
        /// <summary>
        /// Generates WebP path from original image path
        /// </summary>
        public static string GetWebpPath(string originalPath)
        {
            if (string.IsNullOrEmpty(originalPath))
                return originalPath;

            // If already WebP, return as is
            if (originalPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                return originalPath;

            // If external URL, return as is (no WebP conversion for external images)
            if (originalPath.StartsWith("http://") || originalPath.StartsWith("https://"))
                return originalPath;

            // Replace extension with .webp
            var extension = System.IO.Path.GetExtension(originalPath);
            if (string.IsNullOrEmpty(extension))
                return originalPath;

            return originalPath.Substring(0, originalPath.Length - extension.Length) + ".webp";
        }

        /// <summary>
        /// Checks if WebP file exists
        /// </summary>
        public static bool WebpExists(string webpPath, IWebHostEnvironment webHostEnvironment)
        {
            if (string.IsNullOrEmpty(webpPath) || webpPath.StartsWith("http"))
                return false;

            try
            {
                var physicalPath = webpPath.TrimStart('/');
                var fullPath = System.IO.Path.Combine(webHostEnvironment.WebRootPath, physicalPath);
                return System.IO.File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }
    }
}

