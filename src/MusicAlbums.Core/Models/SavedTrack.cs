namespace MusicAlbums.Core.Models;

public sealed class SavedTrack
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SavedAlbumId { get; set; }

    public int Position { get; set; }

    public required string Title { get; set; }

    public int? DurationSeconds { get; set; }
}
