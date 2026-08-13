using CafeMenu.Web.Components;
using CafeMenu.Web.AccountSetup;
using CafeMenu.Web.AdminBranding;
using CafeMenu.Web.AdminCafe;
using CafeMenu.Web.AdminCafeSettings;
using CafeMenu.Web.AdminCategory;
using CafeMenu.Web.AdminPlatform;
using CafeMenu.Web.AdminProduct;
using CafeMenu.Web.AdminAuth;
using CafeMenu.Web.PublicMenu;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAdminAuthenticationInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddScoped<IAdminBrandingApiClient, AdminBrandingApiClient>();
builder.Services.AddScoped<IAdminCafeApiClient, AdminCafeApiClient>();
builder.Services.AddScoped<IAdminCafeSettingsApiClient, AdminCafeSettingsApiClient>();
builder.Services.AddScoped<IAdminCategoryApiClient, AdminCategoryApiClient>();
builder.Services.AddScoped<IAdminPlatformApiClient, AdminPlatformApiClient>();
builder.Services.AddScoped<IAdminProductApiClient, AdminProductApiClient>();
builder.Services.AddScoped<IAccountSetupApiClient, AccountSetupApiClient>();
builder.Services.AddHttpClient(AccountSetupConstants.ApiClientName);
builder.Services.AddOptions<PublicMenuApiOptions>()
    .Bind(builder.Configuration.GetSection("PublicApi"))
    .ValidateDataAnnotations()
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "Public API base URL must be absolute.")
    .ValidateOnStart();
builder.Services.AddHttpClient<IPublicMenuApiClient, PublicMenuApiClient>((serviceProvider, httpClient) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<PublicMenuApiOptions>>().Value;
    httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAdminRouteAuthorizationRedirect();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapAdminAuthEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
