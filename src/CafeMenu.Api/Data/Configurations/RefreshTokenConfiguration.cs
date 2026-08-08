using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeMenu.Api.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable("refresh_token");

        builder.HasKey(refreshToken => refreshToken.Id)
            .HasName("pk_refresh_token");

        builder.Property(refreshToken => refreshToken.Id)
            .HasColumnName("id");

        builder.Property(refreshToken => refreshToken.AppUserId)
            .HasColumnName("app_user_id")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(refreshToken => refreshToken.ReplacedByTokenHash)
            .HasColumnName("replaced_by_token_hash")
            .HasMaxLength(128);

        builder.Property(refreshToken => refreshToken.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(refreshToken => refreshToken.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(refreshToken => refreshToken.AppUserId)
            .HasDatabaseName("idx_refresh_token_app_user");

        builder.HasIndex(refreshToken => refreshToken.TokenHash)
            .IsUnique()
            .HasDatabaseName("uk_refresh_token_token_hash");

        builder.HasOne(refreshToken => refreshToken.AppUser)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(refreshToken => refreshToken.AppUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_refresh_token_app_user");
    }
}
