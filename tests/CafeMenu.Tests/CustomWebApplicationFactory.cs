using CafeMenu.Api.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.TestHost;

namespace CafeMenu.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"cafemenu_tests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=cafemenu_test;Username=test;Password=test"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            var dbContextDescriptors = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(CafeMenuDbContext) ||
                    descriptor.ServiceType == typeof(DbContextOptions) ||
                    descriptor.ServiceType == typeof(DbContextOptions<CafeMenuDbContext>) ||
                    descriptor.ServiceType.Name.Contains("DbContextOptionsConfiguration", StringComparison.Ordinal))
                .ToArray();

            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<CafeMenuDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            var keyDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "cafemenu-test-data-protection"));
            services.AddDataProtection()
                .PersistKeysToFileSystem(keyDirectory);
        });
    }
}
