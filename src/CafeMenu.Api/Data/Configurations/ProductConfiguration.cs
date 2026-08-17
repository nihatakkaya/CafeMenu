using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeMenu.Api.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("product");

        builder.HasKey(product => product.Id)
            .HasName("pk_product");

        builder.Property(product => product.Id)
            .HasColumnName("id");

        builder.Property(product => product.CafeId)
            .HasColumnName("cafe_id")
            .IsRequired();

        builder.Property(product => product.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(product => product.Name)
            .HasColumnName("name")
            .HasMaxLength(180)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(product => product.Price)
            .HasColumnName("price")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(product => product.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500);

        builder.Property(product => product.IsAvailable)
            .HasColumnName("is_available")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(product => product.IsVisible)
            .HasColumnName("is_visible")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(product => product.IsPublished)
            .HasColumnName("is_published")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(product => product.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        builder.Property(product => product.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(product => product.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(product => product.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(product => product.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(product => product.CafeId)
            .HasDatabaseName("idx_product_cafe");

        builder.HasIndex(product => product.CategoryId)
            .HasDatabaseName("idx_product_category");

        builder.HasIndex(product => new { product.CafeId, product.CategoryId })
            .HasDatabaseName("idx_product_cafe_category");

        builder.HasIndex(product => new { product.CafeId, product.CategoryId, product.DisplayOrder })
            .HasDatabaseName("idx_product_cafe_category_display_order");

        builder.HasOne(product => product.Cafe)
            .WithMany(cafe => cafe.Products)
            .HasForeignKey(product => product.CafeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_cafe");

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => new { product.CafeId, product.CategoryId })
            .HasPrincipalKey(category => new { category.CafeId, category.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_product_category_cafe");
    }
}
