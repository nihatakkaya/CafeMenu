using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CafeMenu.Api.Data.Configurations;

public sealed class UserSetupTokenConfiguration : IEntityTypeConfiguration<UserSetupTokenEntity>
{
    public void Configure(EntityTypeBuilder<UserSetupTokenEntity> builder)
    {
        builder.ToTable("user_setup_token");

        builder.HasKey(token => token.Id)
            .HasName("pk_user_setup_token");

        builder.Property(token => token.Id)
            .HasColumnName("id");

        builder.Property(token => token.AppUserId)
            .HasColumnName("app_user_id")
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(token => token.ConsumedAt)
            .HasColumnName("consumed_at");

        builder.Property(token => token.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(token => token.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(token => token.AppUserId)
            .HasDatabaseName("idx_user_setup_token_app_user");

        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("uk_user_setup_token_token_hash");

        builder.HasOne(token => token.AppUser)
            .WithMany()
            .HasForeignKey(token => token.AppUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_user_setup_token_app_user");
    }
}
