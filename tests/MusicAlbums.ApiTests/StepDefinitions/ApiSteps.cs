using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using MusicAlbums.TestSupport;
using Reqnroll;

namespace MusicAlbums.ApiTests.StepDefinitions;

[Binding]
public sealed class ApiSteps(ScenarioContext scenarioContext)
{
    private HttpClient Client => scenarioContext.Get<HttpClient>();

    private HttpResponseMessage? _response;
    private JsonNode? _json;

    [When("I request the provider list")]
    public Task WhenIRequestTheProviderList() => GetAsync("/api/providers");

    [Then("the provider list contains \"(.*)\" as the default")]
    public void ThenTheProviderListContainsAsTheDefault(string providerName) =>
        Assert.Contains(_json!.AsArray(), node =>
            node!["name"]!.GetValue<string>() == providerName && node["isDefault"]!.GetValue<bool>());

    [Then("the provider list contains \"(.*)\"")]
    public void ThenTheProviderListContains(string providerName) =>
        Assert.Contains(_json!.AsArray(), node => node!["name"]!.GetValue<string>() == providerName);

    [When("I search provider \"(.*)\" for \"(.*)\"")]
    public Task WhenISearchProviderFor(string provider, string text) =>
        GetAsync($"/api/albums/search?q={Uri.EscapeDataString(text)}&provider={provider}");

    [When("I search with no query text")]
    public Task WhenISearchWithNoQueryText() => GetAsync("/api/albums/search");

    [Then("the search returns at least (.*) albums?")]
    public void ThenTheSearchReturnsAtLeast(int minimum) =>
        Assert.True(_json!["albums"]!.AsArray().Count >= minimum, "expected at least " + minimum + " albums");

    [Then("the first search result title is \"(.*)\"")]
    public void ThenTheFirstSearchResultTitleIs(string title) =>
        Assert.Equal(title, _json!["albums"]!.AsArray()[0]!["title"]!.GetValue<string>());

    [Then("the first search result exposes artist, title, cover and provider url")]
    public void ThenTheFirstSearchResultExposesArtistTitleCoverAndProviderUrl()
    {
        var album = _json!["albums"]!.AsArray()[0]!;

        Assert.False(string.IsNullOrWhiteSpace(album["artist"]!.GetValue<string>()));
        Assert.False(string.IsNullOrWhiteSpace(album["title"]!.GetValue<string>()));
        Assert.False(string.IsNullOrWhiteSpace(album["coverImageUrl"]!.GetValue<string>()));
        Assert.StartsWith("https://", album["externalUrl"]!.GetValue<string>());
    }

    [When("I save provider \"(.*)\" album \"(.*)\" for user \"(.*)\"")]
    public async Task WhenISaveProviderAlbumForUser(string provider, string providerAlbumId, string user)
    {
        _response = await Client.PostAsJsonAsync($"/api/users/{user}/library", new { provider, providerAlbumId });
        _json = await ReadJsonAsync(_response);
    }

    [When("I remove the saved album of user \"(.*)\"")]
    public async Task WhenIRemoveTheSavedAlbumOfUser(string user)
    {
        var library = JsonNode.Parse(await Client.GetStringAsync($"/api/users/{user}/library"))!.AsArray();
        var albumId = library[0]!["id"]!.GetValue<string>();

        _response = await Client.DeleteAsync($"/api/users/{user}/library/{albumId}");
    }

    [When("I request the library of \"(.*)\"")]
    public Task WhenIRequestTheLibraryOf(string user) => GetAsync($"/api/users/{user}/library");

    [Then("the library of \"(.*)\" contains an album with (.*) tracks")]
    public async Task ThenTheLibraryOfContainsAnAlbumWithTracks(string user, int trackCount)
    {
        var response = await Client.GetAsync($"/api/users/{user}/library");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var library = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsArray();
        var album = Assert.Single(library);
        Assert.Equal(trackCount, album!["tracks"]!.AsArray().Count);
    }

    [Then("the library of \"(.*)\" is empty")]
    public async Task ThenTheLibraryOfIsEmpty(string user)
    {
        var response = await Client.GetAsync($"/api/users/{user}/library");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var library = JsonNode.Parse(await response.Content.ReadAsStringAsync())!.AsArray();
        Assert.Empty(library);
    }

    [When("I request health endpoint \"(.*)\"")]
    public Task WhenIRequestHealthEndpoint(string path) => GetAsync(path);

    [Then("the response is healthy")]
    public async Task ThenTheResponseIsHealthy()
    {
        Assert.Equal(HttpStatusCode.OK, _response!.StatusCode);
        Assert.Equal("Healthy", await _response.Content.ReadAsStringAsync());
    }

    [Then("the response status is (.*)")]
    public void ThenTheResponseStatusIs(int statusCode) =>
        Assert.Equal(statusCode, (int)_response!.StatusCode);

    private async Task GetAsync(string path)
    {
        _response = await Client.GetAsync(path);
        _json = await ReadJsonAsync(_response);
    }

    private static async Task<JsonNode?> ReadJsonAsync(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentLength is null or 0)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync();

        try
        {
            return JsonNode.Parse(body);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
