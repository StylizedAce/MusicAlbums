using System.Net;
using MusicAlbums.TestSupport;

namespace MusicAlbums.Tests.Integration;

public sealed class FrontendTests(MusicAlbumsApiFactory factory) : IClassFixture<MusicAlbumsApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Root_ReturnsTheDemoFrontendShell()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("MusicAlbums", html, StringComparison.Ordinal);
        Assert.Contains("app.js", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StaticAssets_AreServed()
    {
        var response = await _client.GetAsync("/app.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var script = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/albums/search", script, StringComparison.Ordinal);
    }
}
