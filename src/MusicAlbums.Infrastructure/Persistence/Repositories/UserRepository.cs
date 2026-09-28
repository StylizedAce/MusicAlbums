using Microsoft.EntityFrameworkCore;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(MusicAlbumsDbContext dbContext) : IUserRepository
{
    public Task<User?> FindByNameAsync(string name, CancellationToken cancellationToken = default) =>
        dbContext.Users.FirstOrDefaultAsync(user => user.Name == name, cancellationToken);

    public void Add(User user) => dbContext.Users.Add(user);
}
