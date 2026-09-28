using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace MusicAlbums.Tests.Integration;

public sealed class LibraryApiTests(MusicAlbumsApiFactory factory) : IClassFixture<MusicAlbumsApiFactory>
{
    private readonly MusicAlbumsApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task FullLibraryLifecycle_UsesRealDeezerProviderAgainstStubbedHttp()
    {
        var user = $"lifecycle-{Guid.NewGuid():N}";

        var providers = await _client.GetFromJsonAsync<JsonArray>("/api/providers");
        Assert.NotNull(providers);
        Assert.Equal(2, providers.Count);
        Assert.Contains(providers, node => node!["name"]!.GetValue<string>() == "deezer" && node["isDefault"]!.GetValue<bool>());
        Assert.Contains(providers, node => node!["name"]!.GetValue<string>() == "spotify" && !node["isDefault"]!.GetValue<bool>());

        var requestsBeforeSearch = _factory.DeezerHandler.Requests.Count;

        var search = await _client.GetFromJsonAsync<JsonObject>("/api/albums/search?q=daft%20punk");
        Assert.NotNull(search);
        Assert.Equal(95, search["total"]!.GetValue<int>());
        var searchAlbum = Assert.Single(search["albums"]!.AsArray());
        Assert.Equal("Discovery", searchAlbum!["title"]!.GetValue<string>());
        Assert.Equal("deezer", searchAlbum["provider"]!.GetValue<string>());

        var searchRequest = Assert.Single(_factory.DeezerHandler.Requests.Skip(requestsBeforeSearch));
        Assert.Equal("/search/album", searchRequest.RequestUri!.AbsolutePath);

        var saveResponse = await _client.PostAsJsonAsync($"/api/users/{user}/library", new { provider = "deezer", providerAlbumId = "302127" });
        Assert.Equal(HttpStatusCode.Created, saveResponse.StatusCode);

        var saved = JsonNode.Parse(await saveResponse.Content.ReadAsStringAsync())!.AsObject();
        Assert.Equal("Discovery", saved["title"]!.GetValue<string>());
        Assert.Equal("Daft Punk", saved["artist"]!.GetValue<string>());
        Assert.Equal("2001-03-07", saved["releaseDate"]!.GetValue<string>());
        var albumId = saved["id"]!.GetValue<Guid>();
        Assert.Equal($"/api/users/{user}/library/{albumId}", saveResponse.Headers.Location!.ToString());

        var savedTracks = saved["tracks"]!.AsArray();
        Assert.Equal(4, savedTracks.Count);
        Assert.Equal("One More Time", savedTracks[0]!["title"]!.GetValue<string>());
        Assert.Equal(320, savedTracks[0]!["durationSeconds"]!.GetValue<int>());

        var library = await _client.GetFromJsonAsync<JsonArray>($"/api/users/{user}/library");
        Assert.NotNull(library);
        var libraryAlbum = Assert.Single(library);
        Assert.Equal("Discovery", libraryAlbum!["title"]!.GetValue<string>());
        Assert.Equal(4, libraryAlbum["tracks"]!.AsArray().Count);

        var duplicate = await _client.PostAsJsonAsync($"/api/users/{user}/library", new { provider = "deezer", providerAlbumId = "302127" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("application/problem+json", duplicate.Content.Headers.ContentType?.MediaType);

        var deleteResponse = await _client.DeleteAsync($"/api/users/{user}/library/{albumId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await _client.GetFromJsonAsync<JsonArray>($"/api/users/{user}/library");
        Assert.NotNull(afterDelete);
        Assert.Empty(afterDelete);
    }

    [Fact]
    public async Task SearchAlbums_WithSpotifyProvider_UsesFakeSpotifyStrategy()
    {
        var search = await _client.GetFromJsonAsync<JsonObject>("/api/albums/search?q=radiohead&provider=spotify");

        Assert.NotNull(search);
        Assert.Equal(2, search["total"]!.GetValue<int>());
        var albums = search["albums"]!.AsArray();
        Assert.All(albums, album => Assert.Equal("spotify", album!["provider"]!.GetValue<string>()));
    }

    [Fact]
    public async Task SearchAlbums_UnknownProvider_Returns400ProblemDetails()
    {
        var response = await _client.GetAsync("/api/albums/search?q=test&provider=napster");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        Assert.Equal("Unknown album provider", problem["title"]!.GetValue<string>());
    }

    [Fact]
    public async Task SearchAlbums_MissingQuery_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync("/api/albums/search");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsObject();
        Assert.True(problem["errors"]!["q"] is not null);
    }

    [Fact]
    public async Task SaveAlbum_UnknownToProvider_Returns404ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync($"/api/users/erin-{Guid.NewGuid():N}/library", new { provider = "deezer", providerAlbumId = "999999999999" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetLibrary_UnknownUser_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync($"/api/users/missing-{Guid.NewGuid():N}/library");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
