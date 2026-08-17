namespace CafeMenu.Api.Bootstrap;

public interface IPlatformAdminBootstrapService
{
    Task<PlatformAdminBootstrapResult> BootstrapAsync(
        PlatformAdminBootstrapRequest request,
        CancellationToken cancellationToken);
}
