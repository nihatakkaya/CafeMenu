using CafeMenu.Api.DTOs.Responses;
using CafeMenu.Api.Entities;
using Riok.Mapperly.Abstractions;

namespace CafeMenu.Api.Mappings;

[Mapper]
public partial class ProductMapper
{
    [MapperIgnoreSource(nameof(ProductEntity.IsDeleted))]
    [MapperIgnoreSource(nameof(ProductEntity.DeletedAt))]
    [MapperIgnoreSource(nameof(ProductEntity.Cafe))]
    [MapperIgnoreSource(nameof(ProductEntity.Category))]
    public partial ProductResponseDto ToResponse(ProductEntity product);
}
