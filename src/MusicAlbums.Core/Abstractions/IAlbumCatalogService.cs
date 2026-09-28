using MusicAlbums.Core.Models;

namespace MusicAlbums.Core.Abstractions;

public interface IAlbumCatalogService
{
    Task<AlbumSearchResult> SearchAlbumsAsync(string? providerName, AlbumSearchQuery query, CancellationToken cancellationToken = default);

    Task<ProviderAlbum> GetAlbumAsync(string? providerName, string providerAlbumId, CancellationToken cancellationToken = default);
}
