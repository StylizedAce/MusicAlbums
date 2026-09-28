using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Infrastructure.Providers.Spotify;
using MusicAlbums.TestSupport;

namespace MusicAlbums.Tests.Unit;

public sealed class SpotifyTokenProviderTests
{
    private static readonly SpotifyOptions SpotifySettings = new()
    {
        ClientId = "test-id",
        ClientSecret = "test-secret"
    };

    [Fact]
    public async Task GetAccessTokenAsync_RequestsTokenOnce_AndCachesIt()
    {
        var handler = new StubHttpMessageHandler(_ => SpotifyTestData.Json(SpotifyTestData.TokenJson));
        var time = new MutableTimeProvider(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
        var provider = CreateProvider(handler, time);

        var first = await provider.GetAccessTokenAsync();
        var second = await provider.GetAccessTokenAsync();

        Assert.Equal("test-access-token", first);
        Assert.Equal(first, second);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/token", request.RequestUri!.AbsolutePath);
        Assert.Equal("Basic", request.Headers.Authorization!.Scheme);
        Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("test-id:test-secret")), request.Headers.Authorization.Parameter);
        Assert.Contains("grant_type=client_credentials", Assert.Single(handler.RequestBodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAccessTokenAsync_RefreshesTokenAfterExpiry()
    {
        var handler = new StubHttpMessageHandler(_ => SpotifyTestData.Json(SpotifyTestData.TokenJson));
        var time = new MutableTimeProvider(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
        var provider = CreateProvider(handler, time);

        await provider.GetAccessTokenAsync();
        time.UtcNow = time.UtcNow.AddSeconds(3601);
        await provider.GetAccessTokenAsync();

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenTokenEndpointFails_ThrowsProviderUnavailable()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var provider = CreateProvider(handler, new MutableTimeProvider(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<AlbumProviderUnavailableException>(() => provider.GetAccessTokenAsync());
    }

    private static SpotifyTokenProvider CreateProvider(StubHttpMessageHandler handler, MutableTimeProvider time) =>
        new(new StubHttpClientFactory(handler), Options.Create(SpotifySettings), time);
}
