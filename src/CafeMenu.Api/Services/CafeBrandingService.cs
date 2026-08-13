using CafeMenu.Api.DTOs.Requests;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using CafeMenu.Api.Exceptions;
using CafeMenu.Api.Mappings;
using CafeMenu.Api.Repositories;
using CafeMenu.Api.Security;
using CafeMenu.Api.Storage;

namespace CafeMenu.Api.Services;

public sealed class CafeBrandingService : ICafeBrandingService
{
    private static readonly string[] CafeBrandingManagerRoles = [ApplicationRoles.CafeOwner, ApplicationRoles.CafeManager];

    private readonly ICafeRepository _cafeRepository;
    private readonly ICafeThemeRepository _cafeThemeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantAuthorizationService _tenantAuthorizationService;
    private readonly IImageStorage _imageStorage;
    private readonly CafeThemeMapper _cafeThemeMapper;
    private readonly ILogger<CafeBrandingService> _logger;

    public CafeBrandingService(
        ICafeRepository cafeRepository,
        ICafeThemeRepository cafeThemeRepository,
        IUnitOfWork unitOfWork,
        ITenantAuthorizationService tenantAuthorizationService,
        IImageStorage imageStorage,
        CafeThemeMapper cafeThemeMapper,
        ILogger<CafeBrandingService> logger)
    {
        _cafeRepository = cafeRepository;
        _cafeThemeRepository = cafeThemeRepository;
        _unitOfWork = unitOfWork;
        _tenantAuthorizationService = tenantAuthorizationService;
        _imageStorage = imageStorage;
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

    public Task<CafeBrandingResponseDto> UploadLogoImageAsync(
        long appUserId,
        long cafeId,
        ImageUploadInput input,
        CancellationToken cancellationToken)
    {
        return ReplaceCafeImageAsync(
            appUserId,
            cafeId,
            input,
            ImageStorageFolder.CafeLogos,
            cafe => cafe.LogoImageUrl,
            (cafe, imageUrl) => cafe.LogoImageUrl = imageUrl,
            "Cafe logo uploaded for cafe {CafeId}",
            cancellationToken);
    }

    public Task<CafeBrandingResponseDto> UploadCoverImageAsync(
        long appUserId,
        long cafeId,
        ImageUploadInput input,
        CancellationToken cancellationToken)
    {
        return ReplaceCafeImageAsync(
            appUserId,
            cafeId,
            input,
            ImageStorageFolder.CafeCovers,
            cafe => cafe.CoverImageUrl,
            (cafe, imageUrl) => cafe.CoverImageUrl = imageUrl,
            "Cafe cover uploaded for cafe {CafeId}",
            cancellationToken);
    }

    public Task<CafeBrandingResponseDto> RemoveLogoImageAsync(
        long appUserId,
        long cafeId,
        CancellationToken cancellationToken)
    {
        return RemoveCafeImageAsync(
            appUserId,
            cafeId,
            cafe => cafe.LogoImageUrl,
            (cafe, imageUrl) => cafe.LogoImageUrl = imageUrl,
            "Cafe logo removed for cafe {CafeId}",
            cancellationToken);
    }

    public Task<CafeBrandingResponseDto> RemoveCoverImageAsync(
        long appUserId,
        long cafeId,
        CancellationToken cancellationToken)
    {
        return RemoveCafeImageAsync(
            appUserId,
            cafeId,
            cafe => cafe.CoverImageUrl,
            (cafe, imageUrl) => cafe.CoverImageUrl = imageUrl,
            "Cafe cover removed for cafe {CafeId}",
            cancellationToken);
    }

    private async Task<CafeBrandingResponseDto> ReplaceCafeImageAsync(
        long appUserId,
        long cafeId,
        ImageUploadInput input,
        ImageStorageFolder folder,
        Func<CafeEntity, string?> getCurrentImageUrl,
        Action<CafeEntity, string?> setImageUrl,
        string logMessage,
        CancellationToken cancellationToken)
    {
        var cafe = await EnsureCafeBrandingManagementAccessAsync(appUserId, cafeId, cancellationToken);
        var oldImageUrl = getCurrentImageUrl(cafe);
        StoredImage? storedImage = null;

        try
        {
            storedImage = await _imageStorage.StoreAsync(input, folder, cancellationToken);
            setImageUrl(cafe, storedImage.PublicUrl);
            cafe.UpdatedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (storedImage is not null)
            {
                await TryDeleteManagedImageAsync(storedImage.PublicUrl, cancellationToken);
            }

            throw;
        }

        await TryDeleteManagedImageAsync(oldImageUrl, cancellationToken);
        _logger.LogInformation(logMessage, cafe.Id);

        var theme = await _cafeThemeRepository.GetByCafeIdAsync(cafe.Id, cancellationToken);
        return _cafeThemeMapper.ToResponse(cafe, theme);
    }

    private async Task<CafeBrandingResponseDto> RemoveCafeImageAsync(
        long appUserId,
        long cafeId,
        Func<CafeEntity, string?> getCurrentImageUrl,
        Action<CafeEntity, string?> setImageUrl,
        string logMessage,
        CancellationToken cancellationToken)
    {
        var cafe = await EnsureCafeBrandingManagementAccessAsync(appUserId, cafeId, cancellationToken);
        var oldImageUrl = getCurrentImageUrl(cafe);

        setImageUrl(cafe, null);
        cafe.UpdatedAt = DateTimeOffset.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TryDeleteManagedImageAsync(oldImageUrl, cancellationToken);
        _logger.LogInformation(logMessage, cafe.Id);

        var theme = await _cafeThemeRepository.GetByCafeIdAsync(cafe.Id, cancellationToken);
        return _cafeThemeMapper.ToResponse(cafe, theme);
    }

    private async Task TryDeleteManagedImageAsync(string? imageUrl, CancellationToken cancellationToken)
    {
        try
        {
            await _imageStorage.DeleteIfManagedAsync(imageUrl, cancellationToken);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Managed cafe branding image cleanup failed.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Managed cafe branding image cleanup failed.");
        }
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
