using Microsoft.EntityFrameworkCore;

namespace CafeMenu.Api.Data;

public sealed class CafeMenuDbContext : DbContext
{
    public CafeMenuDbContext(DbContextOptions<CafeMenuDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("public");
    }
}
