using Microsoft.EntityFrameworkCore;
using MusicAlbums.Core.Application;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;
using MusicAlbums.Infrastructure.Persistence.Repositories;
using MusicAlbums.Tests.Support;

namespace MusicAlbums.Tests.Unit;

public sealed class LibraryServiceTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteTestDatabase _database = new();
    private readonly StubAlbumProvider _provider = new("deezer");

    public LibraryServiceTests()
    {
        _provider
            .WithAlbum(Album("album-1", "Discovery"))
            .WithAlbum(Album("album-2", "Random Access Memories"));
    }

    [Fact]
    public async Task AddAlbumAsync_CreatesUserOnTheFly_AndSnapshotsAlbumWithTracks()
    {
        var service = CreateService();

        var saved = await service.AddAlbumAsync("Alice", null, "album-1");

        Assert.Equal("deezer", saved.ProviderName);
        Assert.Equal("Discovery", saved.Title);
        Assert.Equal("Daft Punk", saved.ArtistName);
        Assert.Equal(new DateOnly(2001, 3, 7), saved.ReleaseDate);
        Assert.Equal("https://www.deezer.com/album/album-1", saved.ExternalUrl);
        Assert.Equal(2, saved.TrackCount);
        Assert.Equal(Now, saved.SavedAt);
        Assert.Equal(2, saved.Tracks.Count);

        var user = await _database.Context.Users.SingleAsync();
        Assert.Equal("Alice", user.Name);

        var library = await service.GetLibraryAsync("alice");
        var album = Assert.Single(library);
        Assert.Equal("album-1", album.ProviderAlbumId);
        Assert.Equal(new[] { 1, 2 }, album.Tracks.Select(track => track.Position).ToArray());
        Assert.Equal("https://preview.example/track-one.mp3", album.Tracks[0].PreviewUrl);
    }

    [Fact]
    public async Task AddAlbumAsync_DuplicateForSameUser_Throws_AndKeepsSingleEntry()
    {
        var service = CreateService();
        await service.AddAlbumAsync("Bob", null, "album-1");

        await Assert.ThrowsAsync<AlbumAlreadyInLibraryException>(() => service.AddAlbumAsync("bob", null, "album-1"));

        Assert.Single(await service.GetLibraryAsync("Bob"));
    }

    [Fact]
    public async Task AddAlbumAsync_SameAlbumForDifferentUsers_KeepsLibrariesSeparate()
    {
        var service = CreateService();
        await service.AddAlbumAsync("Alice", null, "album-1");
        await service.AddAlbumAsync("Bob", null, "album-1");

        Assert.Single(await service.GetLibraryAsync("Alice"));
        Assert.Single(await service.GetLibraryAsync("Bob"));
    }

    [Fact]
    public async Task AddAlbumAsync_UnknownProvider_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UnknownAlbumProviderException>(() => service.AddAlbumAsync("Alice", "napster", "album-1"));
    }

    [Fact]
    public async Task AddAlbumAsync_AlbumUnknownToProvider_Throws_AndPersistsNothing()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<AlbumProviderNotFoundException>(() => service.AddAlbumAsync("Carol", null, "missing"));

        Assert.Empty(await _database.Context.Users.ToListAsync());
    }

    [Fact]
    public async Task GetLibraryAsync_UnknownUser_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<UserNotFoundException>(() => service.GetLibraryAsync("nobody"));
    }

    [Fact]
    public async Task RemoveAlbumAsync_RemovesOnlyRequestedAlbum_AndReportsMissingOnes()
    {
        var service = CreateService();
        var first = await service.AddAlbumAsync("Dana", null, "album-1");
        await service.AddAlbumAsync("Dana", null, "album-2");

        await service.RemoveAlbumAsync("dana", first.Id);

        var remaining = Assert.Single(await service.GetLibraryAsync("Dana"));
        Assert.Equal("album-2", remaining.ProviderAlbumId);

        await Assert.ThrowsAsync<AlbumNotInLibraryException>(() => service.RemoveAlbumAsync("Dana", first.Id));
        await Assert.ThrowsAsync<AlbumNotInLibraryException>(() => service.RemoveAlbumAsync("Dana", Guid.NewGuid()));
    }

    public void Dispose() => _database.Dispose();

    private static ProviderAlbum Album(string id, string title) =>
        new("deezer",
            id,
            title,
            "Daft Punk",
            new DateOnly(2001, 3, 7),
            "https://cover.example/x.jpg",
            2,
            $"https://www.deezer.com/album/{id}",
            [
                new ProviderTrack(1, "Track One", 100, "https://preview.example/track-one.mp3"),
                new ProviderTrack(2, "Track Two", 200)
            ]);

    private LibraryService CreateService() =>
        new(TestProviderFactory.Create("deezer", _provider),
            new UserRepository(_database.Context),
            new SavedAlbumRepository(_database.Context),
            _database.Context,
            new FixedTimeProvider(Now));
}
