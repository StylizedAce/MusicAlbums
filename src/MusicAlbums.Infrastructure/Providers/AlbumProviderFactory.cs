using Microsoft.Extensions.Options;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Exceptions;

namespace MusicAlbums.Infrastructure.Providers;

public sealed class AlbumProviderFactory : IAlbumProviderFactory
{
    private readonly Dictionary<string, IAlbumProvider> _providers;
    private readonly AlbumProviderOptions _options;

    public AlbumProviderFactory(IEnumerable<IAlbumProvider> providers, IOptions<AlbumProviderOptions> options)
    {
        _providers = providers.ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);
        _options = options.Value;
    }

    public IReadOnlyList<IAlbumProvider> GetAll() => [.. _providers.Values];

    public IAlbumProvider GetRequired(string providerName) =>
        _providers.TryGetValue(providerName, out var provider)
            ? provider
            : throw new UnknownAlbumProviderException(providerName);

    public IAlbumProvider GetDefault() => GetRequired(_options.Default);
}
