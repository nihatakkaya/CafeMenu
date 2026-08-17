using CafeMenu.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Data.Configurations;

public static class AppUserRoleConfiguration
{
    public static void ConfigureAppUserRole(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUserEntity>()
            .HasMany(user => user.Roles)
            .WithMany(role => role.Users)
            .UsingEntity<Dictionary<string, object>>(
                "app_user_role",
                right => right.HasOne<RoleEntity>()
                    .WithMany()
                    .HasForeignKey("role_id")
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_app_user_role_role"),
                left => left.HasOne<AppUserEntity>()
                    .WithMany()
                    .HasForeignKey("app_user_id")
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_app_user_role_app_user"),
                join =>
                {
                    join.ToTable("app_user_role");
                    join.HasKey("app_user_id", "role_id")
                        .HasName("pk_app_user_role");
                    join.HasIndex("role_id")
                        .HasDatabaseName("idx_app_user_role_role");
                    join.HasIndex("app_user_id")
                        .HasDatabaseName("idx_app_user_role_app_user");
                });
    }
}
