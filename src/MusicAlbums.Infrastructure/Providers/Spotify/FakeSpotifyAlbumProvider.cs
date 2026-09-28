using MusicAlbums.Core;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Providers.Spotify;

public sealed class FakeSpotifyAlbumProvider : IAlbumProvider
{
    public string Name => ProviderNames.Spotify;

    private static readonly IReadOnlyList<ProviderAlbum> Catalog =
    [
        new(ProviderNames.Spotify, "alb-001", "Discovery", "Daft Punk", new DateOnly(2001, 3, 7), null, 14,
        [
            new ProviderTrack(1, "One More Time", 320),
            new ProviderTrack(2, "Aerodynamic", 212),
            new ProviderTrack(3, "Digital Love", 301),
            new ProviderTrack(4, "Harder, Better, Faster, Stronger", 224)
        ]),
        new(ProviderNames.Spotify, "alb-002", "Random Access Memories", "Daft Punk", new DateOnly(2013, 5, 17), null, 13,
        [
            new ProviderTrack(1, "Get Lucky", 369),
            new ProviderTrack(2, "Instant Crush", 337),
            new ProviderTrack(3, "Lose Yourself to Dance", 353)
        ]),
        new(ProviderNames.Spotify, "alb-003", "OK Computer", "Radiohead", new DateOnly(1997, 5, 28), null, 12,
        [
            new ProviderTrack(1, "Paranoid Android", 383),
            new ProviderTrack(2, "Karma Police", 261),
            new ProviderTrack(3, "No Surprises", 229)
        ]),
        new(ProviderNames.Spotify, "alb-004", "Kid A", "Radiohead", new DateOnly(2000, 10, 2), null, 10,
        [
            new ProviderTrack(1, "Everything In Its Right Place", 251),
            new ProviderTrack(2, "Idioteque", 309)
        ]),
        new(ProviderNames.Spotify, "alb-005", "Thriller", "Michael Jackson", new DateOnly(1982, 11, 30), null, 9,
        [
            new ProviderTrack(1, "Wanna Be Startin' Somethin'", 363),
            new ProviderTrack(2, "Thriller", 358),
            new ProviderTrack(3, "Billie Jean", 294)
        ])
    ];

    public Task<AlbumSearchResult> SearchAlbumsAsync(AlbumSearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Text);

        var limit = Math.Clamp(query.Limit, 1, AlbumSearchQuery.MaxLimit);
        var offset = Math.Max(query.Offset, 0);

        var matches = Catalog
            .Where(album =>
                album.Title.Contains(query.Text, StringComparison.OrdinalIgnoreCase)
                || album.ArtistName.Contains(query.Text, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var page = matches.Skip(offset).Take(limit).ToArray();

        return Task.FromResult(new AlbumSearchResult(page, matches.Length, query with { Limit = limit, Offset = offset }));
    }

    public Task<ProviderAlbum> GetAlbumAsync(string providerAlbumId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerAlbumId);

        var album = Catalog.FirstOrDefault(candidate => string.Equals(candidate.ProviderAlbumId, providerAlbumId, StringComparison.Ordinal));

        return album is null
            ? throw new AlbumProviderNotFoundException(Name, providerAlbumId)
            : Task.FromResult(album);
    }
}
