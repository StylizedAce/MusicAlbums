using MusicAlbums.Core.Models;

namespace MusicAlbums.Core.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<User> GetOrCreateAsync(string name, CancellationToken cancellationToken = default);
}
