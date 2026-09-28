namespace MusicAlbums.Api.Contracts;

public sealed record TrackResponse(int Position, string Title, int? DurationSeconds, string? PreviewUrl);
