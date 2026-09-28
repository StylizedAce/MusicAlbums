namespace MusicAlbums.Infrastructure.Providers.Deezer;

public sealed class DeezerOptions
{
    public const string SectionName = "AlbumProviders:Deezer";

    public string BaseUrl { get; set; } = "https://api.deezer.com/";

    public int TimeoutSeconds { get; set; } = 15;
}
