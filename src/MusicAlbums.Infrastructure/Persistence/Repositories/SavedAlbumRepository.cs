using Microsoft.EntityFrameworkCore;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Persistence.Repositories;

public sealed class SavedAlbumRepository(MusicAlbumsDbContext dbContext) : ISavedAlbumRepository
{
    public async Task<IReadOnlyList<SavedAlbum>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.SavedAlbums
            .AsNoTracking()
            .Where(album => album.UserId == userId)
            .Include(album => album.Tracks.OrderBy(track => track.Position))
            .OrderByDescending(album => album.SavedAt)
            .ToListAsync(cancellationToken);

    public Task<SavedAlbum?> FindAsync(Guid userId, Guid albumId, CancellationToken cancellationToken = default) =>
        dbContext.SavedAlbums
            .Include(album => album.Tracks.OrderBy(track => track.Position))
            .FirstOrDefaultAsync(album => album.UserId == userId && album.Id == albumId, cancellationToken);

    public Task<bool> ExistsAsync(Guid userId, string providerName, string providerAlbumId, CancellationToken cancellationToken = default) =>
        dbContext.SavedAlbums.AnyAsync(
            album => album.UserId == userId
                && album.ProviderName == providerName
                && album.ProviderAlbumId == providerAlbumId,
            cancellationToken);

    public void Add(SavedAlbum album) => dbContext.SavedAlbums.Add(album);

    public void Remove(SavedAlbum album) => dbContext.SavedAlbums.Remove(album);
}
