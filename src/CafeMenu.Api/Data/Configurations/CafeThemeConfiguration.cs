using CafeMenu.Api.Common;
using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeMenu.Api.Data.Configurations;

public sealed class CafeThemeConfiguration : IEntityTypeConfiguration<CafeThemeEntity>
{
    public void Configure(EntityTypeBuilder<CafeThemeEntity> builder)
    {
        builder.ToTable("cafe_theme");

        builder.HasKey(theme => theme.Id)
            .HasName("pk_cafe_theme");

        builder.Property(theme => theme.Id)
            .HasColumnName("id");

        builder.Property(theme => theme.CafeId)
            .HasColumnName("cafe_id")
            .IsRequired();

        builder.Property(theme => theme.PrimaryColor)
            .HasColumnName("primary_color")
            .HasMaxLength(7)
            .HasDefaultValue(CafeThemeConstants.DefaultPrimaryColor)
            .IsRequired();

        builder.Property(theme => theme.SecondaryColor)
            .HasColumnName("secondary_color")
            .HasMaxLength(7)
            .HasDefaultValue(CafeThemeConstants.DefaultSecondaryColor)
            .IsRequired();

        builder.Property(theme => theme.AccentColor)
            .HasColumnName("accent_color")
            .HasMaxLength(7)
            .HasDefaultValue(CafeThemeConstants.DefaultAccentColor)
            .IsRequired();

        builder.Property(theme => theme.BackgroundColor)
            .HasColumnName("background_color")
            .HasMaxLength(7)
            .HasDefaultValue(CafeThemeConstants.DefaultBackgroundColor)
            .IsRequired();

        builder.Property(theme => theme.TextColor)
            .HasColumnName("text_color")
            .HasMaxLength(7)
            .HasDefaultValue(CafeThemeConstants.DefaultTextColor)
            .IsRequired();

        builder.Property(theme => theme.WelcomeTitle)
            .HasColumnName("welcome_title")
            .HasMaxLength(120);

        builder.Property(theme => theme.WelcomeDescription)
            .HasColumnName("welcome_description")
            .HasMaxLength(500);

        builder.Property(theme => theme.FontPreset)
            .HasColumnName("font_preset")
            .HasMaxLength(20)
            .HasDefaultValue(CafeThemeConstants.SystemFontPreset)
            .IsRequired();

        builder.Property(theme => theme.ThemePreset)
            .HasColumnName("theme_preset")
            .HasMaxLength(20)
            .HasDefaultValue(CafeThemeConstants.ClassicThemePreset)
            .IsRequired();

        builder.Property(theme => theme.IsPublished)
            .HasColumnName("is_published")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(theme => theme.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(theme => theme.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(theme => theme.CafeId)
            .IsUnique()
            .HasDatabaseName("uk_cafe_theme_cafe");

        builder.HasOne(theme => theme.Cafe)
            .WithOne(cafe => cafe.Theme)
            .HasForeignKey<CafeThemeEntity>(theme => theme.CafeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cafe_theme_cafe");
    }
}
