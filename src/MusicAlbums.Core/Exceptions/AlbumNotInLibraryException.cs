namespace MusicAlbums.Core.Exceptions;

public sealed class AlbumNotInLibraryException(Guid albumId)
    : Exception($"Album '{albumId}' is not in the library.")
{
    public Guid AlbumId { get; } = albumId;
}
