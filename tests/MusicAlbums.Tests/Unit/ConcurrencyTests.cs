using Microsoft.EntityFrameworkCore;
using MusicAlbums.Core.Application;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;
using MusicAlbums.Infrastructure.Persistence;
using MusicAlbums.Infrastructure.Persistence.Repositories;
using MusicAlbums.TestSupport;

namespace MusicAlbums.Tests.Unit;

public sealed class ConcurrencyTests : IDisposable
{
    private readonly List<MusicAlbumsDbContext> _contexts = [];
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"musicalbums-race-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task SaveChangesAsync_WhenTheAlbumAlreadyExists_ThrowsAlbumAlreadyInLibrary()
    {
        var owner = new User { Name = "alice" };
        var first = new MusicAlbumsDbContext(CreateOptions());
        first.Database.Migrate();
        _contexts.Add(first);
        first.Users.Add(owner);
        first.SavedAlbums.Add(SavedAlbum.FromProvider(owner, Album("album-1"), DateTimeOffset.UtcNow));
        await first.SaveChangesAsync();

        var second = new MusicAlbumsDbContext(CreateOptions());
        _contexts.Add(second);
        var persistedOwner = await second.Users.SingleAsync(candidate => candidate.Name == "alice");
        second.SavedAlbums.Add(SavedAlbum.FromProvider(persistedOwner, Album("album-1"), DateTimeOffset.UtcNow));

        var exception = await Assert.ThrowsAsync<AlbumAlreadyInLibraryException>(
            () => new TranslatingUnitOfWork(second).SaveChangesAsync());

        Assert.Equal("album-1", exception.ProviderAlbumId);
        Assert.Equal("deezer", exception.ProviderName);
    }

    [Fact]
    public async Task GetOrCreateAsync_CalledTwiceFromDifferentContexts_ReturnsTheSameUser()
    {
        var first = new MusicAlbumsDbContext(CreateOptions());
        first.Database.Migrate();
        _contexts.Add(first);

        var created = await new UserRepository(first).GetOrCreateAsync("bob");

        var second = new MusicAlbumsDbContext(CreateOptions());
        _contexts.Add(second);
        var found = await new UserRepository(second).GetOrCreateAsync("bob");

        Assert.Equal(created.Id, found.Id);
        Assert.Equal(1, await first.Users.CountAsync());
    }

    [Fact]
    public async Task AddAlbumAsync_AfterAnotherRequestSavedTheSameAlbum_ReportsConflict()
    {
        var provider = new StubAlbumProvider("deezer").WithAlbum(Album("album-1"));
        var context = new MusicAlbumsDbContext(CreateOptions());
        context.Database.Migrate();
        _contexts.Add(context);
        var service = CreateService(provider, context);

        await service.AddAlbumAsync("carol", null, "album-1");
        var exception = await Assert.ThrowsAsync<AlbumAlreadyInLibraryException>(() => service.AddAlbumAsync("carol", null, "album-1"));

        Assert.Equal("album-1", exception.ProviderAlbumId);
        Assert.Single(await new SavedAlbumRepository(context).GetByUserAsync((await context.Users.SingleAsync()).Id));
    }

    public void Dispose()
    {
        foreach (var context in _contexts)
        {
            context.Dispose();
        }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }

    private DbContextOptions<MusicAlbumsDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<MusicAlbumsDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

    private LibraryService CreateService(StubAlbumProvider provider, MusicAlbumsDbContext context) =>
        new(TestProviderFactory.Create("deezer", provider),
            new UserRepository(context),
            new SavedAlbumRepository(context),
            new TranslatingUnitOfWork(context),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

    private static ProviderAlbum Album(string id) =>
        new("deezer", id, "Discovery", "Daft Punk", new DateOnly(2001, 3, 7), null, 1, null, []);
}
