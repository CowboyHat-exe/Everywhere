using Everywhere.Utilities;

namespace Everywhere.Abstractions.Tests.Utilities;

[TestFixture]
public class ReusableCancellationTokenSourceTests
{
    [Test]
    public void Token_ShouldReturnValidToken()
    {
        using var rcts = new ReusableCancellationTokenSource();

        var token = rcts.Token;

        Assert.That(token.CanBeCanceled, Is.True);
        Assert.That(token.IsCancellationRequested, Is.False);
    }

    [Test]
    public void Cancel_ShouldCancelCurrentToken()
    {
        using var rcts = new ReusableCancellationTokenSource();
        var token = rcts.Token;

        rcts.Cancel();

        Assert.That(token.IsCancellationRequested, Is.True);
    }

    [Test]
    public void Token_AfterCancel_ShouldReturnNewUncancelledToken()
    {
        using var rcts = new ReusableCancellationTokenSource();
        var firstToken = rcts.Token;

        rcts.Cancel();

        var secondToken = rcts.Token;

        using var _ = Assert.EnterMultipleScope();
        Assert.That(firstToken.IsCancellationRequested, Is.True);
        Assert.That(secondToken.IsCancellationRequested, Is.False);
        Assert.That(firstToken, Is.Not.EqualTo(secondToken));
    }

    [Test]
    public void Cancel_WhenNeverAccessed_ShouldNotThrow()
    {
        using var rcts = new ReusableCancellationTokenSource();

        Assert.DoesNotThrow(() => rcts.Cancel());
    }

    [Test]
    public void Cancel_MultipleTimes_ShouldNotThrow()
    {
        using var rcts = new ReusableCancellationTokenSource();
        _ = rcts.Token;

        rcts.Cancel();
        Assert.DoesNotThrow(() => rcts.Cancel());
    }

    [Test]
    public void Token_MultipleCalls_BeforeCancel_ShouldReturnSameToken()
    {
        using var rcts = new ReusableCancellationTokenSource();

        var token1 = rcts.Token;
        var token2 = rcts.Token;

        Assert.That(token1, Is.EqualTo(token2));
    }

    [Test]
    public void Dispose_ShouldNotThrow()
    {
        var rcts = new ReusableCancellationTokenSource();
        _ = rcts.Token;

        Assert.DoesNotThrow(() => rcts.Dispose());
    }

    [Test]
    public void ConcurrentAccess_ShouldBeThreadSafe()
    {
        using var rcts = new ReusableCancellationTokenSource();
        var exceptions = new List<Exception>();
        var threads = new List<Thread>();

        for (var i = 0; i < 10; i++)
        {
            threads.Add(new Thread(() =>
            {
                try
                {
                    for (var j = 0; j < 100; j++)
                    {
                        var token = rcts.Token;
                        Assert.That(token.CanBeCanceled, Is.True);
                        if (j % 10 == 0) rcts.Cancel();
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            }));
        }

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        if (exceptions.Count != 0)
        {
            Assert.Fail($"Thread safety test failed with {exceptions.Count} exceptions. First: {exceptions.First()}");
        }
    }
}
