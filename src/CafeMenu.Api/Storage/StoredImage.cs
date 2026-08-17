namespace CafeMenu.Api.Storage;

public sealed record StoredImage(string PublicUrl, string FileName, string ContentType);

public sealed record StoredImageFile(Stream Content, string ContentType);

public enum ImageStorageFolder
{
    CafeLogos,
    CafeCovers,
    Categories,
    Products
}
