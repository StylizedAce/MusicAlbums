using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Core.Application;

public sealed class AlbumCatalogService(IAlbumProviderFactory providerFactory) : IAlbumCatalogService
{
    public Task<AlbumSearchResult> SearchAlbumsAsync(string? providerName, AlbumSearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Text);

        var provider = ResolveProvider(providerName);
        return provider.SearchAlbumsAsync(query, cancellationToken);
    }

    public Task<ProviderAlbum> GetAlbumAsync(string? providerName, string providerAlbumId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerAlbumId);

        var provider = ResolveProvider(providerName);
        return provider.GetAlbumAsync(providerAlbumId, cancellationToken);
    }

    private IAlbumProvider ResolveProvider(string? providerName) =>
        string.IsNullOrWhiteSpace(providerName) ? providerFactory.GetDefault() : providerFactory.GetRequired(providerName);
}
