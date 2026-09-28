using System.Globalization;
using MusicAlbums.Core;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Providers.Deezer;

internal static class DeezerAlbumMapper
{
    public static ProviderAlbum ToProviderAlbum(DeezerAlbumDto album)
    {
        var tracks = album.Tracks?.Data is { Count: > 0 } trackData
            ? trackData
                .Select((track, index) => new ProviderTrack(
                    track.TrackPosition ?? index + 1,
                    track.Title ?? $"Track {index + 1}",
                    track.Duration,
                    track.Preview))
                .ToArray()
            : [];

        return new ProviderAlbum(
            ProviderNames.Deezer,
            album.Id.ToString(CultureInfo.InvariantCulture),
            album.Title ?? string.Empty,
            album.Artist?.Name ?? "Unknown Artist",
            ParseReleaseDate(album.ReleaseDate),
            album.CoverXl ?? album.CoverBig,
            album.NbTracks,
            album.Link,
            tracks);
    }

    public static DateOnly? ParseReleaseDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
}
