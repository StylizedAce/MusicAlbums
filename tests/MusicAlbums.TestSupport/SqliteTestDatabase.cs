using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MusicAlbums.Infrastructure.Persistence;

namespace MusicAlbums.TestSupport;

public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();

        var options = new DbContextOptionsBuilder<MusicAlbumsDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new MusicAlbumsDbContext(options);
        Context.Database.Migrate();
    }

    public MusicAlbumsDbContext Context { get; }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
