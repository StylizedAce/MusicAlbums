namespace MusicAlbums.Core.Exceptions;

public sealed class AlbumAlreadyInLibraryException(string providerName, string providerAlbumId)
    : Exception($"Album '{providerAlbumId}' from provider '{providerName}' is already in the library.")
{
    public string ProviderName { get; } = providerName;

    public string ProviderAlbumId { get; } = providerAlbumId;
}
