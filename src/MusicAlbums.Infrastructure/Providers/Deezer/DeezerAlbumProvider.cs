using System.Net.Http.Json;
using System.Text.Json;
using MusicAlbums.Core;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Providers.Deezer;

public sealed class DeezerAlbumProvider(HttpClient httpClient) : IAlbumProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Name => ProviderNames.Deezer;

    public async Task<AlbumSearchResult> SearchAlbumsAsync(AlbumSearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Text);

        var limit = Math.Clamp(query.Limit, 1, AlbumSearchQuery.MaxLimit);
        var offset = Math.Max(query.Offset, 0);
        var effectiveQuery = query with { Limit = limit, Offset = offset };
        var url = $"search/album?q={Uri.EscapeDataString(query.Text)}&limit={limit}&index={offset}";

        var response = await GetAsync<DeezerSearchResponse>(url, cancellationToken);
        EnsureNoError(response.Error);

        var albums = response.Data?.Select(DeezerAlbumMapper.ToProviderAlbum).ToArray() ?? [];
        return new AlbumSearchResult(albums, response.Total ?? albums.Length, effectiveQuery);
    }

    public async Task<ProviderAlbum> GetAlbumAsync(string providerAlbumId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerAlbumId);

        var url = $"album/{Uri.EscapeDataString(providerAlbumId)}";
        var response = await GetAsync<DeezerAlbumDto>(url, cancellationToken);
        EnsureNoError(response.Error, providerAlbumId);

        if (response.Title is null)
        {
            throw new AlbumProviderNotFoundException(Name, providerAlbumId);
        }

        return DeezerAlbumMapper.ToProviderAlbum(response);
    }

    private async Task<T> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(relativeUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new AlbumProviderUnavailableException(Name, $"Deezer returned HTTP {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return payload ?? throw new AlbumProviderUnavailableException(Name, "Deezer returned an empty response body.");
    }

    private async Task<HttpResponseMessage> SendAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient.GetAsync(relativeUrl, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new AlbumProviderUnavailableException(Name, "Could not reach the Deezer API.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AlbumProviderUnavailableException(Name, "The Deezer API request timed out.", exception);
        }
    }

    private void EnsureNoError(DeezerErrorDto? error, string? providerAlbumId = null)
    {
        if (error is null)
        {
            return;
        }

        if (error.Code == 800 && providerAlbumId is not null)
        {
            throw new AlbumProviderNotFoundException(Name, providerAlbumId);
        }

        throw new AlbumProviderUnavailableException(Name, error.Message ?? "Deezer returned an unknown error.");
    }
}
