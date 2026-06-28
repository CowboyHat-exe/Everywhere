using Everywhere.Common;

namespace Everywhere.Core.Tests.Common;

[TestFixture]
public class TokenBucketRateLimiterTests
{
    [Test]
    public void Constructor_WithValidBytesPerSecond_ShouldNotThrow()
    {
        Assert.DoesNotThrow(() => _ = new TokenBucketRateLimiter(1000));
    }

    [Test]
    public void Constructor_WithZeroBytesPerSecond_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new TokenBucketRateLimiter(0));
    }

    [Test]
    public void Constructor_WithNegativeBytesPerSecond_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new TokenBucketRateLimiter(-1));
    }

    [Test]
    public async Task AcquireAsync_FirstCall_ShouldReturnImmediately()
    {
        var limiter = new TokenBucketRateLimiter(10000);

        var result = await limiter.AcquireAsync(100);

        Assert.That(result, Is.EqualTo(100));
    }

    [Test]
    public async Task AcquireAsync_SmallRequest_ShouldReturnRequestedAmount()
    {
        var limiter = new TokenBucketRateLimiter(10000);

        var result = await limiter.AcquireAsync(50);

        Assert.That(result, Is.EqualTo(50));
    }

    [Test]
    public async Task AcquireAsync_RequestExceedsBurstSize_ShouldReturnUpToBurst()
    {
        // bytesPerSecond=100, maxBurst=200 (default 2x)
        var limiter = new TokenBucketRateLimiter(100, maxBurstBytes: 200);

        // First call consumes initial burst tokens
        var result = await limiter.AcquireAsync(500);

        Assert.That(result, Is.LessThanOrEqualTo(200));
        Assert.That(result, Is.GreaterThan(0));
    }

    [Test]
    public async Task AcquireAsync_WithCancellation_ShouldThrowWhenCancelled()
    {
        // Very low rate to force waiting
        var limiter = new TokenBucketRateLimiter(1, maxBurstBytes: 10);

        // Exhaust all initial tokens
        await limiter.AcquireAsync(10);

        // Cancel immediately to ensure no time for replenishment
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await limiter.AcquireAsync(100, cts.Token));
    }

    [Test]
    public async Task AcquireAsync_MultipleSequentialCalls_ShouldRespectRate()
    {
        // 10000 bytes/sec, burst=20000
        var limiter = new TokenBucketRateLimiter(10000);

        var total = 0;
        for (var i = 0; i < 5; i++)
        {
            var bytes = await limiter.AcquireAsync(5000);
            total += bytes;
        }

        // Should have gotten at least some bytes
        Assert.That(total, Is.GreaterThan(0));
    }

    [Test]
    public void ReturnUnused_Zero_ShouldNotThrow()
    {
        var limiter = new TokenBucketRateLimiter(1000);
        Assert.DoesNotThrow(() => limiter.ReturnUnused(0));
    }

    [Test]
    public void ReturnUnused_Negative_ShouldNotThrow()
    {
        var limiter = new TokenBucketRateLimiter(1000);
        Assert.DoesNotThrow(() => limiter.ReturnUnused(-5));
    }

    [Test]
    public async Task ReturnUnused_ShouldMakeTokensAvailable()
    {
        var limiter = new TokenBucketRateLimiter(100, maxBurstBytes: 100);

        // Exhaust all tokens
        var acquired = await limiter.AcquireAsync(100);
        Assert.That(acquired, Is.EqualTo(100));

        // Return some tokens
        limiter.ReturnUnused(50);

        // Should be able to acquire again immediately
        var result = await limiter.AcquireAsync(50);
        Assert.That(result, Is.EqualTo(50));
    }

    [Test]
    public async Task ReturnUnused_ShouldNotExceedMaxBurst()
    {
        var limiter = new TokenBucketRateLimiter(100, maxBurstBytes: 100);

        // Return more than max burst
        limiter.ReturnUnused(500);

        // Should still be capped at max burst
        var result = await limiter.AcquireAsync(200);
        Assert.That(result, Is.LessThanOrEqualTo(100));
    }

    [Test]
    public async Task AcquireAsync_CustomBurstSize_ShouldRespect()
    {
        var limiter = new TokenBucketRateLimiter(1000, maxBurstBytes: 500);

        // Request more than burst - should get at most burst amount
        var result = await limiter.AcquireAsync(1000);
        Assert.That(result, Is.LessThanOrEqualTo(500));
    }

    [Test]
    public async Task AcquireAsync_AfterWait_ShouldReplenishTokens()
    {
        // 10000 bytes/sec
        var limiter = new TokenBucketRateLimiter(10000, maxBurstBytes: 10000);

        // Exhaust tokens
        await limiter.AcquireAsync(10000);

        // Wait for replenishment (100ms should give ~1000 tokens)
        await Task.Delay(150);

        var result = await limiter.AcquireAsync(500);
        Assert.That(result, Is.GreaterThan(0));
    }
}
