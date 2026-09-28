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

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<MusicAlbumsDbContext>().Database.Migrate();
}

app.MapProviderEndpoints();
app.MapCatalogEndpoints();
app.MapLibraryEndpoints();

app.Run();

public partial class Program;
