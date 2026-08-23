using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Core.Application.Common.Ports;
using Ashlar.Core.Application.Execution.Ports;
using Ashlar.Core.Application.NodeCapabilityRuntime.Models;
using Ashlar.Core.Application.NodeCapabilityRuntime.Ports;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Profiles;
using Ashlar.Infrastructure.NodeCapabilityRuntime.Scoring;

namespace Ashlar.Infrastructure.NodeCapabilityRuntime;

internal sealed class SimpleObservable<T> : IObservable<T>
{
    private readonly List<IObserver<T>> _observers = new();
    private readonly object _gate = new();

    /// <summary>Subscribe.</summary>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        if (observer is null) throw new ArgumentNullException(nameof(observer));
        lock (_gate)
        {
            _observers.Add(observer);
        }

        return new Subscription(_observers, _gate, observer);
    }

    /// <summary>Publish.</summary>
    public void Publish(T value)
    {
        List<IObserver<T>> snapshot;
        lock (_gate)
        {
            snapshot = new List<IObserver<T>>(_observers);
        }

        foreach (var observer in snapshot)
        {
            observer.OnNext(value);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly List<IObserver<T>> _observers;
        private readonly object _gate;
        private IObserver<T>? _observer;

        /// <summary>Initializes a new subscription.</summary>
        public Subscription(List<IObserver<T>> observers, object gate, IObserver<T> observer)
        {
            _observers = observers;
            _gate = gate;
            _observer = observer;
        }

        /// <summary>Releases managed resources.</summary>
        public void Dispose()
        {
            if (_observer is null) return;
            lock (_gate)
            {
                _observers.Remove(_observer);
            }

            _observer = null;
        }
    }
}
