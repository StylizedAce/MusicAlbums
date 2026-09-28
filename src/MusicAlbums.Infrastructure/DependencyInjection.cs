using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Infrastructure.Persistence;
using MusicAlbums.Infrastructure.Persistence.Repositories;
using MusicAlbums.Infrastructure.Providers;
using MusicAlbums.Infrastructure.Providers.Deezer;
using MusicAlbums.Infrastructure.Providers.Spotify;

namespace MusicAlbums.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Library")
            ?? throw new InvalidOperationException("Connection string 'Library' is not configured.");

        services.AddDbContext<MusicAlbumsDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<MusicAlbumsDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISavedAlbumRepository, SavedAlbumRepository>();

        services.AddOptions<AlbumProviderOptions>()
            .Bind(configuration.GetSection(AlbumProviderOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Default), "AlbumProviders:Default is required.")
            .ValidateOnStart();

        services.AddOptions<DeezerOptions>()
            .Bind(configuration.GetSection(DeezerOptions.SectionName))
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "AlbumProviders:Deezer:BaseUrl must be an absolute URI.")
            .Validate(options => options.TimeoutSeconds > 0, "AlbumProviders:Deezer:TimeoutSeconds must be positive.")
            .ValidateOnStart();

        services.AddHttpClient<DeezerAlbumProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<DeezerOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddTransient<IAlbumProvider>(serviceProvider => serviceProvider.GetRequiredService<DeezerAlbumProvider>());
        services.AddSingleton<IAlbumProvider>(new SpotifyAlbumProvider());

        services.AddTransient<IAlbumProviderFactory, AlbumProviderFactory>();

        return services;
    }
}
