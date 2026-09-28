using MusicAlbums.Core.Models;

namespace MusicAlbums.Api.Contracts;

internal static class ContractMappings
{
    public static TrackResponse ToResponse(this ProviderTrack track) =>
        new(track.Position, track.Title, track.DurationSeconds, track.PreviewUrl);

    public static AlbumResponse ToResponse(this ProviderAlbum album) =>
        new(album.ProviderName,
            album.ProviderAlbumId,
            album.Title,
            album.ArtistName,
            album.ReleaseDate,
            album.CoverImageUrl,
            album.TrackCount,
            album.ExternalUrl,
            [.. album.Tracks.Select(track => track.ToResponse())]);

    public static SearchAlbumsResponse ToResponse(this AlbumSearchResult result) =>
        new([.. result.Albums.Select(album => album.ToResponse())], result.Total, result.Query.Limit, result.Query.Offset);

    public static SavedAlbumResponse ToResponse(this SavedAlbum album) =>
        new(album.Id,
            album.ProviderName,
            album.ProviderAlbumId,
            album.Title,
            album.ArtistName,
            album.ReleaseDate,
            album.CoverImageUrl,
            album.TrackCount,
            album.ExternalUrl,
            album.SavedAt,
            [.. album.Tracks.Select(track => new TrackResponse(track.Position, track.Title, track.DurationSeconds, track.PreviewUrl))]);
}
