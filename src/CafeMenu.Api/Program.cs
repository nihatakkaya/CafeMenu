using CafeMenu.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationApi();
builder.Services.AddApplicationOpenApi();
builder.Services.AddApplicationDatabase(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program;
