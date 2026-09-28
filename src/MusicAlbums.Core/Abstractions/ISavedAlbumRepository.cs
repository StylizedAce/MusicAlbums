using MusicAlbums.Core.Models;

namespace MusicAlbums.Core.Abstractions;

public interface ISavedAlbumRepository
{
    Task<IReadOnlyList<SavedAlbum>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<SavedAlbum?> FindAsync(Guid userId, Guid albumId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid userId, string providerName, string providerAlbumId, CancellationToken cancellationToken = default);

    void Add(SavedAlbum album);

    void Remove(SavedAlbum album);
}
