using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MusicAlbums.Core;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Providers.Spotify;

public sealed class SpotifyAlbumProvider(HttpClient httpClient, ISpotifyTokenProvider tokenProvider) : IAlbumProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const int MaxSearchLimit = 10;

    public string Name => ProviderNames.Spotify;

    public async Task<AlbumSearchResult> SearchAlbumsAsync(AlbumSearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Text);

        var limit = Math.Clamp(query.Limit, 1, MaxSearchLimit);
        var offset = Math.Max(query.Offset, 0);
        var effectiveQuery = query with { Limit = limit, Offset = offset };
        var url = $"search?q={Uri.EscapeDataString(query.Text)}&type=album&limit={limit}&offset={offset}";

        var response = await GetAsync<SpotifySearchResponse>(url, providerAlbumId: null, cancellationToken);

        var albums = response.Albums?.Items?.Select(SpotifyAlbumMapper.ToProviderAlbum).ToArray() ?? [];
        return new AlbumSearchResult(albums, response.Albums?.Total ?? albums.Length, effectiveQuery);
    }

    public async Task<ProviderAlbum> GetAlbumAsync(string providerAlbumId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerAlbumId);

        var url = $"albums/{Uri.EscapeDataString(providerAlbumId)}";
        var response = await GetAsync<SpotifyAlbumDto>(url, providerAlbumId, cancellationToken);

        return response.Id is null
            ? throw new AlbumProviderNotFoundException(Name, providerAlbumId)
            : SpotifyAlbumMapper.ToProviderAlbum(response);
    }

    private async Task<T> GetAsync<T>(string relativeUrl, string? providerAlbumId, CancellationToken cancellationToken)
    {
        var accessToken = await tokenProvider.GetAccessTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new AlbumProviderUnavailableException(Name, "Could not reach the Spotify Web API.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AlbumProviderUnavailableException(Name, "The Spotify Web API request timed out.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var message = await TryReadErrorMessageAsync(response, cancellationToken);

                if (response.StatusCode == HttpStatusCode.NotFound && providerAlbumId is not null)
                {
                    throw new AlbumProviderNotFoundException(Name, providerAlbumId);
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new AlbumProviderUnavailableException(Name, message
                        ?? "Spotify denied access (HTTP 403). Development Mode apps require the app owner to have an active Premium subscription (February 2026 policy).");
                }

                throw new AlbumProviderUnavailableException(Name, message ?? $"Spotify returned HTTP {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return payload ?? throw new AlbumProviderUnavailableException(Name, "Spotify returned an empty response body.");
        }
    }

    private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<SpotifyErrorResponse>(JsonOptions, cancellationToken);
            return payload?.Error?.Message;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
