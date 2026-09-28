namespace MusicAlbums.TestSupport;

public sealed class StubHttpClientFactory(HttpMessageHandler handler, string baseAddress = "https://accounts.spotify.com/") : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false)
    {
        BaseAddress = new Uri(baseAddress)
    };
}
