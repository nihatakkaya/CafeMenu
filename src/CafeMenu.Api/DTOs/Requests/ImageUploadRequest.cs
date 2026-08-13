using System.ComponentModel.DataAnnotations;

namespace CafeMenu.Api.DTOs.Requests;

public sealed class ImageUploadRequest
{
    [Required]
    public IFormFile? File { get; init; }
}
