namespace MusicAlbums.Core.Models;

public sealed record ProviderAlbum(
    string ProviderName,
    string ProviderAlbumId,
    string Title,
    string ArtistName,
    DateOnly? ReleaseDate,
    string? CoverImageUrl,
    int? TrackCount,
    string? ExternalUrl,
    IReadOnlyList<ProviderTrack> Tracks);
