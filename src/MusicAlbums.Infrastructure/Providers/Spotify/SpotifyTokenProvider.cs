using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MusicAlbums.Core;
using MusicAlbums.Core.Exceptions;

namespace MusicAlbums.Infrastructure.Providers.Spotify;

public sealed class SpotifyTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<SpotifyOptions> options,
    TimeProvider timeProvider) : ISpotifyTokenProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (IsTokenFresh(timeProvider.GetUtcNow()))
        {
            return _accessToken!;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var now = timeProvider.GetUtcNow();
            if (IsTokenFresh(now))
            {
                return _accessToken!;
            }

            var token = await RequestTokenAsync(cancellationToken);
            _accessToken = token.AccessToken!;
            _expiresAt = now.AddSeconds(Math.Max(token.ExpiresIn - 30, 30));

            return _accessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool IsTokenFresh(DateTimeOffset now) => _accessToken is not null && now < _expiresAt;

    private async Task<SpotifyTokenResponse> RequestTokenAsync(CancellationToken cancellationToken)
    {
        var spotifyOptions = options.Value;
        var client = httpClientFactory.CreateClient(SpotifyHttpClients.Accounts);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            })
        };

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{spotifyOptions.ClientId}:{spotifyOptions.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new AlbumProviderUnavailableException(ProviderNames.Spotify, "Could not reach the Spotify accounts API.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AlbumProviderUnavailableException(ProviderNames.Spotify, "The Spotify token request timed out.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new AlbumProviderUnavailableException(ProviderNames.Spotify, $"The Spotify token endpoint returned HTTP {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<SpotifyTokenResponse>(JsonOptions, cancellationToken);
            return payload?.AccessToken is null
                ? throw new AlbumProviderUnavailableException(ProviderNames.Spotify, "The Spotify token endpoint returned an empty payload.")
                : payload;
        }
    }
}
