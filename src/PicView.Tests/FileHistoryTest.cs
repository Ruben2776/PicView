using PicView.Core.FileHandling;
using PicView.Core.Navigation;

namespace PicView.Tests;

public class FileHistoryTest
{
    [Fact]
    public void TestFileHistory()
    {
        var list = new List<string>();
        var history = new FileHistory();
        Assert.NotNull(history);

        // Check adding
        for (var i = 0; i <= FileHistory.MaxCount; i++)
        {
            AddRandomFiles(history, list);
        }
        Assert.Equal(FileHistory.MaxCount, history.GetCount());
        AddRandomFiles(history, list);
        Assert.Equal(FileHistory.MaxCount, history.GetCount());

        // Check removing
        history.Remove(history.GetLastFile());
        Assert.Equal(FileHistory.MaxCount - 1, history.GetCount());

        // Check renaming
        var lastFile = history.GetLastFile();
        var newFile = Path.GetFileNameWithoutExtension(lastFile);
        newFile = Path.GetRandomFileName();
        history.Rename(lastFile, newFile);
        Assert.Equal(newFile, history.GetLastFile());

        history.Remove(newFile);
        Assert.False(history.Contains(newFile));

        // Check getting iterations
        var entry = history.GetEntryAt(1);
        Assert.NotNull(entry);

        var nextEntry = history.GetNextEntry(looping: true, 2, list);
        Assert.NotNull(nextEntry);
        Assert.True(File.Exists(nextEntry));

        var prevEntry = history.GetPreviousEntry(looping: true, 2, list);
        Assert.NotNull(prevEntry);
        Assert.True(File.Exists(prevEntry));

        foreach (var t in list)
        {
            FileDeletionHelper.DeleteFileWithErrorMsg(t, false);
            Assert.False(File.Exists(t));
            history.Remove(t);
        }
        Assert.Equal(0, history.GetCount());
    }

    private static void AddRandomFiles(FileHistory history, List<string> list)
    {
        var imageFileExtensionArray = new[] { ".jpg", ".png", ".bmp", ".gif", ".tiff", ".webp" };
        var path = Path.GetTempPath() + Path.GetRandomFileName();
        var directory = ArchiveHelper.CreateTempDirectory(path);
        Assert.True(directory);
        var randomExtension = imageFileExtensionArray[new Random().Next(0, imageFileExtensionArray.Length)];
        var randomFileNameWithExtension = path + randomExtension;
        var fullPath = Path.Combine(ArchiveHelper.TempFilePath, randomFileNameWithExtension);
        using var fs = File.Create(fullPath);
        Assert.True(File.Exists(fullPath));
        history.Add(randomFileNameWithExtension);
        list.Add(randomFileNameWithExtension);
    }
}