using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Mappings;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;
using CafeMenu.Api.Utilities;
using Microsoft.EntityFrameworkCore;

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

    public async Task<CafeResponseDto> ChangeCafePublicationAsync(
        long appUserId,
        long cafeId,
        ChangeCafePublicationRequest request,
        CancellationToken cancellationToken)
    {
        await _tenantAuthorizationService.EnsureCafeAccessAsync(
            appUserId,
            cafeId,
            CafeOwnerRoles,
            allowPlatformAdmin: true,
            cancellationToken);

        var cafe = await GetCafeOrThrowAsync(cafeId, cancellationToken);
        cafe.IsPublished = request.IsPublished;
        cafe.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cafe {CafeId} publication changed", cafe.Id);

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
        var user = await GetAssignableUserOrThrowAsync(request.AppUserId, cancellationToken);
        var ownerRole = await GetCafeRoleOrThrowAsync(ApplicationRoles.CafeOwner, cancellationToken);

        var response = await AssignCafeRoleAsync(user, cafe, ownerRole, cancellationToken);
        _logger.LogInformation("User {UserId} assigned as owner for cafe {CafeId}", user.Id, cafe.Id);

        return response;
    }

    public async Task<CafeMembershipResponseDto> AssignCafeManagerAsync(
        long appUserId,
        AssignCafeManagerRequest request,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentActiveUserOrThrowAsync(appUserId, cancellationToken);
        var isPlatformAdmin = HasPlatformAdminRole(currentUser);

        if (!isPlatformAdmin)
        {
            await _tenantAuthorizationService.EnsureCafeAccessAsync(
                appUserId,
                request.CafeId,
                CafeOwnerRoles,
                allowPlatformAdmin: false,
                cancellationToken);
        }

        var cafe = await GetCafeOrThrowAsync(request.CafeId, cancellationToken);
        var user = await GetAssignableUserOrThrowAsync(request.AppUserId, cancellationToken);
        var managerRole = await GetCafeRoleOrThrowAsync(ApplicationRoles.CafeManager, cancellationToken);
        var existingMembership = await _cafeMembershipRepository.GetActiveMembershipForUserCafeAsync(
            user.Id,
            cafe.Id,
            cancellationToken);

        if (!isPlatformAdmin && existingMembership?.Role.Code == ApplicationRoles.CafeOwner)
        {
            throw new ForbiddenApplicationException(
                "Cafe owner memberships can only be changed by a platform administrator.",
                ApplicationErrorCodes.TenantAccessForbidden);
        }

        var response = await AssignCafeRoleAsync(user, cafe, managerRole, cancellationToken, existingMembership);
        _logger.LogInformation("User {UserId} assigned as manager for cafe {CafeId}", user.Id, cafe.Id);

        return response;
    }

    public async Task<CafeMembershipResponseDto> DeactivateCafeMembershipAsync(
        long appUserId,
        long membershipId,
        CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentActiveUserOrThrowAsync(appUserId, cancellationToken);
        var isPlatformAdmin = HasPlatformAdminRole(currentUser);
        var membership = await GetMembershipOrThrowAsync(membershipId, cancellationToken);

        if (!isPlatformAdmin)
        {
            await _tenantAuthorizationService.EnsureCafeAccessAsync(
                appUserId,
                membership.CafeId,
                CafeOwnerRoles,
                allowPlatformAdmin: false,
                cancellationToken);

            if (membership.Role.Code != ApplicationRoles.CafeManager)
            {
                throw new ForbiddenApplicationException(
                    "Only platform administrators can deactivate cafe owner memberships.",
                    ApplicationErrorCodes.TenantAccessForbidden);
            }
        }

        if (membership.IsActive)
        {
            membership.IsActive = false;
            membership.UpdatedAt = DateTimeOffset.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cafe membership {MembershipId} deactivated", membership.Id);
        }

        return ToMembershipResponse(membership);
    }

    public async Task<IReadOnlyCollection<CafeMemberResponseDto>> GetCafeMembersAsync(
        long appUserId,
        long cafeId,
        CancellationToken cancellationToken)
    {
        await _tenantAuthorizationService.EnsureCafeAccessAsync(
            appUserId,
            cafeId,
            CafeOwnerRoles,
            allowPlatformAdmin: true,
            cancellationToken);

        _ = await GetCafeOrThrowAsync(cafeId, cancellationToken);
        var memberships = await _cafeMembershipRepository.GetActiveMembershipsForCafeAsync(cafeId, cancellationToken);

        return memberships
            .Select(_cafeMapper.ToMemberResponse)
            .ToArray();
    }

    private async Task<CafeMembershipResponseDto> AssignCafeRoleAsync(
        AppUserEntity user,
        CafeEntity cafe,
        RoleEntity role,
        CancellationToken cancellationToken,
        CafeMembershipEntity? existingMembership = null)
    {
        existingMembership ??= await _cafeMembershipRepository.GetActiveMembershipForUserCafeAsync(
            user.Id,
            cafe.Id,
            cancellationToken);

        var utcNow = DateTimeOffset.UtcNow;
        if (existingMembership is not null)
        {
            if (existingMembership.RoleId != role.Id)
            {
                existingMembership.RoleId = role.Id;
                existingMembership.Role = role;
                existingMembership.UpdatedAt = utcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return ToMembershipResponse(existingMembership);
        }

        var membership = new CafeMembershipEntity
        {
            AppUserId = user.Id,
            CafeId = cafe.Id,
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            AppUser = user,
            Cafe = cafe,
            Role = role
        };

        await _cafeMembershipRepository.AddAsync(membership, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictApplicationException(
                "User already has an active membership for this cafe.",
                ApplicationErrorCodes.CafeMembershipAlreadyExists);
        }

        return ToMembershipResponse(membership);
    }

    private async Task<AppUserEntity> GetAssignableUserOrThrowAsync(long appUserId, CancellationToken cancellationToken)
    {
        var user = await _appUserRepository.GetByIdWithRolesAsync(appUserId, cancellationToken);

        if (user is null || !user.IsActive || user.IsDeleted)
        {
            throw new NotFoundApplicationException("User was not found.", ApplicationErrorCodes.UserNotFound);
        }

        return user;
    }

    private async Task<AppUserEntity> GetCurrentActiveUserOrThrowAsync(long appUserId, CancellationToken cancellationToken)
    {
        var user = await _appUserRepository.GetByIdWithRolesAsync(appUserId, cancellationToken);

        if (user is null || !user.IsActive || user.IsDeleted)
        {
            throw new UnauthorizedApplicationException("User is not authorized.", "AUTH004");
        }

        return user;
    }

    private async Task<RoleEntity> GetCafeRoleOrThrowAsync(string roleCode, CancellationToken cancellationToken)
    {
        return await _roleRepository.GetByCodeAsync(roleCode, cancellationToken)
            ?? throw new NotFoundApplicationException("Cafe role was not found.", ApplicationErrorCodes.CafeMembershipNotFound);
    }

    private async Task<CafeMembershipEntity> GetMembershipOrThrowAsync(long membershipId, CancellationToken cancellationToken)
    {
        return await _cafeMembershipRepository.GetByIdWithUserCafeRoleAsync(membershipId, cancellationToken)
            ?? throw new NotFoundApplicationException("Cafe membership was not found.", ApplicationErrorCodes.CafeMembershipNotFound);
    }

    private static CafeMembershipResponseDto ToMembershipResponse(CafeMembershipEntity membership)
    {
        return new CafeMembershipResponseDto(
            membership.Id,
            membership.CafeId,
            membership.AppUserId,
            membership.AppUser.Email,
            membership.AppUser.FullName,
            membership.Role.Code,
            membership.IsActive);
    }

    private static bool HasPlatformAdminRole(AppUserEntity user)
    {
        return user.Roles.Any(role => role.Code == ApplicationRoles.PlatformAdmin);
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
