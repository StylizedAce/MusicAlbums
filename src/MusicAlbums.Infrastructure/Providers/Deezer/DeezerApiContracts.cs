using System.Text.Json.Serialization;

namespace MusicAlbums.Infrastructure.Providers.Deezer;

internal sealed record DeezerSearchResponse(
    [property: JsonPropertyName("data")] IReadOnlyList<DeezerAlbumDto>? Data,
    [property: JsonPropertyName("total")] int? Total,
    [property: JsonPropertyName("error")] DeezerErrorDto? Error);

internal sealed record DeezerAlbumDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("link")] string? Link,
    [property: JsonPropertyName("cover_xl")] string? CoverXl,
    [property: JsonPropertyName("cover_big")] string? CoverBig,
    [property: JsonPropertyName("nb_tracks")] int? NbTracks,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("artist")] DeezerArtistDto? Artist,
    [property: JsonPropertyName("tracks")] DeezerTracksContainer? Tracks)
{
    [JsonPropertyName("error")]
    public DeezerErrorDto? Error { get; init; }
}

internal sealed record DeezerArtistDto(
    [property: JsonPropertyName("name")] string? Name);

internal sealed record DeezerTracksContainer(
    [property: JsonPropertyName("data")] IReadOnlyList<DeezerTrackDto>? Data);

internal sealed record DeezerTrackDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("duration")] int? Duration,
    [property: JsonPropertyName("track_position")] int? TrackPosition,
    [property: JsonPropertyName("preview")] string? Preview);

internal sealed record DeezerErrorDto(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("code")] int? Code);
