using CafeMenu.Api.Bootstrap;
using CafeMenu.Api.Configuration;
using CafeMenu.Api.Storage;
using CafeMenu.Shared.HealthChecks;
using CafeMenu.Shared.HostFiltering;
using CafeMenu.Shared.RateLimiting;
using CafeMenu.Shared.ReverseProxy;
using CafeMenu.Shared.SecurityHeaders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationApi();
builder.Services.AddApplicationOpenApi();
builder.Services.AddApplicationDatabase(builder.Configuration);
builder.Services.AddApplicationAuthentication(builder.Configuration);
builder.Services.AddApplicationHostFiltering();
builder.Services.AddApplicationReverseProxy(builder.Configuration);
builder.Services.AddApplicationRateLimiting(builder.Configuration);
builder.Services.AddApplicationSecurityHeaders();
builder.Services.AddApplicationHealthChecks();

var app = builder.Build();

if (PlatformAdminBootstrapCommand.IsRequested(args))
{
    var bootstrapRunner = app.Services.GetRequiredService<PlatformAdminBootstrapRunner>();
    Environment.ExitCode = await bootstrapRunner.RunAsync(args, CancellationToken.None);
    return;
}

app.UseExceptionHandler();
app.UseConfiguredForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseApplicationSecurityHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapApplicationHealthEndpoints();
app.MapImageStorageEndpoints();
app.MapControllers();

app.Run();

public partial class Program;
