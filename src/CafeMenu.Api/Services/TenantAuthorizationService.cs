using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;

namespace CafeMenu.Api.Services;

public sealed class TenantAuthorizationService : ITenantAuthorizationService
{
    private readonly IAppUserRepository _appUserRepository;
    private readonly ICafeMembershipRepository _cafeMembershipRepository;

    public TenantAuthorizationService(
        IAppUserRepository appUserRepository,
        ICafeMembershipRepository cafeMembershipRepository)
    {
        _appUserRepository = appUserRepository;
        _cafeMembershipRepository = cafeMembershipRepository;
    }

    public async Task EnsureCafeAccessAsync(
        long appUserId,
        long cafeId,
        IReadOnlyCollection<string> allowedCafeRoles,
        bool allowPlatformAdmin,
        CancellationToken cancellationToken)
    {
        var user = await _appUserRepository.GetByIdWithRolesAsync(appUserId, cancellationToken);

        if (user is null || !user.IsActive || user.IsDeleted)
        {
            throw new UnauthorizedApplicationException("User is not authorized.", "AUTH004");
        }

        if (allowPlatformAdmin && user.Roles.Any(role => role.Code == ApplicationRoles.PlatformAdmin))
        {
            return;
        }

        var membership = await _cafeMembershipRepository.GetActiveMembershipAsync(appUserId, cafeId, cancellationToken);

        if (membership is null)
        {
            throw new ForbiddenApplicationException(
                "Cafe membership is required for this operation.",
                ApplicationErrorCodes.TenantAccessForbidden);
        }

        if (!membership.Cafe.IsActive || membership.Cafe.IsDeleted)
        {
            throw new ForbiddenApplicationException(
                "Cafe is not active for private administration operations.",
                ApplicationErrorCodes.CafeInactive);
        }

        if (!allowedCafeRoles.Contains(membership.Role.Code, StringComparer.Ordinal))
        {
            throw new ForbiddenApplicationException(
                "Cafe membership role is not allowed for this operation.",
                ApplicationErrorCodes.TenantAccessForbidden);
        }
    }
}
