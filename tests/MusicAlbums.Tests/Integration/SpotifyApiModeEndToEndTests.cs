using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;

namespace MusicAlbums.Tests.Integration;

public sealed class SpotifyApiModeEndToEndTests
{
    [Fact]
    public async Task SearchSaveAndDelete_RunThroughTheRealSpotifyAdapter_WithStubbedTransport()
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
        var user = $"spotify-e2e-{Guid.NewGuid():N}";

        var search = await client.GetFromJsonAsync<JsonObject>("/api/albums/search?q=daft%20punk&provider=spotify");
        Assert.NotNull(search);
        var albums = search["albums"]!.AsArray();
        Assert.Equal(2, albums.Count);
        var first = albums[0]!;
        Assert.Equal("Discovery", first["title"]!.GetValue<string>());
        Assert.Equal("https://i.scdn.co/image/large-cover.jpg", first["coverImageUrl"]!.GetValue<string>());

        var saveResponse = await client.PostAsJsonAsync($"/api/users/{user}/library", new { provider = "spotify", providerAlbumId = "sp-1" });
        Assert.Equal(HttpStatusCode.Created, saveResponse.StatusCode);

        var saved = JsonNode.Parse(await saveResponse.Content.ReadAsStringAsync())!.AsObject();
        Assert.Equal("Random Access Memories", saved["title"]!.GetValue<string>());
        Assert.Equal("2013-05-01", saved["releaseDate"]!.GetValue<string>());
        var tracks = saved["tracks"]!.AsArray();
        Assert.Equal(2, tracks.Count);
        Assert.Equal("https://p.scdn.co/mp3-preview/get-lucky", tracks[0]!["previewUrl"]!.GetValue<string>());

        var library = await client.GetFromJsonAsync<JsonArray>($"/api/users/{user}/library");
        Assert.NotNull(library);
        Assert.Single(library);

        var albumId = saved["id"]!.GetValue<Guid>();
        var deleteResponse = await client.DeleteAsync($"/api/users/{user}/library/{albumId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        Assert.Single(factory.SpotifyAccountsHandler.Requests);
        Assert.Equal(2, factory.SpotifyApiHandler.Requests.Count);
    }
}
