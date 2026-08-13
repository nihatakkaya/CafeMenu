using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace CafeMenu.Api.Storage;

public sealed class ImageStorageFormOptionsSetup : IConfigureOptions<FormOptions>
{
    private readonly IOptions<ImageStorageOptions> _imageStorageOptions;

    public ImageStorageFormOptionsSetup(IOptions<ImageStorageOptions> imageStorageOptions)
    {
        _imageStorageOptions = imageStorageOptions;
    }

    public void Configure(FormOptions options)
    {
        options.MultipartBodyLengthLimit = _imageStorageOptions.Value.MaxFileSizeBytes;
    }
}
