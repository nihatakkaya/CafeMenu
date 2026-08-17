using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CafeMenu.Api.Data;

public sealed class CafeMenuDbContextFactory : IDesignTimeDbContextFactory<CafeMenuDbContext>
{
    public CafeMenuDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<CafeMenuDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history"));

        return new CafeMenuDbContext(optionsBuilder.Options);
    }

    private static IConfiguration BuildConfiguration()
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        return new ConfigurationBuilder()
            .SetBasePath(ResolveProjectDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ResolveProjectDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory is not null)
        {
            var directProjectPath = Path.Combine(directory.FullName, "CafeMenu.Api.csproj");
            if (File.Exists(directProjectPath))
            {
                return directory.FullName;
            }

            var nestedProjectDirectory = Path.Combine(directory.FullName, "src", "CafeMenu.Api");
            var nestedProjectPath = Path.Combine(nestedProjectDirectory, "CafeMenu.Api.csproj");
            if (File.Exists(nestedProjectPath))
            {
                return nestedProjectDirectory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("CafeMenu.Api project directory could not be located.");
    }
}
