using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace Everywhere.Interop;

/// <summary>
/// Base class providing shared subscriber lifecycle management for text selection monitoring.
/// Eliminates duplicated Subscribe/Start/Stop patterns across Windows and Mac implementations.
/// </summary>
public abstract class TextSelectionMonitor : IObservable<TextSelectionData>
{
    private readonly Subject<TextSelectionData> _textSelectionSubject = new();
    private IDisposable? _hookSubscription;
    private int _subscriberCount;

    public IDisposable Subscribe(IObserver<TextSelectionData> observer)
    {
        var subscription = _textSelectionSubject.Subscribe(observer);

        // Start monitoring when the first subscriber arrives
        if (Interlocked.Increment(ref _subscriberCount) == 1)
        {
            StartMonitoring();
        }

        return Disposable.Create(() =>
        {
            subscription.Dispose();
            // Stop monitoring when the last subscriber leaves
            if (Interlocked.Decrement(ref _subscriberCount) == 0)
            {
                StopMonitoring();
            }
        });
    }

    /// <summary>
    /// Publishes a text selection event to all subscribers.
    /// </summary>
    protected void PublishSelection(TextSelectionData data)
    {
        _textSelectionSubject.OnNext(data);
    }

    /// <summary>
    /// Creates the platform-specific detector. Called when the first subscriber arrives.
    /// </summary>
    protected abstract IDisposable CreateDetector(Action<TextSelectionData> onSelectionDetected);

    private void StartMonitoring()
    {
        if (_hookSubscription != null) return;
        _hookSubscription = CreateDetector(PublishSelection);
    }

    private void StopMonitoring()
    {
        _hookSubscription?.Dispose();
        _hookSubscription = null;
    }
}
