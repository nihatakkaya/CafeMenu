using CafeMenu.Api.Exceptions;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace CafeMenu.Api.Storage;

public sealed class ImageProcessor : IImageProcessor
{
    private static readonly IReadOnlyDictionary<string, SupportedImageFormat> FormatsByExtension =
        new Dictionary<string, SupportedImageFormat>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new(".jpg", "image/jpeg"),
            [".jpeg"] = new(".jpg", "image/jpeg"),
            [".png"] = new(".png", "image/png"),
            [".webp"] = new(".webp", "image/webp")
        };

    private readonly ImageStorageOptions _options;

    public ImageProcessor(IOptions<ImageStorageOptions> options)
    {
        _options = options.Value;
    }

    public async Task<ProcessedImage> ProcessAsync(ImageUploadInput input, CancellationToken cancellationToken)
    {
        var requestedFormat = ValidateRequestShape(input);
        await using var inputBuffer = await CopyToBoundedBufferAsync(input, cancellationToken);

        ValidateSignature(inputBuffer, requestedFormat);

        inputBuffer.Position = 0;

        try
        {
            using var managedStream = new SKManagedStream(inputBuffer, disposeManagedStream: false);
            using var codec = SKCodec.Create(managedStream);
            if (codec is null)
            {
                throw new BadRequestApplicationException(
                    "Uploaded file is not a valid supported image.",
                    ApplicationErrorCodes.ImageInvalid);
            }

            using var bitmap = SKBitmap.Decode(codec);
            if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                throw new BadRequestApplicationException(
                    "Uploaded file is not a valid supported image.",
                    ApplicationErrorCodes.ImageInvalid);
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var encodedImage = image.Encode(CreateEncodedImageFormat(requestedFormat), quality: 85);

            if (encodedImage is null)
            {
                throw new BadRequestApplicationException(
                    "Uploaded file is not a valid supported image.",
                    ApplicationErrorCodes.ImageInvalid);
            }

            return new ProcessedImage(encodedImage.ToArray(), requestedFormat.Extension, requestedFormat.ContentType);
        }
        catch (ArgumentException ex)
        {
            throw new BadRequestApplicationException(
                "Uploaded file is not a valid supported image.",
                ApplicationErrorCodes.ImageInvalid,
                ex);
        }
    }

    private SupportedImageFormat ValidateRequestShape(ImageUploadInput input)
    {
        if (input.Length <= 0)
        {
            throw new BadRequestApplicationException(
                "Image file is required.",
                ApplicationErrorCodes.ImageInvalid);
        }

        if (input.Length > _options.MaxFileSizeBytes)
        {
            throw new BadRequestApplicationException(
                "Image file is too large.",
                ApplicationErrorCodes.ImageTooLarge);
        }

        var extension = Path.GetExtension(input.OriginalFileName);
        if (string.IsNullOrWhiteSpace(extension) || !FormatsByExtension.TryGetValue(extension, out var format))
        {
            throw new BadRequestApplicationException(
                "Image format is not supported.",
                ApplicationErrorCodes.ImageUnsupportedFormat);
        }

        if (!string.Equals(input.ContentType, format.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestApplicationException(
                "Image content type does not match the supported format.",
                ApplicationErrorCodes.ImageInvalid);
        }

        return format;
    }

    private async Task<MemoryStream> CopyToBoundedBufferAsync(
        ImageUploadInput input,
        CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream(capacity: (int)Math.Min(input.Length, _options.MaxFileSizeBytes));
        var readBuffer = new byte[81920];
        long totalBytes = 0;

        while (true)
        {
            var bytesRead = await input.Content.ReadAsync(readBuffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;
            if (totalBytes > _options.MaxFileSizeBytes)
            {
                await buffer.DisposeAsync();
                throw new BadRequestApplicationException(
                    "Image file is too large.",
                    ApplicationErrorCodes.ImageTooLarge);
            }

            await buffer.WriteAsync(readBuffer.AsMemory(0, bytesRead), cancellationToken);
        }

        buffer.Position = 0;
        return buffer;
    }

    private static void ValidateSignature(Stream content, SupportedImageFormat format)
    {
        Span<byte> header = stackalloc byte[12];
        var bytesRead = content.Read(header);
        content.Position = 0;

        var isValid = format.ContentType switch
        {
            "image/jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => bytesRead >= 8 &&
                header[0] == 0x89 &&
                header[1] == 0x50 &&
                header[2] == 0x4E &&
                header[3] == 0x47 &&
                header[4] == 0x0D &&
                header[5] == 0x0A &&
                header[6] == 0x1A &&
                header[7] == 0x0A,
            "image/webp" => bytesRead >= 12 &&
                header[0] == 0x52 &&
                header[1] == 0x49 &&
                header[2] == 0x46 &&
                header[3] == 0x46 &&
                header[8] == 0x57 &&
                header[9] == 0x45 &&
                header[10] == 0x42 &&
                header[11] == 0x50,
            _ => false
        };

        if (!isValid)
        {
            throw new BadRequestApplicationException(
                "Image signature does not match the supported format.",
                ApplicationErrorCodes.ImageInvalid);
        }
    }

    private static SKEncodedImageFormat CreateEncodedImageFormat(SupportedImageFormat format)
    {
        return format.ContentType switch
        {
            "image/jpeg" => SKEncodedImageFormat.Jpeg,
            "image/png" => SKEncodedImageFormat.Png,
            "image/webp" => SKEncodedImageFormat.Webp,
            _ => throw new InvalidOperationException("Unsupported image encoder.")
        };
    }
}
