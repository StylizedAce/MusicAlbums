using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace MusicAlbums.Tests.Integration;

public sealed class SpotifyConfigurationTests
{
    [Fact]
    public void ApiMode_WithoutCredentials_FailsStartupValidation()
    {
        using var factory = new MusicAlbumsApiFactory();
        using var apiModeFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("AlbumProviders:Spotify:Mode", "Api");
        });

        var exception = Record.Exception(() => apiModeFactory.CreateClient());

        Assert.NotNull(exception);
        Assert.Contains("ClientId", exception.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiMode_WithCredentials_RegistersTheRealProvider()
    {
        using var factory = new MusicAlbumsApiFactory();
        using var apiModeFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("AlbumProviders:Spotify:Mode", "Api");
            builder.UseSetting("AlbumProviders:Spotify:ClientId", "test-id");
            builder.UseSetting("AlbumProviders:Spotify:ClientSecret", "test-secret");
        });

        var client = apiModeFactory.CreateClient();
        var providers = await client.GetFromJsonAsync<JsonArray>("/api/providers");

        Assert.NotNull(providers);
        Assert.Contains(providers, node => node!["name"]!.GetValue<string>() == "spotify");
    }
}
