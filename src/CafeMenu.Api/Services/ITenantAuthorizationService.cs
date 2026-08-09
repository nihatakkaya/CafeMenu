namespace CafeMenu.Api.Services;

public interface ITenantAuthorizationService
{
    Task EnsureCafeAccessAsync(
        long appUserId,
        long cafeId,
        IReadOnlyCollection<string> allowedCafeRoles,
        bool allowPlatformAdmin,
        CancellationToken cancellationToken);
}
