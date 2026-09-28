namespace MusicAlbums.Core.Exceptions;

public sealed class AlbumProviderNotFoundException(string providerName, string providerAlbumId)
    : Exception($"Album '{providerAlbumId}' was not found in provider '{providerName}'.")
{
    public string ProviderName { get; } = providerName;

    public string ProviderAlbumId { get; } = providerAlbumId;
}
