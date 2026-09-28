using Microsoft.Extensions.Options;
using MusicAlbums.Core.Abstractions;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;

namespace MusicAlbums.Infrastructure.Providers.Throttling;

public sealed class ProviderThrottleOptions
{
    public const string SectionName = "AlbumProviders:Throttling";

    public int MaxConcurrentRequests { get; set; } = 4;

    public int FailureThreshold { get; set; } = 5;

    public int BreakDurationSeconds { get; set; } = 30;
}

public sealed class ProviderGuard(string providerName, ProviderThrottleOptions options, TimeProvider timeProvider)
{
    private readonly SemaphoreSlim _concurrency = new(options.MaxConcurrentRequests, options.MaxConcurrentRequests);
    private readonly Lock _circuitLock = new();
    private int _consecutiveFailures;
    private DateTimeOffset? _circuitOpenedUntil;

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ThrowIfCircuitOpen();

        await _concurrency.WaitAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            RecordSuccess();
            return result;
        }
        catch (AlbumProviderUnavailableException)
        {
            RecordFailure();
            throw;
        }
        finally
        {
            _concurrency.Release();
        }
    }

    private void ThrowIfCircuitOpen()
    {
        lock (_circuitLock)
        {
            if (_circuitOpenedUntil is not { } openedUntil)
            {
                return;
            }

            if (timeProvider.GetUtcNow() < openedUntil)
            {
                throw new AlbumProviderUnavailableException(providerName, $"The {providerName} circuit is open after repeated failures; retry in a moment.");
            }

            _consecutiveFailures = 0;
            _circuitOpenedUntil = null;
        }
    }

    private void RecordSuccess()
    {
        lock (_circuitLock)
        {
            _consecutiveFailures = 0;
        }
    }

    private void RecordFailure()
    {
        lock (_circuitLock)
        {
            if (++_consecutiveFailures < options.FailureThreshold)
            {
                return;
            }

            _consecutiveFailures = 0;
            _circuitOpenedUntil = timeProvider.GetUtcNow().AddSeconds(options.BreakDurationSeconds);
        }
    }
}

public sealed class ProviderGuardRegistry(IOptions<ProviderThrottleOptions> options, TimeProvider timeProvider)
{
    private readonly Dictionary<string, ProviderGuard> _guards = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    public ProviderGuard For(string providerName)
    {
        lock (_lock)
        {
            if (!_guards.TryGetValue(providerName, out var guard))
            {
                guard = new ProviderGuard(providerName, options.Value, timeProvider);
                _guards[providerName] = guard;
            }

            return guard;
        }
    }
}

public sealed class ThrottledAlbumProvider(IAlbumProvider inner, ProviderGuardRegistry guards) : IAlbumProvider
{
    public string Name => inner.Name;

    public Task<AlbumSearchResult> SearchAlbumsAsync(AlbumSearchQuery query, CancellationToken cancellationToken = default) =>
        guards.For(inner.Name).ExecuteAsync(token => inner.SearchAlbumsAsync(query, token), cancellationToken);

    public Task<ProviderAlbum> GetAlbumAsync(string providerAlbumId, CancellationToken cancellationToken = default) =>
        guards.For(inner.Name).ExecuteAsync(token => inner.GetAlbumAsync(providerAlbumId, token), cancellationToken);
}
