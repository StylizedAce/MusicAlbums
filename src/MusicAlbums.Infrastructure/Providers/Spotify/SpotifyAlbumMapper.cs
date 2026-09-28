using System.Globalization;
using MusicAlbums.Core;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Providers.Spotify;

internal static class SpotifyAlbumMapper
{
    public static ProviderAlbum ToProviderAlbum(SpotifyAlbumDto album)
    {
        var tracks = album.Tracks?.Items is { Count: > 0 } trackItems
            ? trackItems
                .Where(track => track is not null)
                .Select((track, index) => new ProviderTrack(
                    track!.TrackNumber ?? index + 1,
                    track.Name ?? $"Track {index + 1}",
                    track.DurationMs is { } milliseconds ? (int?)Math.Round(milliseconds / 1000.0) : null,
                    track.PreviewUrl))
                .ToArray()
            : [];

        var coverImageUrl = album.Images?
            .Where(image => !string.IsNullOrWhiteSpace(image.Url))
            .OrderByDescending(image => image.Width ?? 0)
            .FirstOrDefault()?
            .Url;

        return new ProviderAlbum(
            ProviderNames.Spotify,
            album.Id ?? string.Empty,
            album.Name ?? string.Empty,
            album.Artists?.FirstOrDefault(artist => !string.IsNullOrWhiteSpace(artist.Name))?.Name ?? "Unknown Artist",
            ParseReleaseDate(album.ReleaseDate, album.ReleaseDatePrecision),
            coverImageUrl,
            album.TotalTracks,
            tracks);
    }

    public static DateOnly? ParseReleaseDate(string? value, string? precision)
    {
        var format = precision switch
        {
            "day" => "yyyy-MM-dd",
            "month" => "yyyy-MM",
            "year" => "yyyy",
            _ => null
        };

        return format is not null
            && DateOnly.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date
                : null;
    }
}
