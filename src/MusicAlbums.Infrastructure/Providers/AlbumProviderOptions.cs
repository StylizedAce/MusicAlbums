using MusicAlbums.Core;

namespace MusicAlbums.Infrastructure.Providers;

public sealed class AlbumProviderOptions
{
    public const string SectionName = "AlbumProviders";

    public string Default { get; set; } = ProviderNames.Deezer;
}
