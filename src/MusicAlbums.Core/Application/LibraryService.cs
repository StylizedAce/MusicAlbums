using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Core.Application;

public sealed class LibraryService(
    IAlbumProviderFactory providerFactory,
    IUserRepository userRepository,
    ISavedAlbumRepository savedAlbumRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ILibraryService
{
    public async Task<SavedAlbum> AddAlbumAsync(string userName, string? providerName, string providerAlbumId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerAlbumId);

        var provider = ResolveProvider(providerName);
        var normalizedUserName = userName.Trim();

        var providerAlbum = await provider.GetAlbumAsync(providerAlbumId, cancellationToken);

        var user = await userRepository.GetOrCreateAsync(normalizedUserName, cancellationToken);

        if (await savedAlbumRepository.ExistsAsync(user.Id, provider.Name, providerAlbum.ProviderAlbumId, cancellationToken))
        {
            throw new AlbumAlreadyInLibraryException(provider.Name, providerAlbum.ProviderAlbumId);
        }

        var savedAlbum = SavedAlbum.FromProvider(user, providerAlbum, timeProvider.GetUtcNow());
        savedAlbumRepository.Add(savedAlbum);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return savedAlbum;
    }

    public async Task<IReadOnlyList<SavedAlbum>> GetLibraryAsync(string userName, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userName, cancellationToken);
        return await savedAlbumRepository.GetByUserAsync(user.Id, cancellationToken);
    }

    public async Task<SavedAlbum> GetAlbumAsync(string userName, Guid albumId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userName, cancellationToken);
        return await savedAlbumRepository.FindAsync(user.Id, albumId, cancellationToken)
            ?? throw new AlbumNotInLibraryException(albumId);
    }

    public async Task RemoveAlbumAsync(string userName, Guid albumId, CancellationToken cancellationToken = default)
    {
        var user = await FindUserAsync(userName, cancellationToken);
        var album = await savedAlbumRepository.FindAsync(user.Id, albumId, cancellationToken)
            ?? throw new AlbumNotInLibraryException(albumId);

        savedAlbumRepository.Remove(album);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> FindUserAsync(string userName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        var normalizedUserName = userName.Trim();

        return await userRepository.FindByNameAsync(normalizedUserName, cancellationToken)
            ?? throw new UserNotFoundException(normalizedUserName);
    }

    private IAlbumProvider ResolveProvider(string? providerName) =>
        string.IsNullOrWhiteSpace(providerName) ? providerFactory.GetDefault() : providerFactory.GetRequired(providerName);
}
