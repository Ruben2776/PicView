using PicView.Core.DebugTools;

namespace PicView.Core.Config;

/// <summary>
/// At most one save runs; a request made meanwhile is followed by exactly one more.
/// </summary>
/// <param name="save">Saves the settings.</param>
/// <param name="owner">The name logged when the save throws.</param>
internal sealed class CoalescedSave(Func<ValueTask<bool>> save, string owner)
{
    private int _state; // 0 idle; 1 a save runs; 2 a save runs and a change landed since it began
    private Task _saving = Task.CompletedTask;

    /// <summary>The save in flight, completed when idle.</summary>
    internal Task Saving => _saving;

    /// <summary>
    /// Starts a save, or marks the running one to save once more. Safe from any thread. Never throws.
    /// </summary>
    internal void Soon()
    {
        while (true)
        {
            if (Interlocked.CompareExchange(ref _state, 1, 0) is 0)
            {
                _saving = SaveUntilClean();
                return;
            }

            // A save runs: mark that a change landed (1 -> 2), or it is already marked. If the save ended between
            // the two checks the state is 0 again, so go round and start one
            if (Interlocked.CompareExchange(ref _state, 2, 1) is 1 or 2)
            {
                return;
            }
        }
    }

    private async Task SaveUntilClean()
    {
        while (true)
        {
            try
            {
                await save().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(owner, nameof(SaveUntilClean), e);
            }

            // Clean: a change after this starts its own save. Otherwise a change landed meanwhile, so save once more
            if (Interlocked.CompareExchange(ref _state, 0, 1) is 1)
            {
                return;
            }
            Interlocked.CompareExchange(ref _state, 1, 2);
        }
    }
}
