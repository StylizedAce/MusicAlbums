namespace MusicAlbums.Core.Models;

public sealed record AlbumSearchResult(IReadOnlyList<ProviderAlbum> Albums, int Total, AlbumSearchQuery Query);
