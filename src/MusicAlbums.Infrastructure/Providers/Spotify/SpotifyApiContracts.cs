using System.Text.Json.Serialization;

namespace MusicAlbums.Infrastructure.Providers.Spotify;

internal sealed record SpotifyTokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("token_type")] string? TokenType,
    [property: JsonPropertyName("expires_in")] int ExpiresIn);

internal sealed record SpotifySearchResponse(
    [property: JsonPropertyName("albums")] SpotifyAlbumPage? Albums);

internal sealed record SpotifyAlbumPage(
    [property: JsonPropertyName("items")] IReadOnlyList<SpotifyAlbumDto>? Items,
    [property: JsonPropertyName("total")] int? Total);

internal sealed record SpotifyAlbumDto(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("release_date")] string? ReleaseDate,
    [property: JsonPropertyName("release_date_precision")] string? ReleaseDatePrecision,
    [property: JsonPropertyName("total_tracks")] int? TotalTracks,
    [property: JsonPropertyName("external_urls")] SpotifyExternalUrlsDto? ExternalUrls,
    [property: JsonPropertyName("images")] IReadOnlyList<SpotifyImageDto>? Images,
    [property: JsonPropertyName("artists")] IReadOnlyList<SpotifyArtistDto>? Artists,
    [property: JsonPropertyName("tracks")] SpotifyTrackPage? Tracks);

internal sealed record SpotifyExternalUrlsDto(
    [property: JsonPropertyName("spotify")] string? Spotify);

internal sealed record SpotifyImageDto(
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("width")] int? Width,
    [property: JsonPropertyName("height")] int? Height);

internal sealed record SpotifyArtistDto(
    [property: JsonPropertyName("name")] string? Name);

internal sealed record SpotifyTrackPage(
    [property: JsonPropertyName("items")] IReadOnlyList<SpotifyTrackDto?>? Items);

internal sealed record SpotifyTrackDto(
    [property: JsonPropertyName("track_number")] int? TrackNumber,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("duration_ms")] int? DurationMs,
    [property: JsonPropertyName("preview_url")] string? PreviewUrl);

internal sealed record SpotifyErrorResponse(
    [property: JsonPropertyName("error")] SpotifyErrorDto? Error);

internal sealed record SpotifyErrorDto(
    [property: JsonPropertyName("status")] int? Status,
    [property: JsonPropertyName("message")] string? Message);
