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
using MusicAlbums.Infrastructure.Providers.Throttling;

namespace MusicAlbums.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Library")
            ?? throw new InvalidOperationException("Connection string 'Library' is not configured.");

        services.AddDbContext<MusicAlbumsDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IUnitOfWork>(serviceProvider => new TranslatingUnitOfWork(serviceProvider.GetRequiredService<MusicAlbumsDbContext>()));
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

        services.AddOptions<SpotifyOptions>()
            .Bind(configuration.GetSection(SpotifyOptions.SectionName))
            .Validate(options => Uri.TryCreate(options.AccountsBaseUrl, UriKind.Absolute, out _), "AlbumProviders:Spotify:AccountsBaseUrl must be an absolute URI.")
            .Validate(options => Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out _), "AlbumProviders:Spotify:ApiBaseUrl must be an absolute URI.")
            .Validate(options => options.Mode != SpotifyProviderMode.Api
                || (!string.IsNullOrWhiteSpace(options.ClientId) && !string.IsNullOrWhiteSpace(options.ClientSecret)),
                "AlbumProviders:Spotify:ClientId and ClientSecret are required when Mode is 'Api'.")
            .Validate(options => options.TimeoutSeconds > 0, "AlbumProviders:Spotify:TimeoutSeconds must be positive.")
            .ValidateOnStart();

        services.AddHttpClient<DeezerAlbumProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<DeezerOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddTransient<IAlbumProvider>(serviceProvider =>
            new ThrottledAlbumProvider(
                serviceProvider.GetRequiredService<DeezerAlbumProvider>(),
                serviceProvider.GetRequiredService<ProviderGuardRegistry>()));

        services.AddSingleton<ISpotifyTokenProvider, SpotifyTokenProvider>();

        services.AddOptions<ProviderThrottleOptions>()
            .Bind(configuration.GetSection(ProviderThrottleOptions.SectionName))
            .Validate(options => options.MaxConcurrentRequests > 0, "AlbumProviders:Throttling:MaxConcurrentRequests must be positive.")
            .Validate(options => options.FailureThreshold > 0, "AlbumProviders:Throttling:FailureThreshold must be positive.")
            .Validate(options => options.BreakDurationSeconds > 0, "AlbumProviders:Throttling:BreakDurationSeconds must be positive.")
            .ValidateOnStart();

        services.AddSingleton<ProviderGuardRegistry>();

        services.AddHttpClient(SpotifyHttpClients.Accounts, (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SpotifyOptions>>().Value;
            client.BaseAddress = new Uri(options.AccountsBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddHttpClient<SpotifyAlbumProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SpotifyOptions>>().Value;
            client.BaseAddress = new Uri(options.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddSingleton<ISpotifyTokenProvider, SpotifyTokenProvider>();

        var spotifyOptions = configuration.GetSection(SpotifyOptions.SectionName).Get<SpotifyOptions>() ?? new SpotifyOptions();

        if (spotifyOptions.Mode == SpotifyProviderMode.Fake)
        {
            services.AddSingleton<IAlbumProvider, FakeSpotifyAlbumProvider>();
        }
        else
        {
            services.AddTransient<IAlbumProvider>(serviceProvider =>
                new ThrottledAlbumProvider(
                    serviceProvider.GetRequiredService<SpotifyAlbumProvider>(),
                    serviceProvider.GetRequiredService<ProviderGuardRegistry>()));
        }

        services.AddTransient<IAlbumProviderFactory, AlbumProviderFactory>();

        return services;
    }
}
