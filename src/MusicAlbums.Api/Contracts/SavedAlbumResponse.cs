namespace MusicAlbums.Api.Contracts;

public sealed record SavedAlbumResponse(
    Guid Id,
    string Provider,
    string ProviderAlbumId,
    string Title,
    string Artist,
    DateOnly? ReleaseDate,
    string? CoverImageUrl,
    int? TrackCount,
    DateTimeOffset SavedAt,
    IReadOnlyList<TrackResponse> Tracks);
