using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;
using MusicAlbums.Infrastructure.Providers.Spotify;

namespace MusicAlbums.Tests.Unit;

public sealed class SpotifyAlbumProviderTests
{
    private readonly SpotifyAlbumProvider _provider = new();

    [Fact]
    public async Task SearchAlbumsAsync_MatchesArtistOrTitle_CaseInsensitively()
    {
        var byArtist = await _provider.SearchAlbumsAsync(new AlbumSearchQuery("radiohead"));

        Assert.Equal(2, byArtist.Total);
        Assert.All(byArtist.Albums, album => Assert.Equal("Radiohead", album.ArtistName));

        var byTitle = await _provider.SearchAlbumsAsync(new AlbumSearchQuery("kID a"));

        var album = Assert.Single(byTitle.Albums);
        Assert.Equal("Kid A", album.Title);
    }

    [Fact]
    public async Task SearchAlbumsAsync_AppliesPaging()
    {
        var page = await _provider.SearchAlbumsAsync(new AlbumSearchQuery("daft punk", Limit: 1, Offset: 1));

        Assert.Equal(2, page.Total);
        var album = Assert.Single(page.Albums);
        Assert.Equal("Random Access Memories", album.Title);
    }

    [Fact]
    public async Task GetAlbumAsync_ReturnsCatalogueAlbumWithTracks()
    {
        var album = await _provider.GetAlbumAsync("alb-001");

        Assert.Equal("Discovery", album.Title);
        Assert.Equal("spotify", album.ProviderName);
        Assert.Equal(new DateOnly(2001, 3, 7), album.ReleaseDate);
        Assert.Equal(4, album.Tracks.Count);
    }

    [Fact]
    public async Task GetAlbumAsync_UnknownId_ThrowsNotFound()
    {
        var exception = await Assert.ThrowsAsync<AlbumProviderNotFoundException>(() => _provider.GetAlbumAsync("missing"));

        Assert.Equal("missing", exception.ProviderAlbumId);
    }
}
