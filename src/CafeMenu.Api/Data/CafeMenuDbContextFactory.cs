using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CafeMenu.Api.Data;

public sealed class CafeMenuDbContextFactory : IDesignTimeDbContextFactory<CafeMenuDbContext>
{
    public CafeMenuDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CafeMenuDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=cafemenu;Username=cafemenu_user;Password=change_me_for_local_dev");

        return new CafeMenuDbContext(optionsBuilder.Options);
    }
}
