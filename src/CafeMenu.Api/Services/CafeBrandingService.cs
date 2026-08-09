using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Mappings;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;

namespace CafeMenu.Api.Services;

public sealed class CafeBrandingService : ICafeBrandingService
{
    private static readonly string[] CafeBrandingManagerRoles = [ApplicationRoles.CafeOwner, ApplicationRoles.CafeManager];

    private readonly ICafeRepository _cafeRepository;
    private readonly ICafeThemeRepository _cafeThemeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantAuthorizationService _tenantAuthorizationService;
    private readonly CafeThemeMapper _cafeThemeMapper;
    private readonly ILogger<CafeBrandingService> _logger;

    public CafeBrandingService(
        ICafeRepository cafeRepository,
        ICafeThemeRepository cafeThemeRepository,
        IUnitOfWork unitOfWork,
        ITenantAuthorizationService tenantAuthorizationService,
        CafeThemeMapper cafeThemeMapper,
        ILogger<CafeBrandingService> logger)
    {
        _cafeRepository = cafeRepository;
        _cafeThemeRepository = cafeThemeRepository;
        _unitOfWork = unitOfWork;
        _tenantAuthorizationService = tenantAuthorizationService;
        _cafeThemeMapper = cafeThemeMapper;
        _logger = logger;
    }

    public async Task<CafeBrandingResponseDto> GetCafeBrandingAsync(
        long appUserId,
        long cafeId,
        CancellationToken cancellationToken)
    {
        var cafe = await EnsureCafeBrandingManagementAccessAsync(appUserId, cafeId, cancellationToken);
        var theme = await _cafeThemeRepository.GetByCafeIdAsync(cafe.Id, cancellationToken);

        return _cafeThemeMapper.ToResponse(cafe, theme);
    }

    public async Task<CafeBrandingResponseDto> UpdateCafeBrandingAsync(
        long appUserId,
        long cafeId,
        UpdateCafeBrandingRequest request,
        CancellationToken cancellationToken)
    {
        var cafe = await EnsureCafeBrandingManagementAccessAsync(appUserId, cafeId, cancellationToken);
        var theme = await _cafeThemeRepository.GetByCafeIdAsync(cafe.Id, cancellationToken);
        var utcNow = DateTimeOffset.UtcNow;

        if (theme is null)
        {
            theme = CafeThemeMapper.CreateDefaultTheme(cafe.Id);
            theme.CreatedAt = utcNow;
            await _cafeThemeRepository.AddAsync(theme, cancellationToken);
        }

        cafe.LogoImageUrl = NormalizeOptionalText(request.LogoImageUrl);
        cafe.CoverImageUrl = NormalizeOptionalText(request.CoverImageUrl);
        cafe.UpdatedAt = utcNow;

        theme.PrimaryColor = NormalizeRequiredText(request.PrimaryColor);
        theme.SecondaryColor = NormalizeRequiredText(request.SecondaryColor);
        theme.AccentColor = NormalizeRequiredText(request.AccentColor);
        theme.BackgroundColor = NormalizeRequiredText(request.BackgroundColor);
        theme.TextColor = NormalizeRequiredText(request.TextColor);
        theme.WelcomeTitle = NormalizeOptionalText(request.WelcomeTitle);
        theme.WelcomeDescription = NormalizeOptionalText(request.WelcomeDescription);
        theme.FontPreset = NormalizeRequiredText(request.FontPreset);
        theme.ThemePreset = NormalizeRequiredText(request.ThemePreset);
        theme.IsPublished = request.IsPublished;
        theme.UpdatedAt = utcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Cafe branding updated for cafe {CafeId}", cafe.Id);

        return _cafeThemeMapper.ToResponse(cafe, theme);
    }

    private async Task<CafeEntity> EnsureCafeBrandingManagementAccessAsync(
        long appUserId,
        long cafeId,
        CancellationToken cancellationToken)
    {
        await _tenantAuthorizationService.EnsureCafeAccessAsync(
            appUserId,
            cafeId,
            CafeBrandingManagerRoles,
            allowPlatformAdmin: true,
            cancellationToken);

        var cafe = await _cafeRepository.GetByIdAsync(cafeId, cancellationToken)
            ?? throw new NotFoundApplicationException("Cafe was not found.", ApplicationErrorCodes.CafeNotFound);

        if (!cafe.IsActive)
        {
            throw new ForbiddenApplicationException(
                "Cafe is not active for branding management.",
                ApplicationErrorCodes.CafeInactive);
        }

        return cafe;
    }

    private static string NormalizeRequiredText(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
