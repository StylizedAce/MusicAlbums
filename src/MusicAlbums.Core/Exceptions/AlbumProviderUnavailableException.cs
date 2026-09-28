namespace MusicAlbums.Core.Exceptions;

public sealed class AlbumProviderUnavailableException(string providerName, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string ProviderName { get; } = providerName;
}
