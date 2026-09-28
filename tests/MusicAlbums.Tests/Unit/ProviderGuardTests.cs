using Microsoft.Extensions.Options;
using MusicAlbums.Core.Exceptions;
using MusicAlbums.Core.Models;
using MusicAlbums.Infrastructure.Providers.Throttling;
using MusicAlbums.TestSupport;

namespace MusicAlbums.Tests.Unit;

public sealed class ProviderGuardTests
{
    private static ProviderGuard CreateGuard(int maxConcurrent = 1, int failureThreshold = 2, int breakSeconds = 30) =>
        new("deezer",
            new ProviderThrottleOptions
            {
                MaxConcurrentRequests = maxConcurrent,
                FailureThreshold = failureThreshold,
                BreakDurationSeconds = breakSeconds
            },
            new MutableTimeProvider(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero)));

    [Fact]
    public async Task ExecuteAsync_LimitsConcurrentRequestsPerProvider()
    {
        var guard = CreateGuard(maxConcurrent: 1);
        var release = new TaskCompletionSource();
        var secondStarted = false;

        var first = guard.ExecuteAsync(async _ =>
        {
            await release.Task;
            return "first";
        }, CancellationToken.None);

        var second = guard.ExecuteAsync(_ =>
        {
            secondStarted = true;
            return Task.FromResult("second");
        }, CancellationToken.None);

        await Task.Delay(50);
        Assert.False(secondStarted);

        release.SetResult();
        Assert.Equal("first", await first);
        Assert.Equal("second", await second);
    }

    [Fact]
    public async Task ExecuteAsync_OpensCircuitAfterConsecutiveFailures_AndSkipsTheProvider()
    {
        var guard = CreateGuard(failureThreshold: 2);
        var invocations = 0;

        Task<string> Failing()
        {
            invocations++;
            throw new AlbumProviderUnavailableException("deezer", "HTTP 503");
        }

        await Assert.ThrowsAsync<AlbumProviderUnavailableException>(() => guard.ExecuteAsync(_ => Failing(), CancellationToken.None));
        await Assert.ThrowsAsync<AlbumProviderUnavailableException>(() => guard.ExecuteAsync(_ => Failing(), CancellationToken.None));

        var circuitOpen = await Assert.ThrowsAsync<AlbumProviderUnavailableException>(() => guard.ExecuteAsync(_ => Failing(), CancellationToken.None));

        Assert.Contains("circuit is open", circuitOpen.Message, StringComparison.Ordinal);
        Assert.Equal(2, invocations);
    }

    [Fact]
    public async Task ExecuteAsync_ClosesCircuitAfterTheBreakDuration()
    {
        var time = new MutableTimeProvider(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero));
        var guard = new ProviderGuard("deezer",
            new ProviderThrottleOptions { MaxConcurrentRequests = 4, FailureThreshold = 1, BreakDurationSeconds = 30 },
            time);

        Task<string> Failing()
        {
            throw new AlbumProviderUnavailableException("deezer", "HTTP 503");
        }

        await Assert.ThrowsAsync<AlbumProviderUnavailableException>(() => guard.ExecuteAsync(_ => Failing(), CancellationToken.None));
        await Assert.ThrowsAsync<AlbumProviderUnavailableException>(() => guard.ExecuteAsync(_ => Failing(), CancellationToken.None));

        time.UtcNow = time.UtcNow.AddSeconds(31);

        var recovered = await guard.ExecuteAsync(_ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", recovered);
    }

    [Fact]
    public async Task ThrottledAlbumProvider_DelegatesToTheInnerProvider()
    {
        var inner = new StubAlbumProvider("deezer")
            .WithAlbum(new ProviderAlbum("deezer", "album-1", "Discovery", "Daft Punk", new DateOnly(2001, 3, 7), null, 1, "https://deezer.com/album/album-1", []));
        var registry = new ProviderGuardRegistry(Options.Create(new ProviderThrottleOptions()), TimeProvider.System);
        var provider = new ThrottledAlbumProvider(inner, registry);

        var album = await provider.GetAlbumAsync("album-1");

        Assert.Equal("deezer", provider.Name);
        Assert.Equal("Discovery", album.Title);
    }
}
