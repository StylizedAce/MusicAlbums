using Microsoft.Extensions.Options;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Infrastructure.Providers;

namespace MusicAlbums.Tests.Support;

internal static class TestProviderFactory
{
    public static IAlbumProviderFactory Create(string defaultProviderName, params IAlbumProvider[] providers) =>
        new AlbumProviderFactory(providers, Options.Create(new AlbumProviderOptions { Default = defaultProviderName }));
}
