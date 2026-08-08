using CafeMenu.Api.Entities;
using CafeMenu.Api.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeMenu.Api.Data.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<RoleEntity>
{
    public void Configure(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.ToTable("role");

        builder.HasKey(role => role.Id)
            .HasName("pk_role");

        builder.Property(role => role.Id)
            .HasColumnName("id");

        builder.Property(role => role.Code)
            .HasColumnName("code")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(role => role.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(role => role.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(role => role.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(role => role.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(role => role.Code)
            .IsUnique()
            .HasDatabaseName("uk_role_code");

        var seededAt = new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(
            new RoleEntity
            {
                Id = 1,
                Code = ApplicationRoles.PlatformAdmin,
                Name = "Platform Administrator",
                Description = "Manages platform-level administration.",
                CreatedAt = seededAt,
                UpdatedAt = seededAt
            },
            new RoleEntity
            {
                Id = 2,
                Code = ApplicationRoles.CafeOwner,
                Name = "Cafe Owner",
                Description = "Cafe-scoped owner role reserved for membership authorization.",
                CreatedAt = seededAt,
                UpdatedAt = seededAt
            },
            new RoleEntity
            {
                Id = 3,
                Code = ApplicationRoles.CafeManager,
                Name = "Cafe Manager",
                Description = "Cafe-scoped manager role reserved for membership authorization.",
                CreatedAt = seededAt,
                UpdatedAt = seededAt
            });
    }
}
