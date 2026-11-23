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

    /// <summary>Whether a file can go to the Recycle Bin.</summary>
    public bool HasRecycleBin { get; set; } = true;

    public new Task<bool> DeleteFile(string path, bool recycle)
    {
        lock (Deleted)
        {
            Deleted.Add((path, File.ReadAllBytes(path)));
        }

        File.Delete(path);
        return Task.FromResult(true);
    }

    public new bool CanMoveToRecycleBin(string path) => HasRecycleBin;
}
