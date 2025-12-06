using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Png;

namespace DubiRent.Services
{
    public interface IImageOptimizationService
    {
        Task<string> OptimizeAndSaveImageAsync(Stream imageStream, string fileName, string outputFolder, int? maxWidth = null, int? maxHeight = null, int quality = 85);
        Task<(string webpPath, string originalPath)> OptimizeAndSaveImageWithWebPAsync(Stream imageStream, string fileName, string outputFolder, int? maxWidth = null, int? maxHeight = null, int quality = 85);
        Task<OptimizedImageResult> OptimizeAndSaveImageWithWebPAndFallbackAsync(Stream imageStream, string fileName, string outputFolder, int? maxWidth = null, int? maxHeight = null, int quality = 85);
    }

    public class OptimizedImageResult
    {
        public string OriginalPath { get; set; }
        public string WebpPath { get; set; }
        public string OriginalFileName { get; set; }
        public string WebpFileName { get; set; }
    }

    public class ImageOptimizationService : IImageOptimizationService
    {
        private readonly ILogger<ImageOptimizationService> _logger;

        public ImageOptimizationService(ILogger<ImageOptimizationService> logger)
        {
            _logger = logger;
        }

        public async Task<OptimizedImageResult> OptimizeAndSaveImageWithWebPAndFallbackAsync(Stream imageStream, string fileName, string outputFolder, int? maxWidth = null, int? maxHeight = null, int quality = 85)
        {
            try
            {
                // Ensure output folder exists
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // Reset stream position
                imageStream.Position = 0;

                // Load image once
                using (var image = await Image.LoadAsync(imageStream))
                {
                    // Resize if needed
                    if (maxWidth.HasValue || maxHeight.HasValue)
                    {
                        var resizeOptions = new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(maxWidth ?? int.MaxValue, maxHeight ?? int.MaxValue)
                        };
                        image.Mutate(x => x.Resize(resizeOptions));
                    }

                    var extension = Path.GetExtension(fileName).ToLowerInvariant();
                    var baseFileName = Path.GetFileNameWithoutExtension(fileName);
                    var originalFileName = fileName;
                    var webpFileName = $"{baseFileName}.webp";

                    // Convert to JPEG if not already JPEG or PNG
                    if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                    {
                        originalFileName = $"{baseFileName}.jpg";
                    }

                    var originalPath = Path.Combine(outputFolder, originalFileName);
                    var webpPath = Path.Combine(outputFolder, webpFileName);

                    // Save original optimized version (JPEG or PNG)
                    using (var outputStream = new FileStream(originalPath, FileMode.Create))
                    {
                        if (extension == ".png")
                        {
                            var pngEncoder = new PngEncoder
                            {
                                CompressionLevel = PngCompressionLevel.BestCompression
                            };
                            await image.SaveAsync(outputStream, pngEncoder);
                        }
                        else
                        {
                            // Default to JPEG
                            var jpegEncoder = new JpegEncoder { Quality = quality };
                            await image.SaveAsync(outputStream, jpegEncoder);
                        }
                    }

                    // Save WebP version
                    imageStream.Position = 0;
                    using (var webpImage = await Image.LoadAsync(imageStream))
                    {
                        if (maxWidth.HasValue || maxHeight.HasValue)
                        {
                            var resizeOptions = new ResizeOptions
                            {
                                Mode = ResizeMode.Max,
                                Size = new Size(maxWidth ?? int.MaxValue, maxHeight ?? int.MaxValue)
                            };
                            webpImage.Mutate(x => x.Resize(resizeOptions));
                        }

                        using (var webpStream = new FileStream(webpPath, FileMode.Create))
                        {
                            var webpEncoder = new WebpEncoder
                            {
                                Quality = quality,
                                Method = WebpEncodingMethod.BestQuality
                            };
                            await webpImage.SaveAsync(webpStream, webpEncoder);
                        }
                    }

                    var originalRelativePath = $"/images/properties/{originalFileName}";
                    var webpRelativePath = $"/images/properties/{webpFileName}";

                    _logger.LogInformation($"Image optimized (original + WebP) and saved: {originalPath}");

                    return new OptimizedImageResult
                    {
                        OriginalPath = originalRelativePath,
                        WebpPath = webpRelativePath,
                        OriginalFileName = originalFileName,
                        WebpFileName = webpFileName
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error optimizing image with WebP: {fileName}");
                throw;
            }
        }

        public async Task<string> OptimizeAndSaveImageAsync(Stream imageStream, string fileName, string outputFolder, int? maxWidth = null, int? maxHeight = null, int quality = 85)
        {
            try
            {
                // Ensure output folder exists
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // Reset stream position
                imageStream.Position = 0;

                // Load image
                using (var image = await Image.LoadAsync(imageStream))
                {
                    // Resize if needed
                    if (maxWidth.HasValue || maxHeight.HasValue)
                    {
                        var resizeOptions = new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(maxWidth ?? int.MaxValue, maxHeight ?? int.MaxValue)
                        };
                        image.Mutate(x => x.Resize(resizeOptions));
                    }

                    // Determine format based on extension
                    var extension = Path.GetExtension(fileName).ToLowerInvariant();
                    var outputPath = Path.Combine(outputFolder, fileName);
                    var relativePath = $"/images/properties/{fileName}";

                    // Save optimized image
                    using (var outputStream = new FileStream(outputPath, FileMode.Create))
                    {
                        switch (extension)
                        {
                            case ".jpg":
                            case ".jpeg":
                                var jpegEncoder = new JpegEncoder
                                {
                                    Quality = quality
                                };
                                await image.SaveAsync(outputStream, jpegEncoder);
                                break;

                            case ".png":
                                var pngEncoder = new PngEncoder
                                {
                                    CompressionLevel = PngCompressionLevel.BestCompression
                                };
                                await image.SaveAsync(outputStream, pngEncoder);
                                break;

                            default:
                                // Default to JPEG for other formats
                                var defaultEncoder = new JpegEncoder
                                {
                                    Quality = quality
                                };
                                var jpgFileName = Path.ChangeExtension(fileName, ".jpg");
                                outputPath = Path.Combine(outputFolder, jpgFileName);
                                relativePath = $"/images/properties/{jpgFileName}";
                                using (var jpgStream = new FileStream(outputPath, FileMode.Create))
                                {
                                    await image.SaveAsync(jpgStream, defaultEncoder);
                                }
                                break;
                        }
                    }

                    _logger.LogInformation($"Image optimized and saved: {outputPath}");
                    return relativePath;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error optimizing image: {fileName}");
                throw;
            }
        }

        public async Task<(string webpPath, string originalPath)> OptimizeAndSaveImageWithWebPAsync(Stream imageStream, string fileName, string outputFolder, int? maxWidth = null, int? maxHeight = null, int quality = 85)
        {
            try
            {
                // Ensure output folder exists
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // Reset stream position
                imageStream.Position = 0;

                // Load image
                using (var image = await Image.LoadAsync(imageStream))
                {
                    // Resize if needed
                    if (maxWidth.HasValue || maxHeight.HasValue)
                    {
                        var resizeOptions = new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(maxWidth ?? int.MaxValue, maxHeight ?? int.MaxValue)
                        };
                        image.Mutate(x => x.Resize(resizeOptions));
                    }

                    var extension = Path.GetExtension(fileName).ToLowerInvariant();
                    var baseFileName = Path.GetFileNameWithoutExtension(fileName);
                    var originalPath = Path.Combine(outputFolder, fileName);
                    var webpPath = Path.Combine(outputFolder, $"{baseFileName}.webp");

                    // Save original optimized version
                    string originalRelativePath;
                    using (var outputStream = new FileStream(originalPath, FileMode.Create))
                    {
                        switch (extension)
                        {
                            case ".jpg":
                            case ".jpeg":
                                var jpegEncoder = new JpegEncoder { Quality = quality };
                                await image.SaveAsync(outputStream, jpegEncoder);
                                break;

                            case ".png":
                                var pngEncoder = new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression };
                                await image.SaveAsync(outputStream, pngEncoder);
                                break;

                            default:
                                var defaultEncoder = new JpegEncoder { Quality = quality };
                                var jpgFileName = Path.ChangeExtension(fileName, ".jpg");
                                originalPath = Path.Combine(outputFolder, jpgFileName);
                                using (var jpgStream = new FileStream(originalPath, FileMode.Create))
                                {
                                    await image.SaveAsync(jpgStream, defaultEncoder);
                                }
                                fileName = jpgFileName;
                                break;
                        }
                    }

                    originalRelativePath = $"/images/properties/{fileName}";

                    // Save WebP version for modern browsers
                    imageStream.Position = 0;
                    using (var webpImage = await Image.LoadAsync(imageStream))
                    {
                        if (maxWidth.HasValue || maxHeight.HasValue)
                        {
                            var resizeOptions = new ResizeOptions
                            {
                                Mode = ResizeMode.Max,
                                Size = new Size(maxWidth ?? int.MaxValue, maxHeight ?? int.MaxValue)
                            };
                            webpImage.Mutate(x => x.Resize(resizeOptions));
                        }

                        using (var webpStream = new FileStream(webpPath, FileMode.Create))
                        {
                            var webpEncoder = new WebpEncoder
                            {
                                Quality = quality,
                                Method = WebpEncodingMethod.BestQuality
                            };
                            await webpImage.SaveAsync(webpStream, webpEncoder);
                        }
                    }

                    var webpRelativePath = $"/images/properties/{Path.GetFileName(webpPath)}";
                    _logger.LogInformation($"Image optimized (original + WebP) and saved: {originalPath}");
                    return (webpRelativePath, originalRelativePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error optimizing image with WebP: {fileName}");
                throw;
            }
        }
    }
}

