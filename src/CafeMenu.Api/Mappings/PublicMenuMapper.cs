using CafeMenu.Api.Common;
using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace CafeMenu.Api.Mappings;

[Mapper]
public partial class PublicMenuMapper
{
    public PublicMenuResponseDto ToResponse(CafeEntity cafe)
    {
        var theme = cafe.Theme is { IsPublished: true }
            ? cafe.Theme
            : CafeThemeMapper.CreateDefaultTheme(cafe.Id);

        return new PublicMenuResponseDto(
            cafe.Name,
            cafe.Slug,
            cafe.LogoImageUrl,
            cafe.CoverImageUrl,
            ToThemeResponse(theme),
            cafe.Categories
                .Where(category => category.CafeId == cafe.Id &&
                    category.IsVisible &&
                    category.IsPublished &&
                    !category.IsDeleted)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .Select(category => ToCategoryResponse(category, cafe.Id))
                .ToArray());
    }

    private static PublicMenuThemeResponseDto ToThemeResponse(CafeThemeEntity theme)
    {
        return new PublicMenuThemeResponseDto(
            theme.PrimaryColor,
            theme.SecondaryColor,
            theme.AccentColor,
            theme.BackgroundColor,
            theme.TextColor,
            theme.WelcomeTitle,
            theme.WelcomeDescription,
            theme.FontPreset,
            theme.ThemePreset);
    }

    private static PublicMenuCategoryResponseDto ToCategoryResponse(CategoryEntity category, long cafeId)
    {
        return new PublicMenuCategoryResponseDto(
            category.Id,
            category.Name,
            category.Description,
            category.ImageUrl,
            category.DisplayOrder,
            category.Products
                .Where(product => product.CafeId == cafeId &&
                    product.CategoryId == category.Id &&
                    product.IsVisible &&
                    product.IsPublished &&
                    !product.IsDeleted)
                .OrderBy(product => product.DisplayOrder)
                .ThenBy(product => product.Name)
                .Select(ToProductResponse)
                .ToArray());
    }

    private static PublicMenuProductResponseDto ToProductResponse(ProductEntity product)
    {
        return new PublicMenuProductResponseDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.ImageUrl,
            product.IsAvailable,
            product.DisplayOrder);
    }
}
