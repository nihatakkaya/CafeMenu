using CafeMenu.Web.Configuration;
using Microsoft.Extensions.Options;

namespace CafeMenu.Web.AdminAuth;

public sealed class AdminApiOptionsValidator : IValidateOptions<AdminApiOptions>
{
    private readonly IWebHostEnvironment _environment;

    public AdminApiOptionsValidator(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, AdminApiOptions options)
    {
        return HttpBaseUrlOptionsValidator.Validate("AdminApi", options.BaseUrl, _environment);
    }
}
