namespace MusicAlbums.Core.Models;

public sealed class SavedAlbum
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string ProviderName { get; set; }

    public required string ProviderAlbumId { get; set; }

    public required string Title { get; set; }

    public required string ArtistName { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? CoverImageUrl { get; set; }

    public int? TrackCount { get; set; }

    public string? ExternalUrl { get; set; }

    public DateTimeOffset SavedAt { get; set; }

    public List<SavedTrack> Tracks { get; set; } = [];

    public static SavedAlbum FromProvider(User user, ProviderAlbum album, DateTimeOffset savedAt)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(album);

        return new SavedAlbum
        {
            UserId = user.Id,
            User = user,
            ProviderName = album.ProviderName,
            ProviderAlbumId = album.ProviderAlbumId,
            Title = album.Title,
            ArtistName = album.ArtistName,
            ReleaseDate = album.ReleaseDate,
            CoverImageUrl = album.CoverImageUrl,
            TrackCount = album.TrackCount ?? (album.Tracks.Count > 0 ? album.Tracks.Count : null),
            ExternalUrl = album.ExternalUrl,
            SavedAt = savedAt,
            Tracks = [.. album.Tracks.Select(track => new SavedTrack
            {
                Position = track.Position,
                Title = track.Title,
                DurationSeconds = track.DurationSeconds,
                PreviewUrl = track.PreviewUrl
            })]
        };
    }
}
