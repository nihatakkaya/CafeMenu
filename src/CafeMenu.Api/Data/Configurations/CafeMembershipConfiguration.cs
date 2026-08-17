using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeMenu.Api.Data.Configurations;

public sealed class CafeMembershipConfiguration : IEntityTypeConfiguration<CafeMembershipEntity>
{
    public void Configure(EntityTypeBuilder<CafeMembershipEntity> builder)
    {
        builder.ToTable("cafe_membership");

        builder.HasKey(membership => membership.Id)
            .HasName("pk_cafe_membership");

        builder.Property(membership => membership.Id)
            .HasColumnName("id");

        builder.Property(membership => membership.AppUserId)
            .HasColumnName("app_user_id")
            .IsRequired();

        builder.Property(membership => membership.CafeId)
            .HasColumnName("cafe_id")
            .IsRequired();

        builder.Property(membership => membership.RoleId)
            .HasColumnName("role_id")
            .IsRequired();

        builder.Property(membership => membership.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(membership => membership.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(membership => membership.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(membership => membership.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(membership => membership.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasIndex(membership => membership.CafeId)
            .HasDatabaseName("idx_cafe_membership_cafe");

        builder.HasIndex(membership => membership.RoleId)
            .HasDatabaseName("idx_cafe_membership_role");

        builder.HasIndex(membership => new { membership.AppUserId, membership.CafeId })
            .IsUnique()
            .HasFilter("is_active = true AND is_deleted = false")
            .HasDatabaseName("uk_cafe_membership_app_user_cafe_active");

        builder.HasOne(membership => membership.AppUser)
            .WithMany(user => user.CafeMemberships)
            .HasForeignKey(membership => membership.AppUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cafe_membership_app_user");

        builder.HasOne(membership => membership.Cafe)
            .WithMany(cafe => cafe.Memberships)
            .HasForeignKey(membership => membership.CafeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cafe_membership_cafe");

        builder.HasOne(membership => membership.Role)
            .WithMany(role => role.CafeMemberships)
            .HasForeignKey(membership => membership.RoleId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_cafe_membership_role");
    }
}
