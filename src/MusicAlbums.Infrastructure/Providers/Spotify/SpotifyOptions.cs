namespace MusicAlbums.Infrastructure.Providers.Spotify;

public sealed class SpotifyOptions
{
    public const string SectionName = "AlbumProviders:Spotify";

    public SpotifyProviderMode Mode { get; set; } = SpotifyProviderMode.Fake;

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string AccountsBaseUrl { get; set; } = "https://accounts.spotify.com/";

    public string ApiBaseUrl { get; set; } = "https://api.spotify.com/v1/";

    public int TimeoutSeconds { get; set; } = 15;
}
