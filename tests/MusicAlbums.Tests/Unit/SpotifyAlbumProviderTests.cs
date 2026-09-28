using System.Net;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;
using MusicAlbums.Infrastructure.Providers.Spotify;
using MusicAlbums.TestSupport;

namespace MusicAlbums.Tests.Unit;

public sealed class SpotifyAlbumProviderTests
{
    [Fact]
    public async Task SearchAlbumsAsync_MapsResults_AndSendsPagingPlusBearerToken()
    {
        var handler = new StubHttpMessageHandler(_ => SpotifyTestData.Json(SpotifyTestData.SearchResponseJson));
        var provider = CreateProvider(handler);

        var result = await provider.SearchAlbumsAsync(new AlbumSearchQuery("daft punk", Limit: 5, Offset: 10));

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/search", request.RequestUri!.AbsolutePath);
        Assert.Contains("q=daft%20punk", request.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("type=album", request.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("limit=5", request.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("offset=10", request.RequestUri.Query, StringComparison.Ordinal);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("test-access-token", request.Headers.Authorization.Parameter);

        Assert.Equal(2, result.Total);
        var album = result.Albums[0];
        Assert.Equal("sp-1", album.ProviderAlbumId);
        Assert.Equal("spotify", album.ProviderName);
        Assert.Equal("Discovery", album.Title);
        Assert.Equal("Daft Punk", album.ArtistName);
        Assert.Equal("https://i.scdn.co/image/large-cover.jpg", album.CoverImageUrl);
        Assert.Equal("https://open.spotify.com/album/sp-1", album.ExternalUrl);
        Assert.Equal(new DateOnly(2001, 3, 7), album.ReleaseDate);
        Assert.Equal(14, album.TrackCount);
        Assert.Empty(album.Tracks);
    }

    [Fact]
    public async Task GetAlbumAsync_MapsTracksPreviewAndMonthPrecisionReleaseDate()
    {
        var handler = new StubHttpMessageHandler(_ => SpotifyTestData.Json(SpotifyTestData.AlbumDetailJson));
        var provider = CreateProvider(handler);

        var album = await provider.GetAlbumAsync("sp-1");

        Assert.Equal("Random Access Memories", album.Title);
        Assert.Equal("https://open.spotify.com/album/sp-1", album.ExternalUrl);
        Assert.Equal(new DateOnly(2013, 5, 1), album.ReleaseDate);
        Assert.Equal(2, album.Tracks.Count);
        Assert.Equal(1, album.Tracks[0].Position);
        Assert.Equal("Get Lucky", album.Tracks[0].Title);
        Assert.Equal(369, album.Tracks[0].DurationSeconds);
        Assert.Equal("https://p.scdn.co/mp3-preview/get-lucky", album.Tracks[0].PreviewUrl);
        Assert.Null(album.Tracks[1].PreviewUrl);
    }

    [Fact]
    public async Task SearchAlbumsAsync_ClampsLimitToSpotifyDevModeMaximum()
    {
        var handler = new StubHttpMessageHandler(_ => SpotifyTestData.Json(SpotifyTestData.SearchResponseJson));
        var provider = CreateProvider(handler);

        await provider.SearchAlbumsAsync(new AlbumSearchQuery("daft punk", Limit: 50));

        var request = Assert.Single(handler.Requests);
        Assert.Contains("limit=10", request.RequestUri!.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAlbumAsync_WhenForbidden_HintsPremiumRequirement()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        var provider = CreateProvider(handler);

        var exception = await Assert.ThrowsAsync<AlbumProviderUnavailableException>(() => provider.GetAlbumAsync("sp-1"));

        Assert.Contains("Premium", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAlbumAsync_WhenAlbumNotFound_ThrowsNotFound()
    {
        var handler = new StubHttpMessageHandler(_ => SpotifyTestData.Json(SpotifyTestData.NotFoundErrorJson, HttpStatusCode.NotFound));
        var provider = CreateProvider(handler);

        var exception = await Assert.ThrowsAsync<AlbumProviderNotFoundException>(() => provider.GetAlbumAsync("missing"));

        Assert.Equal("missing", exception.ProviderAlbumId);
    }

    [Fact]
    public async Task GetAlbumAsync_WhenUnauthorized_ThrowsUnavailableWithProviderMessage()
    {
        var handler = new StubHttpMessageHandler(_ => SpotifyTestData.Json(SpotifyTestData.UnauthorizedErrorJson, HttpStatusCode.Unauthorized));
        var provider = CreateProvider(handler);

        var exception = await Assert.ThrowsAsync<AlbumProviderUnavailableException>(() => provider.GetAlbumAsync("sp-1"));

        Assert.Contains("Invalid access token", exception.Message, StringComparison.Ordinal);
    }

    private static SpotifyAlbumProvider CreateProvider(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.spotify.com/v1/") }, new StubSpotifyTokenProvider());

    private sealed class StubSpotifyTokenProvider : ISpotifyTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("test-access-token");
    }
}
