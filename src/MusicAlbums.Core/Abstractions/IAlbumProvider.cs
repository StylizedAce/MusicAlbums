using MusicAlbums.Core.Models;

namespace MusicAlbums.Core.Abstractions;

public interface IAlbumProvider
{
    string Name { get; }

    Task<AlbumSearchResult> SearchAlbumsAsync(AlbumSearchQuery query, CancellationToken cancellationToken = default);

    Task<ProviderAlbum> GetAlbumAsync(string providerAlbumId, CancellationToken cancellationToken = default);
}
