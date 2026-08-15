using CafeMenu.Api.Bootstrap;
using CafeMenu.Api.Configuration;
using CafeMenu.Api.Storage;
using CafeMenu.Shared.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationApi();
builder.Services.AddApplicationOpenApi();
builder.Services.AddApplicationDatabase(builder.Configuration);
builder.Services.AddApplicationAuthentication(builder.Configuration);
builder.Services.AddApplicationReverseProxy(builder.Configuration);

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapImageStorageEndpoints();
app.MapControllers();

app.Run();

public partial class Program;
