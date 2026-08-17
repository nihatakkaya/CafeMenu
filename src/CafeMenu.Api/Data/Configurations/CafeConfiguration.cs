using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeMenu.Api.Data.Configurations;

public sealed class CafeConfiguration : IEntityTypeConfiguration<CafeEntity>
{
    public void Configure(EntityTypeBuilder<CafeEntity> builder)
    {
        builder.ToTable("cafe");

        builder.HasKey(cafe => cafe.Id)
            .HasName("pk_cafe");

        builder.Property(cafe => cafe.Id)
            .HasColumnName("id");

        builder.Property(cafe => cafe.Name)
            .HasColumnName("name")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(cafe => cafe.Slug)
            .HasColumnName("slug")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(cafe => cafe.LogoImageUrl)
            .HasColumnName("logo_image_url")
            .HasMaxLength(500);

        builder.Property(cafe => cafe.CoverImageUrl)
            .HasColumnName("cover_image_url")
            .HasMaxLength(500);

        builder.Property(cafe => cafe.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(cafe => cafe.IsPublished)
            .HasColumnName("is_published")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(cafe => cafe.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(cafe => cafe.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(cafe => cafe.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(cafe => cafe.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(cafe => cafe.Slug)
            .IsUnique()
            .HasDatabaseName("uk_cafe_slug");
    }
}
