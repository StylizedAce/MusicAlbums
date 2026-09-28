using MusicAlbums.Core.Models;

namespace MusicAlbums.Core.Abstractions;

public interface ILibraryService
{
    Task<SavedAlbum> AddAlbumAsync(string userName, string? providerName, string providerAlbumId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedAlbum>> GetLibraryAsync(string userName, CancellationToken cancellationToken = default);

    Task<SavedAlbum> GetAlbumAsync(string userName, Guid albumId, CancellationToken cancellationToken = default);

    Task RemoveAlbumAsync(string userName, Guid albumId, CancellationToken cancellationToken = default);
}
