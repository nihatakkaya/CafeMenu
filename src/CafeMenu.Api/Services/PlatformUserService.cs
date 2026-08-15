using CafeMenu.Api.Bootstrap;
using CafeMenu.Api.Configuration;
using CafeMenu.Api.Data;
using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Mappings;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CafeMenu.Api.Services;

public sealed class PlatformUserService : IPlatformUserService
{
    private const int MaximumTokenGenerationAttempts = 5;
    private const string InvalidSetupTokenMessage = "Setup token is invalid or expired.";

    private readonly IAppUserRepository _appUserRepository;
    private readonly IUserSetupTokenRepository _setupTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly PlatformUserMapper _platformUserMapper;
    private readonly CafeMenuDbContext _dbContext;
    private readonly UserSetupOptions _options;
    private readonly ILogger<PlatformUserService> _logger;

    public PlatformUserService(
        IAppUserRepository appUserRepository,
        IUserSetupTokenRepository setupTokenRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        PlatformUserMapper platformUserMapper,
        CafeMenuDbContext dbContext,
        IOptions<UserSetupOptions> options,
        ILogger<PlatformUserService> logger)
    {
        _appUserRepository = appUserRepository;
        _setupTokenRepository = setupTokenRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _platformUserMapper = platformUserMapper;
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<UserSetupResponseDto> CreateUserSetupAsync(
        CreateUserSetupRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeAndValidateEmail(request.Email);
        var fullName = NormalizeAndValidateFullName(request.FullName);

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            if (await _appUserRepository.EmailExistsIncludingDeletedAsync(email, cancellationToken))
            {
                throw new ConflictApplicationException(
                    "Email is already in use.",
                    ApplicationErrorCodes.UserEmailAlreadyExists);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var utcNow = DateTimeOffset.UtcNow;
            var user = new AppUserEntity
            {
                Email = email,
                FullName = fullName,
                PasswordHash = _passwordHasher.HashPassword(UserSetupTokenGenerator.Generate()),
                IsActive = false,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };

            var generatedToken = await CreateSetupTokenAsync(user.Id, utcNow, cancellationToken);
            await _appUserRepository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            generatedToken.Entity.AppUserId = user.Id;
            await _setupTokenRepository.AddAsync(generatedToken.Entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Pending platform user {UserId} created for setup", user.Id);

            return _platformUserMapper.ToSetupResponse(user, generatedToken.PlainToken, generatedToken.Entity.ExpiresAt);
        });
    }

    public async Task<PlatformUserResponseDto> CompleteUserSetupAsync(
        CompleteUserSetupRequest request,
        CancellationToken cancellationToken)
    {
        ValidatePasswordConfirmation(request.Password, request.ConfirmPassword);
        ValidatePasswordPolicy(request.Password);

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            var tokenHash = UserSetupTokenGenerator.Hash(request.Token.Trim());
            var setupToken = await _setupTokenRepository.GetByTokenHashWithUserAsync(tokenHash, cancellationToken);
            var utcNow = DateTimeOffset.UtcNow;

            EnsureSetupTokenCanBeUsed(setupToken, utcNow);
            var user = setupToken!.AppUser;

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            user.PasswordHash = _passwordHasher.HashPassword(request.Password);
            user.IsActive = true;
            user.UpdatedAt = utcNow;

            var openTokens = await _setupTokenRepository.GetUnconsumedByUserIdAsync(user.Id, cancellationToken);
            foreach (var openToken in openTokens)
            {
                openToken.ConsumedAt = utcNow;
                openToken.UpdatedAt = utcNow;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("User {UserId} completed setup", user.Id);

            return _platformUserMapper.ToResponse(user);
        });
    }

    public async Task<UserSetupResponseDto> ReissueUserSetupAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            var user = await _appUserRepository.GetByIdWithRolesAsync(userId, cancellationToken)
                ?? throw new NotFoundApplicationException("User was not found.", ApplicationErrorCodes.UserNotFound);

            if (user.IsActive)
            {
                throw new ConflictApplicationException(
                    "User setup is already completed.",
                    ApplicationErrorCodes.UserSetupAlreadyCompleted);
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var utcNow = DateTimeOffset.UtcNow;
            var openTokens = await _setupTokenRepository.GetUnconsumedByUserIdAsync(user.Id, cancellationToken);
            foreach (var openToken in openTokens)
            {
                openToken.ConsumedAt = utcNow;
                openToken.UpdatedAt = utcNow;
            }

            var generatedToken = await CreateSetupTokenAsync(user.Id, utcNow, cancellationToken);
            await _setupTokenRepository.AddAsync(generatedToken.Entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Setup token reissued for user {UserId}", user.Id);

            return _platformUserMapper.ToSetupResponse(user, generatedToken.PlainToken, generatedToken.Entity.ExpiresAt);
        });
    }

    public async Task<IReadOnlyCollection<PlatformUserSearchResponseDto>> SearchUsersAsync(
        SearchPlatformUsersRequest request,
        CancellationToken cancellationToken)
    {
        var query = NormalizeAndValidateSearchQuery(request.Query);
        var pageSize = Math.Clamp(request.PageSize, 1, 20);
        var users = await _appUserRepository.SearchForPlatformOnboardingAsync(query, pageSize, cancellationToken);

        return users
            .Select(_platformUserMapper.ToSearchResponse)
            .ToArray();
    }

    private async Task<GeneratedSetupToken> CreateSetupTokenAsync(
        long appUserId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaximumTokenGenerationAttempts; attempt++)
        {
            var token = UserSetupTokenGenerator.Generate();
            var tokenHash = UserSetupTokenGenerator.Hash(token);

            if (await _setupTokenRepository.TokenHashExistsAsync(tokenHash, cancellationToken))
            {
                continue;
            }

            return new GeneratedSetupToken(
                token,
                new UserSetupTokenEntity
                {
                    AppUserId = appUserId,
                    TokenHash = tokenHash,
                    ExpiresAt = utcNow.Add(_options.TokenExpiration),
                    CreatedAt = utcNow,
                    UpdatedAt = utcNow
                });
        }

        throw new InvalidOperationException("Could not generate a unique setup token.");
    }

    private static string NormalizeAndValidateEmail(string email)
    {
        var normalizedEmail = PlatformAdminBootstrapValidation.NormalizeEmail(email);
        if (!PlatformAdminBootstrapValidation.IsValidEmail(normalizedEmail))
        {
            throw new BadRequestApplicationException("Email is invalid.", ApplicationErrorCodes.ValidationFailed);
        }

        return normalizedEmail;
    }

    private static string NormalizeAndValidateFullName(string fullName)
    {
        var normalizedFullName = fullName.Trim();
        if (normalizedFullName.Length < 2 || normalizedFullName.Length > 200)
        {
            throw new BadRequestApplicationException("Full name is invalid.", ApplicationErrorCodes.ValidationFailed);
        }

        return normalizedFullName;
    }

    private static string NormalizeAndValidateSearchQuery(string query)
    {
        var normalizedQuery = query.Trim();
        if (normalizedQuery.Length < 2 || normalizedQuery.Length > 320)
        {
            throw new BadRequestApplicationException(
                "Search query is invalid.",
                ApplicationErrorCodes.ValidationFailed);
        }

        return normalizedQuery;
    }

    private static void ValidatePasswordConfirmation(string password, string confirmPassword)
    {
        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            throw new BadRequestApplicationException(
                "Password confirmation does not match.",
                ApplicationErrorCodes.ValidationFailed);
        }
    }

    private static void ValidatePasswordPolicy(string password)
    {
        if (PlatformAdminBootstrapValidation.ValidatePassword(password).Count > 0)
        {
            throw new BadRequestApplicationException(
                "Password does not meet the password policy.",
                ApplicationErrorCodes.ValidationFailed);
        }
    }

    private static void EnsureSetupTokenCanBeUsed(UserSetupTokenEntity? setupToken, DateTimeOffset utcNow)
    {
        if (setupToken is null ||
            setupToken.ConsumedAt is not null ||
            setupToken.ExpiresAt <= utcNow ||
            setupToken.AppUser.IsDeleted ||
            setupToken.AppUser.IsActive)
        {
            throw new UnauthorizedApplicationException(
                InvalidSetupTokenMessage,
                ApplicationErrorCodes.UserSetupTokenInvalid);
        }
    }

    private sealed record GeneratedSetupToken(string PlainToken, UserSetupTokenEntity Entity);
}
