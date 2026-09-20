using Claims.Infrastructure.Auditing;

namespace Claims.Tests.Unit.Infrastructure;

/// <summary>
/// An <see cref="IAuditStore"/> that lets a test observe and steer the background writer.
/// Every observation is a <see cref="TaskCompletionSource"/> the test can await, so no test has to
/// sleep, poll, or depend on which of two operations happens to win.
/// </summary>
public sealed class FakeAuditStore : IAuditStore
{
    private readonly object _sync = new();
    private readonly List<AuditEntry> _written = [];
    private readonly List<(Func<bool> Reached, TaskCompletionSource Signal)> _waiters = [];

    private TaskCompletionSource? _gate;
    private int _started;
    private int _completed;
    private int _failuresRemaining;

    /// <summary>Entries the store has accepted, in the order it accepted them.</summary>
    public IReadOnlyList<AuditEntry> Written
    {
        get
        {
            lock (_sync)
            {
                return _written.ToList();
            }
        }
    }

    /// <summary>Number of write attempts that have finished, whether they succeeded or threw.</summary>
    public int CompletedWrites
    {
        get
        {
            lock (_sync)
            {
                return _completed;
            }
        }
    }

    /// <summary>Completes once the writer has entered its <paramref name="count"/>th write.</summary>
    public Task WriteStarted(int count) => When(() => _started >= count);

    /// <summary>Completes once <paramref name="count"/> write attempts have finished.</summary>
    public Task WriteCompleted(int count) => When(() => _completed >= count);

    /// <summary>Completes once the store holds at least <paramref name="count"/> entries.</summary>
    public Task WrittenCountReaches(int count) => When(() => _written.Count >= count);

    /// <summary>Parks the next write inside the store until <see cref="ReleaseBlockedWrite"/> is called.</summary>
    public void BlockNextWrite()
    {
        lock (_sync)
        {
            _gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    /// <summary>Lets a write parked by <see cref="BlockNextWrite"/> continue.</summary>
    public void ReleaseBlockedWrite()
    {
        TaskCompletionSource? gate;

        lock (_sync)
        {
            gate = _gate;
            _gate = null;
        }

        gate?.TrySetResult();
    }

    /// <summary>Makes the next <paramref name="count"/> writes throw, as a failing database would.</summary>
    public void FailNextWrites(int count)
    {
        lock (_sync)
        {
            _failuresRemaining = count;
        }
    }

    /// <inheritdoc />
    public async Task WriteAsync(IReadOnlyList<AuditEntry> batch, CancellationToken cancellationToken = default)
    {
        Task? gate;

        lock (_sync)
        {
            _started++;
            gate = _gate?.Task;
            SignalReachedWaiters();
        }

        if (gate is not null)
        {
            await gate;
        }

        bool shouldFail;

        lock (_sync)
        {
            shouldFail = _failuresRemaining > 0;

            if (shouldFail)
            {
                _failuresRemaining--;
            }
            else
            {
                _written.AddRange(batch);
            }

            _completed++;
            SignalReachedWaiters();
        }

        if (shouldFail)
        {
            throw new InvalidOperationException("audit database is unreachable");
        }
    }

    private Task When(Func<bool> reached)
    {
        lock (_sync)
        {
            if (reached())
            {
                return Task.CompletedTask;
            }

            var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((reached, signal));
            return signal.Task;
        }
    }

    private void SignalReachedWaiters()
    {
        for (var i = _waiters.Count - 1; i >= 0; i--)
        {
            if (!_waiters[i].Reached())
            {
                continue;
            }

            // RunContinuationsAsynchronously keeps awaiting tests off this thread, and out of the lock.
            _waiters[i].Signal.TrySetResult();
            _waiters.RemoveAt(i);
        }
    }
}
