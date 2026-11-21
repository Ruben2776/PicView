namespace PicView.Core.Thumbnailing;

/// <summary>
/// A fixed number of decode slots, taken in the order asked for. Unlike <see cref="SemaphoreSlim"/>, a wait that is
/// canceled ends with false rather than an <see cref="OperationCanceledException"/>: gallery tiles cancel their
/// waits by the thousand while scrolling, and an exception for each is costly.
/// </summary>
internal sealed class DecodeSlots(int count)
{
    private readonly Lock _lock = new();
    private readonly LinkedList<Waiter> _waiters = new();
    private int _free = count;

    /// <summary>
    /// Takes a slot, waiting for one to be released if none is free.
    /// </summary>
    /// <returns>True once a slot is taken, which <see cref="Release"/> then gives back; false when
    /// <paramref name="ct"/> was canceled first, and no slot is taken.</returns>
    public async ValueTask<bool> TakeAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return false;
        }

        Waiter waiter;
        lock (_lock)
        {
            if (_free > 0)
            {
                _free--;
                return true;
            }

            waiter = new Waiter(this);
            waiter.Node = _waiters.AddLast(waiter);
        }

        // Registered outside the lock: an already canceled token runs the callback at once, and it takes the lock
        // ReSharper disable once UseAwaitUsing
#pragma warning disable MA0042
        using var registration = ct.UnsafeRegister(static state => ((Waiter)state!).Cancel(), waiter);
#pragma warning restore MA0042
        return await waiter.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Gives back a slot <see cref="TakeAsync"/> took, to the longest waiting request, if any.
    /// </summary>
    public void Release()
    {
        Waiter? next = null;
        lock (_lock)
        {
            if (_waiters.First is { } first)
            {
                _waiters.RemoveFirst();
                first.Value.Node = null;
                next = first.Value;
            }
            else
            {
                _free++;
            }
        }

        // Continuations run asynchronously, so the waiter's decode never runs on the releasing thread
        next?.Complete(true);
    }

    private sealed class Waiter(DecodeSlots slots)
        : TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
    {
        // Set and cleared under the lock: whoever clears it, Release or Cancel, decides the result
        public LinkedListNode<Waiter>? Node;

        public void Complete(bool result) => TrySetResult(result);

        public void Cancel()
        {
            lock (slots._lock)
            {
                if (Node is null)
                {
                    // Already handed a slot
                    return;
                }

                slots._waiters.Remove(Node);
                Node = null;
            }

            Complete(false);
        }
    }
}
