namespace MusicAlbums.Core.Exceptions;

public sealed class UnknownAlbumProviderException(string providerName)
    : Exception($"Album provider '{providerName}' is not registered.")
{
    public string ProviderName { get; } = providerName;
}
