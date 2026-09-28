using Microsoft.EntityFrameworkCore;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(MusicAlbumsDbContext dbContext) : IUserRepository
{
    public Task<User?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return dbContext.Users.FirstOrDefaultAsync(user => user.Name == name, cancellationToken);
    }

    public async Task<User> GetOrCreateAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var existing = await FindByNameAsync(name, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var user = new User { Name = name };
        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return user;
        }
        catch (DbUpdateException exception) when (SqliteErrors.IsUniqueConstraintViolation(exception))
        {
            dbContext.Entry(user).State = EntityState.Detached;
            return await dbContext.Users.FirstAsync(candidate => candidate.Name == name, cancellationToken);
        }
    }
}
