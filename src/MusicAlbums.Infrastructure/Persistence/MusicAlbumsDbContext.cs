using Microsoft.EntityFrameworkCore;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Persistence;

public sealed class MusicAlbumsDbContext(DbContextOptions<MusicAlbumsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();

    public DbSet<SavedAlbum> SavedAlbums => Set<SavedAlbum>();

    public DbSet<SavedTrack> SavedTracks => Set<SavedTrack>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MusicAlbumsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
