using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Tests.Support;

internal sealed class StubAlbumProvider(string name = "stub") : IAlbumProvider
{
    private readonly Dictionary<string, ProviderAlbum> _albums = new(StringComparer.Ordinal);

    public string Name { get; } = name;

    public StubAlbumProvider WithAlbum(ProviderAlbum album)
    {
        _albums[album.ProviderAlbumId] = album;
        return this;
    }

    public Task<AlbumSearchResult> SearchAlbumsAsync(AlbumSearchQuery query, CancellationToken cancellationToken = default)
    {
        var matches = _albums.Values
            .Where(album => album.Title.Contains(query.Text, StringComparison.OrdinalIgnoreCase)
                || album.ArtistName.Contains(query.Text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(album => album.ProviderAlbumId, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult(new AlbumSearchResult(matches, matches.Length, query));
    }

    public Task<ProviderAlbum> GetAlbumAsync(string providerAlbumId, CancellationToken cancellationToken = default) =>
        _albums.TryGetValue(providerAlbumId, out var album)
            ? Task.FromResult(album)
            : throw new AlbumProviderNotFoundException(Name, providerAlbumId);
}
