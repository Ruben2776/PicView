using PicView.Core.IPlatform;
using PicView.Tests.Navigation;

namespace PicView.Tests.Saving;

/// <summary>
/// A platform whose Recycle Bin records each file sent to it, with the file's bytes at that moment, then deletes it.
/// </summary>
public sealed class RecordingPlatform : TestTabServices.OrdinalPlatform, IPlatformSpecificService
{
    /// <summary>Each path deleted, with the bytes it held when the delete was asked.</summary>
    public List<(string Path, byte[] Bytes)> Deleted { get; } = [];

    /// <summary>The paths of <see cref="Deleted"/> that went through <see cref="RecycleQuietlyAsync"/>.</summary>
    public List<string> RecycledQuietly { get; } = [];

    /// <summary>Whether a file can go to the Recycle Bin.</summary>
    public bool HasRecycleBin { get; set; } = true;

    /// <summary>Whether the file at a path can go to the Recycle Bin, in place of <see cref="HasRecycleBin"/>.</summary>
    public Func<string, bool>? HasRecycleBinAt { get; set; }

    /// <summary>
    /// Whether a recycle goes through. When false the file stays and false is returned, as when the user answers No to
    /// the shell's "delete permanently?" for a file too large for the Recycle Bin.
    /// </summary>
    public bool RecycleSucceeds { get; set; } = true;

    public new Task<bool> DeleteFile(string path, bool recycle)
    {
        if (recycle && !RecycleSucceeds)
        {
            return Task.FromResult(false);
        }

        lock (Deleted)
        {
            Deleted.Add((path, File.ReadAllBytes(path)));
        }

        File.Delete(path);
        return Task.FromResult(true);
    }

    public async Task<bool> RecycleQuietlyAsync(string path)
    {
        var recycled = await DeleteFile(path, recycle: true).ConfigureAwait(false);
        if (recycled)
        {
            lock (Deleted)
            {
                RecycledQuietly.Add(path);
            }
        }

        return recycled;
    }

    public new bool CanMoveToRecycleBin(string path) => HasRecycleBinAt?.Invoke(path) ?? HasRecycleBin;
}
