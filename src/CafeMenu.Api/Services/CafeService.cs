using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Mappings;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;
using CafeMenu.Api.Utilities;

namespace CafeMenu.Api.Services;

public sealed class CafeService : ICafeService
{
    private static readonly string[] CafeReaderRoles = [ApplicationRoles.CafeOwner, ApplicationRoles.CafeManager];
    private static readonly string[] CafeOwnerRoles = [ApplicationRoles.CafeOwner];
    private static readonly string[] PlatformAdminRoleCodes = [ApplicationRoles.PlatformAdmin];

    private readonly ICafeRepository _cafeRepository;
    private readonly ICafeMembershipRepository _cafeMembershipRepository;
    private readonly IAppUserRepository _appUserRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantAuthorizationService _tenantAuthorizationService;
    private readonly CafeMapper _cafeMapper;
    private readonly ILogger<CafeService> _logger;

    public CafeService(
        ICafeRepository cafeRepository,
        ICafeMembershipRepository cafeMembershipRepository,
        IAppUserRepository appUserRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        ITenantAuthorizationService tenantAuthorizationService,
        CafeMapper cafeMapper,
        ILogger<CafeService> logger)
    {
        _cafeRepository = cafeRepository;
        _cafeMembershipRepository = cafeMembershipRepository;
        _appUserRepository = appUserRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _tenantAuthorizationService = tenantAuthorizationService;
        _cafeMapper = cafeMapper;
        _logger = logger;
    }

    public async Task<CafeResponseDto> CreateCafeAsync(CreateCafeRequest request, CancellationToken cancellationToken)
    {
        var slug = NormalizeSlug(request.Slug ?? request.Name);

        if (await _cafeRepository.SlugExistsAsync(slug, cancellationToken))
        {
            throw new ConflictApplicationException(
                "Cafe slug is already in use.",
                ApplicationErrorCodes.CafeSlugAlreadyExists);
        }

        var utcNow = DateTimeOffset.UtcNow;
        var cafe = new CafeEntity
        {
            Name = request.Name.Trim(),
            Slug = slug,
            IsActive = true,
            IsPublished = false,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        await _cafeRepository.AddAsync(cafe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cafe {CafeId} created", cafe.Id);

        return _cafeMapper.ToResponse(cafe);
    }

    public async Task<CafeDetailResponseDto> GetCafeByIdAsync(long appUserId, long cafeId, CancellationToken cancellationToken)
    {
        await _tenantAuthorizationService.EnsureCafeAccessAsync(
            appUserId,
            cafeId,
            CafeReaderRoles,
            allowPlatformAdmin: true,
            cancellationToken);

        var cafe = await GetCafeWithMembershipsOrThrowAsync(cafeId, cancellationToken);
        return _cafeMapper.ToDetailResponse(cafe);
    }

    public async Task<IReadOnlyCollection<CafeResponseDto>> GetCafesAsync(CancellationToken cancellationToken)
    {
        var cafes = await _cafeRepository.GetAllAsync(cancellationToken);
        return cafes.Select(_cafeMapper.ToResponse).ToArray();
    }

    public async Task<IReadOnlyCollection<MyCafeResponseDto>> GetMyCafesAsync(long appUserId, CancellationToken cancellationToken)
    {
        var user = await _appUserRepository.GetByIdWithRolesAsync(appUserId, cancellationToken);

        if (user is null || !user.IsActive || user.IsDeleted)
        {
            throw new UnauthorizedApplicationException("User is not authorized.", "AUTH004");
        }

        if (user.Roles.Any(role => role.Code == ApplicationRoles.PlatformAdmin))
        {
            var cafes = await _cafeRepository.GetAllAsync(cancellationToken);

            return cafes
                .Select(cafe => _cafeMapper.ToMyCafeResponse(cafe, PlatformAdminRoleCodes))
                .ToArray();
        }

        var memberships = await _cafeMembershipRepository.GetActiveMembershipsForUserAsync(
            appUserId,
            CafeReaderRoles,
            cancellationToken);

        return memberships
            .GroupBy(membership => membership.CafeId)
            .Select(group =>
            {
                var cafe = group.First().Cafe;
                var roleCodes = group
                    .Select(membership => membership.Role.Code)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToArray();

                return _cafeMapper.ToMyCafeResponse(cafe, roleCodes);
            })
            .OrderBy(cafe => cafe.Name, StringComparer.Ordinal)
            .ThenBy(cafe => cafe.Id)
            .ToArray();
    }

    public async Task<CafeResponseDto> UpdateCafeAsync(
        long appUserId,
        long cafeId,
        UpdateCafeRequest request,
        CancellationToken cancellationToken)
    {
        await _tenantAuthorizationService.EnsureCafeAccessAsync(
            appUserId,
            cafeId,
            CafeOwnerRoles,
            allowPlatformAdmin: true,
            cancellationToken);

        var cafe = await GetCafeOrThrowAsync(cafeId, cancellationToken);
        var slug = NormalizeSlug(request.Slug ?? cafe.Slug);

        if (!string.Equals(cafe.Slug, slug, StringComparison.Ordinal) &&
            await _cafeRepository.SlugExistsAsync(slug, cancellationToken))
        {
            throw new ConflictApplicationException(
                "Cafe slug is already in use.",
                ApplicationErrorCodes.CafeSlugAlreadyExists);
        }

        cafe.Name = request.Name.Trim();
        cafe.Slug = slug;
        cafe.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cafe {CafeId} updated", cafe.Id);

        return _cafeMapper.ToResponse(cafe);
    }

    public async Task<CafeResponseDto> ActivateCafeAsync(long cafeId, CancellationToken cancellationToken)
    {
        var cafe = await GetCafeOrThrowAsync(cafeId, cancellationToken);
        cafe.IsActive = true;
        cafe.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cafe {CafeId} activated", cafe.Id);

        return _cafeMapper.ToResponse(cafe);
    }

    public async Task<CafeResponseDto> DeactivateCafeAsync(long cafeId, CancellationToken cancellationToken)
    {
        var cafe = await GetCafeOrThrowAsync(cafeId, cancellationToken);
        cafe.IsActive = false;
        cafe.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cafe {CafeId} deactivated", cafe.Id);

        return _cafeMapper.ToResponse(cafe);
    }

    public async Task<CafeMembershipResponseDto> AssignCafeOwnerAsync(
        AssignCafeOwnerRequest request,
        CancellationToken cancellationToken)
    {
        var cafe = await GetCafeOrThrowAsync(request.CafeId, cancellationToken);
        var user = await _appUserRepository.GetByIdWithRolesAsync(request.AppUserId, cancellationToken);

        if (user is null || !user.IsActive || user.IsDeleted)
        {
            throw new NotFoundApplicationException("User was not found.", "USER001");
        }

        if (await _cafeMembershipRepository.ActiveMembershipExistsAsync(user.Id, cafe.Id, cancellationToken))
        {
            throw new ConflictApplicationException(
                "User already has an active membership for this cafe.",
                ApplicationErrorCodes.CafeMembershipAlreadyExists);
        }

        var ownerRole = await _roleRepository.GetByCodeAsync(ApplicationRoles.CafeOwner, cancellationToken)
            ?? throw new NotFoundApplicationException("Cafe owner role was not found.", ApplicationErrorCodes.CafeMembershipNotFound);

        var utcNow = DateTimeOffset.UtcNow;
        var membership = new CafeMembershipEntity
        {
            AppUserId = user.Id,
            CafeId = cafe.Id,
            RoleId = ownerRole.Id,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            AppUser = user,
            Cafe = cafe,
            Role = ownerRole
        };

        await _cafeMembershipRepository.AddAsync(membership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} assigned as owner for cafe {CafeId}", user.Id, cafe.Id);

        return new CafeMembershipResponseDto(
            membership.Id,
            cafe.Id,
            user.Id,
            user.Email,
            user.FullName,
            ownerRole.Code,
            membership.IsActive);
    }

    private async Task<CafeEntity> GetCafeOrThrowAsync(long cafeId, CancellationToken cancellationToken)
    {
        return await _cafeRepository.GetByIdAsync(cafeId, cancellationToken)
            ?? throw new NotFoundApplicationException("Cafe was not found.", ApplicationErrorCodes.CafeNotFound);
    }

    private async Task<CafeEntity> GetCafeWithMembershipsOrThrowAsync(long cafeId, CancellationToken cancellationToken)
    {
        return await _cafeRepository.GetByIdWithMembershipsAsync(cafeId, cancellationToken)
            ?? throw new NotFoundApplicationException("Cafe was not found.", ApplicationErrorCodes.CafeNotFound);
    }

    private static string NormalizeSlug(string value)
    {
        var slug = SlugNormalizer.Normalize(value);

        if (slug.Length < 2)
        {
            throw new ConflictApplicationException("Cafe slug is invalid.", ApplicationErrorCodes.CafeSlugAlreadyExists);
        }

        return slug;
    }
}
