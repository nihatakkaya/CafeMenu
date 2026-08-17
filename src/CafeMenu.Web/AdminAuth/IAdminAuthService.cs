using System.Security.Claims;

namespace CafeMenu.Web.AdminAuth;

public interface IAdminAuthService
{
    Task<AdminLoginResult> LoginAsync(AdminLoginCommand command, CancellationToken cancellationToken);

    Task LogoutAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
