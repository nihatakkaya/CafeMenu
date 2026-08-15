using CafeMenu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CafeMenu.Api.Configuration;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();

        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();
        var validationResult = new DatabaseOptionsValidator().Validate(null, databaseOptions);

        if (validationResult.Failed)
        {
            throw new OptionsValidationException(
                DatabaseOptions.SectionName,
                typeof(DatabaseOptions),
                validationResult.Failures);
        }

        services.AddDbContext<CafeMenuDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history");

                    if (databaseOptions.Retry.Enabled)
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            databaseOptions.Retry.MaxRetryCount,
                            TimeSpan.FromSeconds(databaseOptions.Retry.MaxRetryDelaySeconds),
                            errorCodesToAdd: null);
                    }
                }));

        return services;
    }
}
