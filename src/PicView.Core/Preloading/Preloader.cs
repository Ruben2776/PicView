using System.Collections.Concurrent;
using PicView.Core.DebugTools;

namespace PicView.Core.Preloading;

/// <summary>
/// Background worker of <see cref="PicView.Core.Navigation.SharedImageCache"/>.
/// <para>
/// Responsible for calculating predictive indices (look-ahead) and loading them, ahead in the direction of travel
/// first, while at most one preload per owner runs.
/// </para>
/// </summary>
/// <param name="load">Loads a file for an owner and completes when the image is loaded.</param>
internal sealed class Preloader(Func<uint, FileInfo, Task> load)
{
    private readonly ConcurrentDictionary<uint, byte> _running = new();

    /// <summary>
    /// Loads the files around <paramref name="currentIndex"/> in the background.
    /// Does nothing while a preload for the same owner is still running.
    /// </summary>
    public void Preload(uint ownerId, FileInfo[] files, int currentIndex, bool reversed, CancellationToken ct)
    {
        if (!_running.TryAdd(ownerId, 0))
        {
            return;
        }

        // Not passing ct to Task.Run: a task cancelled before it starts would never clear the running flag.
        _ = Task.Run(async () =>
        {
            try
            {
                await PreloadAroundAsync(ownerId, files, currentIndex, reversed, ct).ConfigureAwait(false);
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
            }
        }, CancellationToken.None);
    }

    private async Task PreloadAroundAsync(uint ownerId, FileInfo[] files, int currentIndex, bool reversed,
        CancellationToken ct)
    {
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
            var iterations = forwards ? PreLoaderConfig.PositiveIterations : PreLoaderConfig.NegativeIterations;
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
