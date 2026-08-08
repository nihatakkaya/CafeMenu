using Microsoft.EntityFrameworkCore;
using CafeMenu.Api.Data.Configurations;
using CafeMenu.Api.Entities;

namespace CafeMenu.Api.Data;

public sealed class CafeMenuDbContext : DbContext
{
    public CafeMenuDbContext(DbContextOptions<CafeMenuDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUserEntity> AppUsers => Set<AppUserEntity>();

    public DbSet<RoleEntity> Roles => Set<RoleEntity>();

    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfiguration(new AppUserConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ConfigureAppUserRole();
    }
}
