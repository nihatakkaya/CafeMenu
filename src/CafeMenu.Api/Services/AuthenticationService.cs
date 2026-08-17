using CafeMenu.Api.Data;
using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Mappings;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IAppUserRepository _appUserRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly AppUserMapper _appUserMapper;
    private readonly CafeMenuDbContext _dbContext;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IAppUserRepository appUserRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        AppUserMapper appUserMapper,
        CafeMenuDbContext dbContext,
        ILogger<AuthenticationService> logger)
    {
        _appUserRepository = appUserRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _appUserMapper = appUserMapper;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _appUserRepository.GetByEmailWithRolesAsync(email, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedApplicationException("Invalid email or password.");
        }

        var utcNow = DateTimeOffset.UtcNow;
        user.LastLoginAt = utcNow;
        user.UpdatedAt = utcNow;

        var authResponse = await IssueTokenPairAsync(user, utcNow, cancellationToken);

        _logger.LogInformation("User {UserId} logged in", user.Id);

        return authResponse;
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            var utcNow = DateTimeOffset.UtcNow;
            var existingTokenHash = RefreshTokenGenerator.Hash(request.RefreshToken);
            var existingToken = await _refreshTokenRepository.GetByTokenHashWithUserAsync(existingTokenHash, cancellationToken);

            if (existingToken is null || !existingToken.IsActive(utcNow) || !existingToken.AppUser.IsActive)
            {
                throw new UnauthorizedApplicationException("Refresh token is invalid or expired.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var refreshToken = _tokenService.CreateRefreshToken(existingToken.AppUserId, utcNow);

            existingToken.RevokedAt = utcNow;
            existingToken.ReplacedByTokenHash = refreshToken.Entity.TokenHash;
            existingToken.UpdatedAt = utcNow;

            await _refreshTokenRepository.AddAsync(refreshToken.Entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var accessToken = _tokenService.CreateAccessToken(existingToken.AppUser);

            return new AuthResponseDto(
                accessToken.Token,
                refreshToken.Token,
                accessToken.ExpiresAt,
                refreshToken.Entity.ExpiresAt,
                _appUserMapper.ToResponse(existingToken.AppUser));
        });
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var tokenHash = RefreshTokenGenerator.Hash(request.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashWithUserAsync(tokenHash, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive(utcNow))
        {
            throw new UnauthorizedApplicationException("Refresh token is invalid or expired.");
        }

        refreshToken.RevokedAt = utcNow;
        refreshToken.UpdatedAt = utcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {UserId} logged out", refreshToken.AppUserId);
    }

    public async Task<UserResponseDto> GetCurrentUserAsync(long appUserId, CancellationToken cancellationToken)
    {
        var user = await _appUserRepository.GetByIdWithRolesAsync(appUserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedApplicationException("User is not authorized.");
        }

        return _appUserMapper.ToResponse(user);
    }

    private async Task<AuthResponseDto> IssueTokenPairAsync(
        AppUserEntity user,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var refreshToken = _tokenService.CreateRefreshToken(user.Id, utcNow);
        await _refreshTokenRepository.AddAsync(refreshToken.Entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.CreateAccessToken(user);

        return new AuthResponseDto(
            accessToken.Token,
            refreshToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Entity.ExpiresAt,
            _appUserMapper.ToResponse(user));
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
