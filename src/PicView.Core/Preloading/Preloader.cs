using System.Collections.Concurrent;
using PicView.Core.DebugTools;

namespace PicView.Core.Preloading;

/// <summary>
/// Background worker of <see cref="PicView.Core.Navigation.SharedImageCache"/>.
/// <para>
/// Responsible for calculating predictive indices (look-ahead) and loading them, in the direction of the last step
/// first and deepest in the direction of travel, while at most one preload per owner runs. A preload asked for while one runs follows it, with the list and
/// position as they were asked for last: an archive's pages are listed as they are extracted, and the page after the
/// shown one should be loaded by the time Next asks for it.
/// </para>
/// </summary>
internal sealed class Preloader(Func<uint, FileInfo, Task> load)
{
    private readonly ConcurrentDictionary<uint, byte> _running = new();

    // The preload each owner asked for last and has not run yet
    private readonly ConcurrentDictionary<uint, PreloadRequest> _next = new();

    /// <summary>
    /// Loads the files around <paramref name="currentIndex"/> in the background, after the preload running for the
    /// owner, if any.
    /// </summary>
    public void Preload(uint ownerId, PreloadRequest request)
    {
        _next[ownerId] = request;
        RunNext(ownerId);
    }

    /// <summary>
    /// Runs the preload the owner asked for last, unless one is running: that one runs it when it ends.
    /// </summary>
    private void RunNext(uint ownerId)
    {
        if (!_running.TryAdd(ownerId, 0))
        {
            return;
        }

        if (!_next.TryRemove(ownerId, out var request))
        {
            _running.TryRemove(ownerId, out _);
            // A request made between the two reads saw the preload as running, so it falls to this one
            if (_next.ContainsKey(ownerId))
            {
                RunNext(ownerId);
            }

            return;
        }

        // Not passing ct to Task.Run: a task cancelled before it starts would never clear the running flag.
        _ = Task.Run(async () =>
        {
            try
            {
                await PreloadAroundAsync(ownerId, request).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Navigated elsewhere
            }
            catch (Exception ex)
            {
                DebugHelper.LogDebug(nameof(Preloader), nameof(Preload), ex);
            }
            finally
            {
                _running.TryRemove(ownerId, out _);
                RunNext(ownerId);
            }
        }, CancellationToken.None);
    }

    private async Task PreloadAroundAsync(uint ownerId, PreloadRequest request)
    {
        var (files, currentIndex, reversed, travelsReversed, ct) = request;
        var count = files.Length;
        if (count is 0)
        {
            return;
        }

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = PreLoaderConfig.MaxParallelism,
            CancellationToken = ct
        };

        if (reversed)
        {
            await LoopAsync(forwards: false).ConfigureAwait(false);
            await LoopAsync(forwards: true).ConfigureAwait(false);
        }
        else
        {
            await LoopAsync(forwards: true).ConfigureAwait(false);
            await LoopAsync(forwards: false).ConfigureAwait(false);
        }

        return;

        async Task LoopAsync(bool forwards)
        {
            // The deep side is the one ahead in the direction of travel, the shallow one what was left behind
            var isAhead = forwards != travelsReversed;
            var iterations = isAhead ? PreLoaderConfig.PositiveIterations : PreLoaderConfig.NegativeIterations;
            await Parallel.ForAsync(0, iterations, options, async (i, _) =>
            {
                var index = forwards
                    ? (currentIndex + 1 + i) % count
                    : ((currentIndex - 1 - i) % count + count) % count;
                await load(ownerId, files[index]).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// What to preload for an owner: the files around <paramref name="CurrentIndex"/> in <paramref name="Files"/>.
/// </summary>
/// <param name="Reversed">Whether the last step went backwards: that side is loaded first.</param>
/// <param name="TravelsReversed">Whether the owner travels backwards: that side is loaded deepest.</param>
internal readonly record struct PreloadRequest(FileInfo[] Files, int CurrentIndex, bool Reversed,
    bool TravelsReversed, CancellationToken Ct);
