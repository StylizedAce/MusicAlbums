namespace MusicAlbums.Infrastructure.Providers.Spotify;

public interface ISpotifyTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
