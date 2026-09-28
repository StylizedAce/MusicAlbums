using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.TestSupport;

namespace MusicAlbums.Tests.Unit;

public sealed class AlbumProviderFactoryTests
{
    private readonly StubAlbumProvider _deezer = new("deezer");
    private readonly StubAlbumProvider _spotify = new("spotify");

    private IAlbumProviderFactory Factory => TestProviderFactory.Create("deezer", _deezer, _spotify);

    [Fact]
    public void GetRequired_ResolvesProviderNameCaseInsensitively()
    {
        Assert.Same(_deezer, Factory.GetRequired("DEeZER"));
    }

    [Fact]
    public void GetRequired_UnknownProvider_Throws()
    {
        var exception = Assert.Throws<UnknownAlbumProviderException>(() => Factory.GetRequired("napster"));

        Assert.Equal("napster", exception.ProviderName);
    }

    [Fact]
    public void GetDefault_ReturnsConfiguredDefaultProvider()
    {
        Assert.Same(_deezer, Factory.GetDefault());
    }

    [Fact]
    public void GetAll_ReturnsEveryRegisteredProvider()
    {
        var names = Factory.GetAll().Select(provider => provider.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray();

        Assert.Equal(new[] { "deezer", "spotify" }, names);
    }
}
