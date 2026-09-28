namespace MusicAlbums.Api.Contracts;

public sealed record AlbumResponse(
    string Provider,
    string ProviderAlbumId,
    string Title,
    string Artist,
    DateOnly? ReleaseDate,
    string? CoverImageUrl,
    int? TrackCount,
    IReadOnlyList<TrackResponse> Tracks);
