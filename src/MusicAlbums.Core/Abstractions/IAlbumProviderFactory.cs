namespace MusicAlbums.Core.Abstractions;

public interface IAlbumProviderFactory
{
    IReadOnlyList<IAlbumProvider> GetAll();

    IAlbumProvider GetRequired(string providerName);

    IAlbumProvider GetDefault();
}
