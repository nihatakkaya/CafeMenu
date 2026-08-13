using System.Text.RegularExpressions;
using CafeMenu.Api.Exceptions;
using Microsoft.Extensions.Options;

namespace CafeMenu.Api.Storage;

public sealed partial class LocalImageStorage : IImageStorage
{
    private readonly ImageStorageOptions _options;
    private readonly IImageProcessor _imageProcessor;
    private readonly ILogger<LocalImageStorage> _logger;
    private readonly string _localRoot;
    private readonly Uri _publicBaseUri;

    public LocalImageStorage(
        IOptions<ImageStorageOptions> options,
        IWebHostEnvironment environment,
        IImageProcessor imageProcessor,
        ILogger<LocalImageStorage> logger)
    {
        _options = options.Value;
        _imageProcessor = imageProcessor;
        _logger = logger;
        _localRoot = ResolveLocalRoot(_options, environment);
        _publicBaseUri = new Uri(_options.PublicBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    public async Task<StoredImage> StoreAsync(
        ImageUploadInput input,
        ImageStorageFolder folder,
        CancellationToken cancellationToken)
    {
        var processedImage = await _imageProcessor.ProcessAsync(input, cancellationToken);
        var fileName = $"{Guid.NewGuid():N}{processedImage.Extension}";
        var directory = ResolveFolderPath(folder);
        var filePath = ResolveFilePath(folder, fileName);

        try
        {
            Directory.CreateDirectory(directory);
            await File.WriteAllBytesAsync(filePath, processedImage.Content, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ImageStorageApplicationException(
                "Image could not be stored.",
                ApplicationErrorCodes.ImageStorageFailed,
                ex);
        }

        return new StoredImage(
            BuildPublicUrl(folder, fileName),
            fileName,
            processedImage.ContentType);
    }

    public Task DeleteIfManagedAsync(string? publicUrl, CancellationToken cancellationToken)
    {
        if (!TryResolveManagedUrl(publicUrl, out var folder, out var fileName))
        {
            return Task.CompletedTask;
        }

        var filePath = ResolveFilePath(folder, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Managed image file deleted from local storage.");
        }

        return Task.CompletedTask;
    }

    public Task<StoredImageFile?> GetAsync(
        ImageStorageFolder folder,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (!IsSafeGeneratedFileName(fileName))
        {
            return Task.FromResult<StoredImageFile?>(null);
        }

        var filePath = ResolveFilePath(folder, fileName);
        if (!File.Exists(filePath))
        {
            return Task.FromResult<StoredImageFile?>(null);
        }

        var stream = File.OpenRead(filePath);
        var contentType = GetContentType(fileName);
        return Task.FromResult<StoredImageFile?>(new StoredImageFile(stream, contentType));
    }

    public bool IsManagedUrl(string? publicUrl)
    {
        return TryResolveManagedUrl(publicUrl, out _, out _);
    }

    private string ResolveFolderPath(ImageStorageFolder folder)
    {
        var folderName = GetFolderName(folder);
        var folderPath = Path.GetFullPath(Path.Combine(_localRoot, folderName));

        EnsureWithinRoot(folderPath);
        return folderPath;
    }

    private string ResolveFilePath(ImageStorageFolder folder, string fileName)
    {
        if (!IsSafeGeneratedFileName(fileName))
        {
            throw new InvalidOperationException("Invalid generated image file name.");
        }

        var filePath = Path.GetFullPath(Path.Combine(ResolveFolderPath(folder), fileName));
        EnsureWithinRoot(filePath);
        return filePath;
    }

    private bool TryResolveManagedUrl(
        string? publicUrl,
        out ImageStorageFolder folder,
        out string fileName)
    {
        folder = default;
        fileName = string.Empty;

        if (string.IsNullOrWhiteSpace(publicUrl) ||
            !Uri.TryCreate(publicUrl, UriKind.Absolute, out var uri) ||
            !IsSameBaseUri(uri))
        {
            return false;
        }

        var relativePath = _publicBaseUri.MakeRelativeUri(uri).ToString();
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 2 || !TryParseFolder(segments[0], out folder) || !IsSafeGeneratedFileName(segments[1]))
        {
            return false;
        }

        fileName = segments[1];
        return true;
    }

    private bool IsSameBaseUri(Uri uri)
    {
        return string.Equals(uri.Scheme, _publicBaseUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(uri.Host, _publicBaseUri.Host, StringComparison.OrdinalIgnoreCase) &&
            uri.Port == _publicBaseUri.Port &&
            uri.AbsolutePath.StartsWith(_publicBaseUri.AbsolutePath, StringComparison.Ordinal);
    }

    private string BuildPublicUrl(ImageStorageFolder folder, string fileName)
    {
        return new Uri(_publicBaseUri, $"{GetFolderName(folder)}/{fileName}").ToString();
    }

    private void EnsureWithinRoot(string path)
    {
        var normalizedRoot = Path.GetFullPath(_localRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var normalizedPath = Path.GetFullPath(path);

        if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Resolved image storage path is outside the configured root.");
        }
    }

    private static string ResolveLocalRoot(ImageStorageOptions options, IWebHostEnvironment environment)
    {
        var localRoot = options.LocalRoot;
        if (string.IsNullOrWhiteSpace(localRoot) && environment.IsDevelopment())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            localRoot = Path.Combine(localAppData, "CafeMenu", "media");
        }

        if (string.IsNullOrWhiteSpace(localRoot))
        {
            throw new InvalidOperationException("ImageStorage:LocalRoot must be configured.");
        }

        return Path.GetFullPath(localRoot);
    }

    private static string GetFolderName(ImageStorageFolder folder)
    {
        return folder switch
        {
            ImageStorageFolder.CafeLogos => "cafe-logos",
            ImageStorageFolder.CafeCovers => "cafe-covers",
            ImageStorageFolder.Categories => "categories",
            ImageStorageFolder.Products => "products",
            _ => throw new InvalidOperationException("Unsupported image storage folder.")
        };
    }

    private static bool TryParseFolder(string value, out ImageStorageFolder folder)
    {
        folder = value switch
        {
            "cafe-logos" => ImageStorageFolder.CafeLogos,
            "cafe-covers" => ImageStorageFolder.CafeCovers,
            "categories" => ImageStorageFolder.Categories,
            "products" => ImageStorageFolder.Products,
            _ => default
        };

        return value is "cafe-logos" or "cafe-covers" or "categories" or "products";
    }

    private static string GetContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static bool IsSafeGeneratedFileName(string fileName)
    {
        return GeneratedFileNameRegex().IsMatch(fileName);
    }

    [GeneratedRegex("^[a-f0-9]{32}\\.(jpg|png|webp)$", RegexOptions.CultureInvariant)]
    private static partial Regex GeneratedFileNameRegex();
}
