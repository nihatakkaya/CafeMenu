using CafeMenu.Api.Entities;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;

namespace CafeMenu.Api.Bootstrap;

public sealed class PlatformAdminBootstrapService : IPlatformAdminBootstrapService
{
    private const string BootstrapFullName = "Platform Admin";

    private readonly IAppUserRepository _appUserRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public PlatformAdminBootstrapService(
        IAppUserRepository appUserRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _appUserRepository = appUserRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<PlatformAdminBootstrapResult> BootstrapAsync(
        PlatformAdminBootstrapRequest request,
        CancellationToken cancellationToken)
    {
        var email = PlatformAdminBootstrapValidation.NormalizeEmail(request.Email);

        if (!PlatformAdminBootstrapValidation.IsValidEmail(email))
        {
            return PlatformAdminBootstrapResult.Failure(
                PlatformAdminBootstrapStatus.InvalidEmail,
                email,
                "Email is invalid.");
        }

        var passwordErrors = PlatformAdminBootstrapValidation.ValidatePassword(request.Password);
        if (passwordErrors.Count > 0)
        {
            return PlatformAdminBootstrapResult.Failure(
                PlatformAdminBootstrapStatus.InvalidPassword,
                email,
                "Password does not meet the bootstrap password policy.");
        }

        var existingUser = await _appUserRepository.GetByEmailWithRolesAsync(email, cancellationToken);
        if (existingUser is not null)
        {
            return PlatformAdminBootstrapResult.AlreadyExists(existingUser.Id, email);
        }

        var platformAdminRole = await _roleRepository.GetByCodeAsync(ApplicationRoles.PlatformAdmin, cancellationToken);
        if (platformAdminRole is null)
        {
            return PlatformAdminBootstrapResult.Failure(
                PlatformAdminBootstrapStatus.PlatformAdminRoleMissing,
                email,
                "PLATFORM_ADMIN role was not found.");
        }

        var utcNow = DateTimeOffset.UtcNow;
        var user = new AppUserEntity
        {
            Email = email,
            FullName = BootstrapFullName,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        user.Roles.Add(platformAdminRole);

        await _appUserRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return PlatformAdminBootstrapResult.Created(user.Id, email);
    }
}
