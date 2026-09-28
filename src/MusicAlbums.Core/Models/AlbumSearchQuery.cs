namespace MusicAlbums.Core.Models;

public sealed record AlbumSearchQuery(string Text, int Limit = 25, int Offset = 0)
{
    public const int MaxLimit = 100;
}
