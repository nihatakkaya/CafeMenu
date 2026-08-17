using CafeMenu.Web.Configuration;
using Microsoft.Extensions.Options;

namespace CafeMenu.Web.PublicMenu;

public sealed class PublicApiOptionsValidator : IValidateOptions<PublicMenuApiOptions>
{
    private readonly IWebHostEnvironment _environment;

    public PublicApiOptionsValidator(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, PublicMenuApiOptions options)
    {
        return HttpBaseUrlOptionsValidator.Validate("PublicApi", options.BaseUrl, _environment);
    }
}
