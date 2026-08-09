using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace CafeMenu.Api.Mappings;

[Mapper]
public partial class CategoryMapper
{
    [MapperIgnoreSource(nameof(CategoryEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(CategoryEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(CategoryEntity.Cafe))]
    [MapperIgnoreSource(nameof(CategoryEntity.Products))]
    public partial CategoryResponseDto ToResponse(CategoryEntity category);
}
