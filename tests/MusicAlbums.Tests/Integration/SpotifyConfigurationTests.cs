using Microsoft.AspNetCore.Hosting;

namespace MusicAlbums.Tests.Integration;

public sealed class SpotifyConfigurationTests
{
    [Fact]
    public void SpotifyApiMode_FailsFast_UntilTheRealAdapterShips()
    {
        using var factory = new MusicAlbumsApiFactory();
        using var apiModeFactory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("AlbumProviders:Spotify:Mode", "Api"));

        var exception = Record.Exception(() => apiModeFactory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains("not implemented yet", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
