using Microsoft.Extensions.Hosting;

namespace CafeMenu.Api.Bootstrap;

public sealed class PlatformAdminBootstrapRunner
{
    private const int SuccessExitCode = 0;
    private const int ValidationFailureExitCode = 2;
    private const int OperationFailureExitCode = 1;

    private readonly IHostEnvironment _environment;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConsolePasswordReader _passwordReader;

    public PlatformAdminBootstrapRunner(
        IHostEnvironment environment,
        IServiceScopeFactory serviceScopeFactory,
        IConsolePasswordReader passwordReader)
    {
        _environment = environment;
        _serviceScopeFactory = serviceScopeFactory;
        _passwordReader = passwordReader;
    }

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            Console.Error.WriteLine("Platform admin bootstrap is allowed only in the Development environment.");
            return ValidationFailureExitCode;
        }

        var parseResult = PlatformAdminBootstrapCommand.Parse(args);
        if (!parseResult.IsSuccess || parseResult.Email is null)
        {
            Console.Error.WriteLine(parseResult.ErrorMessage);
            Console.Error.WriteLine(PlatformAdminBootstrapCommand.Usage);
            return ValidationFailureExitCode;
        }

        var email = PlatformAdminBootstrapValidation.NormalizeEmail(parseResult.Email);
        if (!PlatformAdminBootstrapValidation.IsValidEmail(email))
        {
            Console.Error.WriteLine("Email is invalid.");
            return ValidationFailureExitCode;
        }

        string password;
        string passwordConfirmation;

        try
        {
            password = _passwordReader.ReadPassword("Password: ");
            passwordConfirmation = _passwordReader.ReadPassword("Confirm password: ");
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return ValidationFailureExitCode;
        }

        if (!string.Equals(password, passwordConfirmation, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Password confirmation does not match.");
            return ValidationFailureExitCode;
        }

        var passwordErrors = PlatformAdminBootstrapValidation.ValidatePassword(password);
        if (passwordErrors.Count > 0)
        {
            Console.Error.WriteLine("Password does not meet the bootstrap password policy.");
            foreach (var passwordError in passwordErrors)
            {
                Console.Error.WriteLine($"- {passwordError}");
            }

            return ValidationFailureExitCode;
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var bootstrapService = scope.ServiceProvider.GetRequiredService<IPlatformAdminBootstrapService>();
        var result = await bootstrapService.BootstrapAsync(
            new PlatformAdminBootstrapRequest(email, password),
            cancellationToken);

        return WriteResult(result);
    }

    private static int WriteResult(PlatformAdminBootstrapResult result)
    {
        switch (result.Status)
        {
            case PlatformAdminBootstrapStatus.Created:
                Console.WriteLine($"Platform admin created. UserId={result.UserId}; Email={result.Email}");
                return SuccessExitCode;

            case PlatformAdminBootstrapStatus.AlreadyExists:
                Console.WriteLine($"User already exists. UserId={result.UserId}; Email={result.Email}. No changes were made.");
                return SuccessExitCode;

            case PlatformAdminBootstrapStatus.InvalidEmail:
            case PlatformAdminBootstrapStatus.InvalidPassword:
                Console.Error.WriteLine(result.Message);
                return ValidationFailureExitCode;

            case PlatformAdminBootstrapStatus.PlatformAdminRoleMissing:
                Console.Error.WriteLine(result.Message);
                return OperationFailureExitCode;

            default:
                Console.Error.WriteLine("Platform admin bootstrap failed.");
                return OperationFailureExitCode;
        }
    }
}
