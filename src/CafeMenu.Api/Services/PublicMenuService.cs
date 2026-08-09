using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Mappings;
using CafeMenu.Api.Repositories;

namespace CafeMenu.Api.Services;

public sealed class PublicMenuService : IPublicMenuService
{
    private readonly IPublicMenuRepository _publicMenuRepository;
    private readonly PublicMenuMapper _publicMenuMapper;

    public PublicMenuService(
        IPublicMenuRepository publicMenuRepository,
        PublicMenuMapper publicMenuMapper)
    {
        _publicMenuRepository = publicMenuRepository;
        _publicMenuMapper = publicMenuMapper;
    }

    public async Task<PublicMenuResponseDto> GetMenuAsync(string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var cafe = await _publicMenuRepository.GetPublishedMenuBySlugAsync(normalizedSlug, cancellationToken)
            ?? throw new NotFoundApplicationException("Public menu was not found.", ApplicationErrorCodes.CafeNotFound);

        return _publicMenuMapper.ToResponse(cafe);
    }
}
