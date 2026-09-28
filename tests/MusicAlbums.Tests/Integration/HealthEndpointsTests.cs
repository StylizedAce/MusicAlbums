using System.Net;
using MusicAlbums.TestSupport;

namespace MusicAlbums.Tests.Integration;

public sealed class HealthEndpointsTests(MusicAlbumsApiFactory factory) : IClassFixture<MusicAlbumsApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Live_ReturnsHealthy_WithoutCheckingDependencies()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_ReturnsHealthy_WithDatabaseCheck()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
