namespace CafeMenu.Api.Bootstrap;

public static class PlatformAdminBootstrapCommand
{
    public const string CommandName = "--bootstrap-platform-admin";
    private const string EmailOptionName = "--email";

    public const string Usage =
        "Usage: dotnet run --project src/CafeMenu.Api -- --bootstrap-platform-admin --email admin@example.local";

    public static bool IsRequested(string[] args)
    {
        return args.Any(argument => string.Equals(argument, CommandName, StringComparison.OrdinalIgnoreCase));
    }

    public static PlatformAdminBootstrapCommandParseResult Parse(string[] args)
    {
        if (!IsRequested(args))
        {
            return PlatformAdminBootstrapCommandParseResult.Failure("Bootstrap command was not requested.");
        }

        string? email = null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            if (argument.StartsWith("--password", StringComparison.OrdinalIgnoreCase))
            {
                return PlatformAdminBootstrapCommandParseResult.Failure(
                    "Password must not be provided as a command-line argument.");
            }

            if (argument.StartsWith($"{EmailOptionName}=", StringComparison.OrdinalIgnoreCase))
            {
                email = argument[(EmailOptionName.Length + 1)..];
                continue;
            }

            if (!string.Equals(argument, EmailOptionName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index + 1 >= args.Length)
            {
                return PlatformAdminBootstrapCommandParseResult.Failure("Missing email value.");
            }

            email = args[index + 1];
            index++;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return PlatformAdminBootstrapCommandParseResult.Failure("Missing required --email option.");
        }

        return PlatformAdminBootstrapCommandParseResult.Success(email);
    }
}
