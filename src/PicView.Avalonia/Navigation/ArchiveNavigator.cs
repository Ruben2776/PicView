using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.ArchiveHandling;
using PicView.Core.FileHandling;

namespace PicView.Avalonia.Navigation;

public static class ArchiveNavigator
{
    public static async ValueTask NavigateBetweenArchives(bool next, MainViewModel vm)
    {
        if (!NavigationManager.CanNavigate(vm))
        {
            return;
        }

        TitleManager.SetLoadingTitle(vm);
        await ImageLoader.CancelAsync().ConfigureAwait(false);

        // Determine context
        string? searchDirectory = null;
        string? currentArchive = null;

        if (ArchiveExtraction.IsArchived && ArchiveExtraction.LastOpenedArchive != null)
        {
            currentArchive = ArchiveExtraction.LastOpenedArchive;
            searchDirectory = Path.GetDirectoryName(currentArchive);
        }
        else if (NavigationManager.ImageIterator?.InitialFileInfo != null)
        {
            // If not in an archive, look for archives in the current file's directory
            searchDirectory = NavigationManager.ImageIterator.InitialFileInfo.DirectoryName;
            currentArchive = null; // No current archive, so we will pick first/last
        }

        if (string.IsNullOrEmpty(searchDirectory) || !Directory.Exists(searchDirectory))
        {
            TitleManager.SetTitle(vm);
            return;
        }

        // Find archives
        var archives = GetArchivesInDirectory(searchDirectory, vm);

        if (archives.Count == 0)
        {
            TitleManager.SetTitle(vm);
            return;
        }

        // Determine target index
        int targetIndex;
        if (currentArchive != null)
        {
            var currentIndex = archives.FindIndex(x => x.Equals(currentArchive, StringComparison.OrdinalIgnoreCase));
            if (currentIndex == -1)
            {
                // Current archive not found (maybe moved/deleted?), fallback to first
                targetIndex = 0;
            }
            else
            {
                var dirCount = archives.Count;
                var indexChange = next ? 1 : -1;
                targetIndex = (currentIndex + indexChange + dirCount) % dirCount;
            }
        }
        else
        {
            // Entering archive mode from normal mode
            // Option B: Look for archives in the current folder and open the first one (or last if navigating backwards?)
            // Usually "Next" -> First archive. "Prev" -> Last archive?
            // Or just always First?
            // "Next Archive" -> First archive in list.
            targetIndex = next ? 0 : archives.Count - 1;
        }

        var targetArchive = archives[targetIndex];

        // Load the archive
        await NavigationManager.LoadPicFromArchiveAsync(targetArchive, vm).ConfigureAwait(false);
    }

    private static List<string> GetArchivesInDirectory(string directory, MainViewModel vm)
    {
        try
        {
            var files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly);
            var archives = files.Where(file => file.IsArchive()).ToList();

            if (vm.PlatformService != null)
            {
                archives.Sort((x, y) => vm.PlatformService.CompareStrings(x, y));
            }
            else
            {
                archives.Sort();
            }

            return archives;
        }
        catch (Exception)
        {
            return [];
        }
    }
}