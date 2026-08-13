namespace CafeMenu.Api.Storage;

public static class ImageStorageEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapImageStorageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/media/{folder}/{fileName}", GetImageAsync)
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> GetImageAsync(
        string folder,
        string fileName,
        IImageStorage imageStorage,
        CancellationToken cancellationToken)
    {
        if (!TryParseFolder(folder, out var storageFolder))
        {
            return Results.NotFound();
        }

        var file = await imageStorage.GetAsync(storageFolder, fileName, cancellationToken);

        return file is null
            ? Results.NotFound()
            : Results.File(file.Content, file.ContentType);
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
}
