using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeMenu.Api.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<CategoryEntity>
{
    public void Configure(EntityTypeBuilder<CategoryEntity> builder)
    {
        builder.ToTable("category");

        builder.HasKey(category => category.Id)
            .HasName("pk_category");

        builder.Property(category => category.Id)
            .HasColumnName("id");

        builder.Property(category => category.CafeId)
            .HasColumnName("cafe_id")
            .IsRequired();

        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(category => category.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(category => category.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500);

        builder.Property(category => category.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        builder.Property(category => category.IsVisible)
            .HasColumnName("is_visible")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(category => category.IsPublished)
            .HasColumnName("is_published")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(category => category.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(category => category.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(category => category.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(category => category.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(category => category.CafeId)
            .HasDatabaseName("idx_category_cafe");

        builder.HasIndex(category => new { category.CafeId, category.DisplayOrder })
            .HasDatabaseName("idx_category_cafe_display_order");

        builder.HasOne(category => category.Cafe)
            .WithMany(cafe => cafe.Categories)
            .HasForeignKey(category => category.CafeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_category_cafe");
    }
}
