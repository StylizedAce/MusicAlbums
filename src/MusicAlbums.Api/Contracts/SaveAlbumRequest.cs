namespace MusicAlbums.Api.Contracts;

public sealed record SaveAlbumRequest(string? Provider, string? ProviderAlbumId);
