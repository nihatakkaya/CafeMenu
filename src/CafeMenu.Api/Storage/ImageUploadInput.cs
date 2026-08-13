namespace CafeMenu.Api.Storage;

public sealed record ImageUploadInput(
    string OriginalFileName,
    string ContentType,
    long Length,
    Stream Content);
