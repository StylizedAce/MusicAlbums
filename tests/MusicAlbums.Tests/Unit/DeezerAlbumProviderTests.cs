using System.Net;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;
using MusicAlbums.Infrastructure.Providers.Deezer;
using MusicAlbums.TestSupport;

namespace MusicAlbums.Tests.Unit;

public sealed class DeezerAlbumProviderTests
{
    private static DeezerAlbumProvider CreateProvider(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.deezer.com/") });

    [Fact]
    public async Task SearchAlbumsAsync_MapsResults_AndSendsDeezerPagingParameters()
    {
        var handler = new StubHttpMessageHandler(_ => DeezerTestData.Json(DeezerTestData.SearchResponseJson));
        var provider = CreateProvider(handler);

        var result = await provider.SearchAlbumsAsync(new AlbumSearchQuery("daft punk", Limit: 5, Offset: 10));

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/search/album", request.RequestUri!.AbsolutePath);
        Assert.Contains("q=daft%20punk", request.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("limit=5", request.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("index=10", request.RequestUri.Query, StringComparison.Ordinal);

        Assert.Equal(95, result.Total);
        var album = Assert.Single(result.Albums);
        Assert.Equal(DeezerTestData.DiscoveryAlbumId, album.ProviderAlbumId);
        Assert.Equal("Discovery", album.Title);
        Assert.Equal("Daft Punk", album.ArtistName);
        Assert.Equal("https://cdn-images.dzcdn.net/images/cover/discovery/1000x1000.jpg", album.CoverImageUrl);
        Assert.Equal("https://www.deezer.com/album/302127", album.ExternalUrl);
        Assert.Equal(14, album.TrackCount);
        Assert.Null(album.ReleaseDate);
        Assert.Empty(album.Tracks);
    }

    [Fact]
    public async Task SearchAlbumsAsync_ClampsLimitAndOffsetToSupportedRange()
    {
        var handler = new StubHttpMessageHandler(_ => DeezerTestData.Json(DeezerTestData.SearchResponseJson));
        var provider = CreateProvider(handler);

        await provider.SearchAlbumsAsync(new AlbumSearchQuery("daft punk", Limit: 5000, Offset: -10));

        var request = Assert.Single(handler.Requests);
        Assert.Contains($"limit={AlbumSearchQuery.MaxLimit}", request.RequestUri!.Query, StringComparison.Ordinal);
        Assert.Contains("index=0", request.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAlbumAsync_MapsDetailAndTracks_FallingBackToTrackOrderForPositions()
    {
        var handler = new StubHttpMessageHandler(_ => DeezerTestData.Json(DeezerTestData.AlbumDetailJson));
        var provider = CreateProvider(handler);

        var album = await provider.GetAlbumAsync(DeezerTestData.DiscoveryAlbumId);

        Assert.Equal("Discovery", album.Title);
        Assert.Equal(new DateOnly(2001, 3, 7), album.ReleaseDate);
        Assert.Equal(4, album.Tracks.Count);
        Assert.Equal(new[] { 1, 2, 3, 4 }, album.Tracks.Select(track => track.Position).ToArray());
        Assert.Equal("One More Time", album.Tracks[0].Title);
        Assert.Equal(320, album.Tracks[0].DurationSeconds);
        Assert.Equal("https://cdns-preview.dzcdn.net/stream/one-more-time.mp3", album.Tracks[0].PreviewUrl);
    }

    [Fact]
    public async Task GetAlbumAsync_WhenDeezerReportsNoData_ThrowsNotFound()
    {
        var handler = new StubHttpMessageHandler(_ => DeezerTestData.Json(DeezerTestData.NoDataErrorJson));
        var provider = CreateProvider(handler);

        var exception = await Assert.ThrowsAsync<AlbumProviderNotFoundException>(() => provider.GetAlbumAsync("999"));

        Assert.Equal("999", exception.ProviderAlbumId);
        Assert.Equal("deezer", exception.ProviderName);
    }

    [Fact]
    public async Task GetAlbumAsync_WhenDeezerIsUnavailable_ThrowsProviderUnavailable()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var provider = CreateProvider(handler);

        await Assert.ThrowsAsync<AlbumProviderUnavailableException>(() => provider.GetAlbumAsync(DeezerTestData.DiscoveryAlbumId));
    }

    [Fact]
    public async Task GetAlbumAsync_WhenReleaseDateIsInvalid_ReturnsNullReleaseDate()
    {
        const string json = """{ "id": 1, "title": "Unknown Date", "release_date": "0000-00-00", "artist": { "name": "Someone" } }""";
        var handler = new StubHttpMessageHandler(_ => DeezerTestData.Json(json));
        var provider = CreateProvider(handler);

        var album = await provider.GetAlbumAsync("1");

        Assert.Null(album.ReleaseDate);
        Assert.Empty(album.Tracks);
    }
}
