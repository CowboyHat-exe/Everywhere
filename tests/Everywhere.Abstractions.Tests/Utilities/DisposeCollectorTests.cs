using Everywhere.Utilities;

namespace Everywhere.Abstractions.Tests.Utilities;

[TestFixture]
public class DisposeCollectorTests
{
    [Test]
    public void Add_ShouldTrackDisposable()
    {
        var collector = new DisposeCollector<IDisposable>();
        var disposable = new TestDisposable();

        collector.Add(disposable);

        Assert.That(collector.Count, Is.EqualTo(1));
    }

    [Test]
    public void Add_Factory_ShouldCreateAndTrackDisposable()
    {
        var collector = new DisposeCollector<IDisposable>();
        var disposable = new TestDisposable();

        var result = collector.Add(() => disposable);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(result, Is.SameAs(disposable));
        Assert.That(collector.Count, Is.EqualTo(1));
    }

    [Test]
    public void Dispose_ShouldDisposeAllItems()
    {
        var collector = new DisposeCollector<IDisposable>();
        var d1 = new TestDisposable();
        var d2 = new TestDisposable();
        collector.Add(d1);
        collector.Add(d2);

        collector.Dispose();

        using var _ = Assert.EnterMultipleScope();
        Assert.That(d1.IsDisposed, Is.True);
        Assert.That(d2.IsDisposed, Is.True);
        Assert.That(collector.IsDisposed, Is.True);
    }

    [Test]
    public void Dispose_WhenAlreadyDisposed_ShouldNotThrow()
    {
        var collector = new DisposeCollector<IDisposable>();
        collector.Dispose();

        Assert.DoesNotThrow(() => collector.Dispose());
    }

    [Test]
    public void Clear_ShouldDisposeAllAndAllowReuse()
    {
        var collector = new DisposeCollector<IDisposable>();
        var d1 = new TestDisposable();
        collector.Add(d1);

        collector.Clear();

        using var _ = Assert.EnterMultipleScope();
        Assert.That(d1.IsDisposed, Is.True);
        Assert.That(collector.Count, Is.EqualTo(0));
        Assert.That(collector.IsDisposed, Is.False);

        // Can still add after Clear
        var d2 = new TestDisposable();
        collector.Add(d2);
        Assert.That(collector.Count, Is.EqualTo(1));
    }

    [Test]
    public void Add_WhenDisposed_ShouldThrow()
    {
        var collector = new DisposeCollector<IDisposable>();
        collector.Dispose();

        Assert.Throws<ObjectDisposedException>(() => collector.Add(new TestDisposable()));
    }

    [Test]
    public void RemoveAndDispose_ShouldDisposeAndRemoveItem()
    {
        var collector = new DisposeCollector<IDisposable>();
        IDisposable? d1 = new TestDisposable();
        collector.Add(d1);

        collector.RemoveAndDispose(ref d1);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(d1, Is.Null);
        Assert.That(collector.Count, Is.EqualTo(0));
    }

    [Test]
    public void RemoveAndDispose_WhenNull_ShouldNotThrow()
    {
        var collector = new DisposeCollector<IDisposable>();
        IDisposable? d1 = null;

        Assert.DoesNotThrow(() => collector.RemoveAndDispose(ref d1));
    }

    [Test]
    public void Indexer_ShouldReturnItemAtPosition()
    {
        var collector = new DisposeCollector<IDisposable>();
        var d1 = new TestDisposable();
        var d2 = new TestDisposable();
        collector.Add(d1);
        collector.Add(d2);

        Assert.That(collector[0], Is.SameAs(d1));
        Assert.That(collector[1], Is.SameAs(d2));
    }

    [Test]
    public void Enumeration_ShouldEnumerateAllItems()
    {
        var collector = new DisposeCollector<IDisposable>();
        var d1 = new TestDisposable();
        var d2 = new TestDisposable();
        collector.Add(d1);
        collector.Add(d2);

        var items = collector.ToList();

        Assert.That(items, Has.Count.EqualTo(2));
        Assert.That(items, Does.Contain(d1));
        Assert.That(items, Does.Contain(d2));
    }

    [Test]
    public void NonGenericCollector_AddAction_ShouldInvokeOnDispose()
    {
        var collector = new DisposeCollector();
        var wasInvoked = false;

        collector.Add(() => wasInvoked = true);
        collector.Dispose();

        Assert.That(wasInvoked, Is.True);
    }

    [Test]
    public void NonGenericCollector_AddGenericDisposable_ShouldTrackAndDispose()
    {
        var collector = new DisposeCollector();
        var d1 = new TestDisposable();

        var result = collector.Add(d1);

        Assert.That(result, Is.SameAs(d1));

        collector.Dispose();
        Assert.That(d1.IsDisposed, Is.True);
    }

    [Test]
    public void NonGenericCollector_Replace_ShouldDisposeOldAndTrackNew()
    {
        var collector = new DisposeCollector();
        TestDisposable? old = new TestDisposable();
        collector.Add(old);
        var newDisposable = new TestDisposable();

        collector.Replace(ref old, newDisposable);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(old, Is.SameAs(newDisposable));
        Assert.That(collector.Count, Is.EqualTo(1));
    }

    [Test]
    public void NonGenericCollector_Replace_WithNull_ShouldDisposeOld()
    {
        var collector = new DisposeCollector();
        TestDisposable? old = new TestDisposable();
        var oldRef = old;
        collector.Add(old);

        collector.Replace(ref old, null);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(old, Is.Null);
        Assert.That(oldRef.IsDisposed, Is.True);
        Assert.That(collector.Count, Is.EqualTo(0));
    }

    [Test]
    public void DisposeToDefault_ShouldDisposeAndSetToDefault()
    {
        IDisposable? d = new TestDisposable();
        var reference = (TestDisposable)d;

        DisposeCollector<IDisposable>.DisposeToDefault(ref d);

        using var _ = Assert.EnterMultipleScope();
        Assert.That(d, Is.Null);
        Assert.That(reference.IsDisposed, Is.True);
    }

    [Test]
    public void DisposeToDefault_WhenNull_ShouldNotThrow()
    {
        IDisposable? d = null;
        Assert.DoesNotThrow(() => DisposeCollector<IDisposable>.DisposeToDefault(ref d));
    }

    private class TestDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
