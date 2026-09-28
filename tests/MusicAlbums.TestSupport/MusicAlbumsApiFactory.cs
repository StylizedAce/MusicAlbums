using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MusicAlbums.Infrastructure.Persistence;
using MusicAlbums.Infrastructure.Providers.Deezer;
using MusicAlbums.Infrastructure.Providers.Spotify;

namespace MusicAlbums.TestSupport;

public sealed class MusicAlbumsApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public MusicAlbumsApiFactory()
    {
        _connection.Open();
        DeezerHandler = new StubHttpMessageHandler(DeezerTestData.Respond);
        SpotifyAccountsHandler = new StubHttpMessageHandler(_ => SpotifyTestData.Json(SpotifyTestData.TokenJson));
        SpotifyApiHandler = new StubHttpMessageHandler(SpotifyTestData.Respond);
    }

    public StubHttpMessageHandler DeezerHandler { get; }

    public StubHttpMessageHandler SpotifyAccountsHandler { get; }

    public StubHttpMessageHandler SpotifyApiHandler { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MusicAlbumsDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<MusicAlbumsDbContext>>();
            services.RemoveAll<MusicAlbumsDbContext>();

            services.AddDbContext<MusicAlbumsDbContext>(options => options.UseSqlite(_connection));

            services.AddHttpClient<DeezerAlbumProvider>()
                .ConfigurePrimaryHttpMessageHandler(() => DeezerHandler);
            services.AddHttpClient(SpotifyHttpClients.Accounts)
                .ConfigurePrimaryHttpMessageHandler(() => SpotifyAccountsHandler);
            services.AddHttpClient<SpotifyAlbumProvider>()
                .ConfigurePrimaryHttpMessageHandler(() => SpotifyApiHandler);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
