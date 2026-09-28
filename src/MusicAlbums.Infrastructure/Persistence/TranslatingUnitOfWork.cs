using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Persistence;

public static class SqliteErrors
{
    private const int ConstraintErrorCode = 19;

    public static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: ConstraintErrorCode };
}

public sealed class TranslatingUnitOfWork(MusicAlbumsDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateAlbum(exception))
        {
            var album = exception.Entries
                .Select(entry => entry.Entity)
                .OfType<SavedAlbum>()
                .First();

            throw new AlbumAlreadyInLibraryException(album.ProviderName, album.ProviderAlbumId);
        }
    }

    private static bool IsDuplicateAlbum(DbUpdateException exception) =>
        exception.Entries.Any(entry => entry.Entity is SavedAlbum)
        && SqliteErrors.IsUniqueConstraintViolation(exception);
}
