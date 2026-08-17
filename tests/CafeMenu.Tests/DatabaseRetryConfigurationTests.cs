using CafeMenu.Api.Configuration;
using CafeMenu.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CafeMenu.Tests;

public sealed class DatabaseRetryConfigurationTests
{
    [Fact]
    public void Validator_ShouldAllowDefaultRetryConfiguration()
    {
        var result = Validate(new DatabaseOptions());

        AssertValidationSucceeded(result);
    }

    [Fact]
    public void Validator_ShouldAllowEnabledRetryWithValidValues()
    {
        var result = Validate(new DatabaseOptions
        {
            Retry = new DatabaseRetryOptions
            {
                Enabled = true,
                MaxRetryCount = 3,
                MaxRetryDelaySeconds = 5
            }
        });

        AssertValidationSucceeded(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_ShouldRejectNonPositiveMaxRetryCountWhenEnabled(int maxRetryCount)
    {
        var result = Validate(new DatabaseOptions
        {
            Retry = new DatabaseRetryOptions
            {
                Enabled = true,
                MaxRetryCount = maxRetryCount,
                MaxRetryDelaySeconds = 5
            }
        });

        AssertValidationFailed(result, "Database:Retry:MaxRetryCount");
    }

    [Fact]
    public void Validator_ShouldRejectMaxRetryCountAboveUpperLimitWhenEnabled()
    {
        var result = Validate(new DatabaseOptions
        {
            Retry = new DatabaseRetryOptions
            {
                Enabled = true,
                MaxRetryCount = DatabaseOptionsValidator.MaximumRetryCount + 1,
                MaxRetryDelaySeconds = 5
            }
        });

        AssertValidationFailed(result, "Database:Retry:MaxRetryCount");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_ShouldRejectNonPositiveMaxRetryDelayWhenEnabled(int maxRetryDelaySeconds)
    {
        var result = Validate(new DatabaseOptions
        {
            Retry = new DatabaseRetryOptions
            {
                Enabled = true,
                MaxRetryCount = 3,
                MaxRetryDelaySeconds = maxRetryDelaySeconds
            }
        });

        AssertValidationFailed(result, "Database:Retry:MaxRetryDelaySeconds");
    }

    [Fact]
    public void Validator_ShouldRejectMaxRetryDelayAboveUpperLimitWhenEnabled()
    {
        var result = Validate(new DatabaseOptions
        {
            Retry = new DatabaseRetryOptions
            {
                Enabled = true,
                MaxRetryCount = 3,
                MaxRetryDelaySeconds = DatabaseOptionsValidator.MaximumRetryDelaySeconds + 1
            }
        });

        AssertValidationFailed(result, "Database:Retry:MaxRetryDelaySeconds");
    }

    [Fact]
    public void Validator_ShouldAllowInvalidRetryNumbersWhenRetryIsDisabled()
    {
        var result = Validate(new DatabaseOptions
        {
            Retry = new DatabaseRetryOptions
            {
                Enabled = false,
                MaxRetryCount = 0,
                MaxRetryDelaySeconds = 0
            }
        });

        AssertValidationSucceeded(result);
    }

    [Fact]
    public void Registration_ShouldUseNpgsqlRetryingExecutionStrategyWhenEnabled()
    {
        using var serviceProvider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Database:Retry:Enabled"] = "true",
            ["Database:Retry:MaxRetryCount"] = "4",
            ["Database:Retry:MaxRetryDelaySeconds"] = "7"
        });

        var strategy = CreateExecutionStrategy(serviceProvider);

        Assert.Contains("NpgsqlRetryingExecutionStrategy", strategy.GetType().Name, StringComparison.Ordinal);
        Assert.Equal(4, GetExecutionStrategyMaxRetryCount(strategy));
        Assert.Equal(TimeSpan.FromSeconds(7), GetExecutionStrategyMaxRetryDelay(strategy));
    }

    [Fact]
    public void Registration_ShouldUseDefaultNonRetryingExecutionStrategyWhenDisabled()
    {
        using var serviceProvider = BuildServiceProvider(new Dictionary<string, string?>
        {
            ["Database:Retry:Enabled"] = "false",
            ["Database:Retry:MaxRetryCount"] = "0",
            ["Database:Retry:MaxRetryDelaySeconds"] = "0"
        });

        var strategy = CreateExecutionStrategy(serviceProvider);

        Assert.DoesNotContain("Retrying", strategy.GetType().Name, StringComparison.Ordinal);
    }

    [Fact]
    public void Registration_ShouldFailFastWithInvalidEnabledRetryConfiguration()
    {
        var exception = Assert.Throws<OptionsValidationException>(() =>
            BuildServiceProvider(new Dictionary<string, string?>
            {
                ["Database:Retry:Enabled"] = "true",
                ["Database:Retry:MaxRetryCount"] = "0",
                ["Database:Retry:MaxRetryDelaySeconds"] = "5"
            }));

        Assert.Contains("Database:Retry:MaxRetryCount", exception.Message, StringComparison.Ordinal);
    }

    private static ValidateOptionsResult Validate(DatabaseOptions options)
    {
        return new DatabaseOptionsValidator().Validate(null, options);
    }

    private static ServiceProvider BuildServiceProvider(IReadOnlyDictionary<string, string?> configurationOverrides)
    {
        var configurationValues = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=cafemenu_test;Username=test;Password=test"
        };

        foreach (var item in configurationOverrides)
        {
            configurationValues[item.Key] = item.Value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var services = new ServiceCollection();
        services.AddApplicationDatabase(configuration);

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static IExecutionStrategy CreateExecutionStrategy(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CafeMenuDbContext>();

        return dbContext.Database.CreateExecutionStrategy();
    }

    private static int GetExecutionStrategyMaxRetryCount(IExecutionStrategy strategy)
    {
        var property = strategy.GetType().GetProperty("MaxRetryCount")
            ?? strategy.GetType().BaseType?.GetProperty("MaxRetryCount");

        Assert.NotNull(property);
        return Assert.IsType<int>(property.GetValue(strategy));
    }

    private static TimeSpan GetExecutionStrategyMaxRetryDelay(IExecutionStrategy strategy)
    {
        var property = strategy.GetType().GetProperty("MaxRetryDelay")
            ?? strategy.GetType().BaseType?.GetProperty("MaxRetryDelay");

        Assert.NotNull(property);
        return Assert.IsType<TimeSpan>(property.GetValue(strategy));
    }

    private static void AssertValidationSucceeded(ValidateOptionsResult result)
    {
        Assert.False(result.Failed, result.FailureMessage);
    }

    private static void AssertValidationFailed(ValidateOptionsResult result, string expectedFailure)
    {
        Assert.True(result.Failed);
        Assert.Contains(expectedFailure, string.Join(" ", result.Failures), StringComparison.Ordinal);
    }
}
