namespace MusicAlbums.Core.Models;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<SavedAlbum> Albums { get; set; } = [];
}
