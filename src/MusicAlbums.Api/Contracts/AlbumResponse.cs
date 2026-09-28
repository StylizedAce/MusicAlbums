namespace MusicAlbums.Api.Contracts;

public sealed record AlbumResponse(
    string Provider,
    string ProviderAlbumId,
    string Title,
    string Artist,
    DateOnly? ReleaseDate,
    string? CoverImageUrl,
    int? TrackCount,
    string? ExternalUrl,
    IReadOnlyList<TrackResponse> Tracks);
