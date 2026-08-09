using CafeMenu.Api.DTOs.Responses;

namespace CafeMenu.Api.Services;

public interface IPublicMenuService
{
    Task<PublicMenuResponseDto> GetMenuAsync(string slug, CancellationToken cancellationToken);
}
