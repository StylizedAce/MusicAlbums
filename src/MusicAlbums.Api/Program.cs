using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MusicAlbums.Api.Endpoints;
using MusicAlbums.Api.ErrorHandling;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Application;
using MusicAlbums.Infrastructure;
using MusicAlbums.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<MusicAlbumsExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<MusicAlbumsDbContext>("database", tags: ["ready"]);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAlbumCatalogService, AlbumCatalogService>();
builder.Services.AddScoped<ILibraryService, LibraryService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") });

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<MusicAlbumsDbContext>().Database.Migrate();
}

app.MapProviderEndpoints();
app.MapCatalogEndpoints();
app.MapLibraryEndpoints();

app.Run();

public partial class Program;
