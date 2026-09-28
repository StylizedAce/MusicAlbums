namespace MusicAlbums.Api.Contracts;

public sealed record SearchAlbumsResponse(IReadOnlyList<AlbumResponse> Albums, int Total, int Limit, int Offset);
