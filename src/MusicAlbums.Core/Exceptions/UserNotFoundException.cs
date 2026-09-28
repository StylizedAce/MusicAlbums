namespace MusicAlbums.Core.Exceptions;

public sealed class UserNotFoundException(string userName)
    : Exception($"Library user '{userName}' was not found.")
{
    public string UserName { get; } = userName;
}
