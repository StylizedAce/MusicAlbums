namespace MusicAlbums.Core.Models;

public sealed record ProviderTrack(int Position, string Title, int? DurationSeconds, string? PreviewUrl = null);
