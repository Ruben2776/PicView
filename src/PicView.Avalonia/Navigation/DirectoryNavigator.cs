using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Localization;

namespace PicView.Avalonia.Navigation;

public static class DirectoryNavigator
{
    /// <summary>
    ///     Navigates to the next or previous folder and loads the first image in that folder.
    /// </summary>
    /// <param name="next">True to navigate to the next folder, false for the previous folder.</param>
    /// <param name="iterator">The image iterator.</param>
    /// <param name="loadWithoutImageIterator">The action to load an image without the iterator.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async ValueTask NavigateBetweenDirectories(bool next, ImageIterator iterator,
        Func<FileInfo, MainViewModel, List<FileInfo>?, int, ValueTask> loadWithoutImageIterator, MainViewModel vm)
    {
        if (!NavigationManager.CanNavigate(vm))
        {
            return;
        }

        TitleManager.SetLoadingTitle(vm);
        await ImageLoader.CancelAsync().ConfigureAwait(false);

        if (Settings.Sorting.IncludeSubDirectories)
        {
            await NavigateDirectoryWithinSubdirectories(next, iterator, loadWithoutImageIterator, vm)
                .ConfigureAwait(false);
        }
        else
        {
            await NavigateToNextDirectory(next, iterator, loadWithoutImageIterator, vm).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Navigates to the next or previous directory using the file system structure.
    /// </summary>
    private static async ValueTask NavigateToNextDirectory(bool next, ImageIterator iterator,
        Func<FileInfo, MainViewModel, List<FileInfo>?, int, ValueTask> loadWithoutImageIterator, MainViewModel vm)
    {
        var fileList = GetNextFolderFileList(next, iterator, vm);

        if (fileList is null)
        {
            TitleManager.SetTitle(vm);
        }
        else
        {
            vm.PlatformService.StopTaskbarProgress();
            await loadWithoutImageIterator(fileList[0], vm, fileList, 0);
            if (vm.PicViewer.Title.CurrentValue == TranslationManager.Translation.Loading)
            {
                TitleManager.SetTitle(vm);
            }
        }
    }

    /// <summary>
    /// Navigates to the next or previous directory from the list of directories of all loaded images.
    /// </summary>
    private static async ValueTask NavigateDirectoryWithinSubdirectories(bool next, ImageIterator iterator,
        Func<FileInfo, MainViewModel, List<FileInfo>?, int, ValueTask> loadWithoutImageIterator, MainViewModel vm)
    {
        var imagePaths = iterator?.ImagePaths;
        if (imagePaths is null || imagePaths.Count == 0)
        {
            return;
        }

        var directories = imagePaths.Select(path => path.DirectoryName).Distinct().ToList();

        if (directories.Count <= 1)
        {
            await NavigateToNextDirectory(next, iterator, loadWithoutImageIterator, vm).ConfigureAwait(false);
            return;
        }

        var currentDir = imagePaths[iterator.CurrentIndex].DirectoryName;
        var index = directories.IndexOf(currentDir);
        if (index == -1)
        {
            // Fallback if the current directory is not in the list for some reason
            await NavigateToNextDirectory(next, iterator, loadWithoutImageIterator, vm).ConfigureAwait(false);
            return;
        }

        var nextIndex = next
            ? (index + 1) % directories.Count
            : (index - 1 + directories.Count) % directories.Count;

        var nextDir = directories[nextIndex];
        var firstFileInNextDirIndex = imagePaths.FindIndex(path => path.DirectoryName == nextDir);

        if (firstFileInNextDirIndex != -1)
        {
            await ImageLoader.IterateToIndexAsync(firstFileInNextDirIndex, iterator).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Gets the list of files in the next or previous folder.
    /// </summary>
    /// <param name="next">True to get the next folder, false for the previous folder.</param>
    /// <param name="iterator">The image iterator.</param>
    /// <param name="vm">The main view model instance.</param>
    /// <returns>A task representing the asynchronous operation that returns a list of file paths.</returns>
    private static List<FileInfo>? GetNextFolderFileList(bool next, ImageIterator iterator,
        MainViewModel vm)
    {
        var currentFolder = iterator?.ImagePaths[iterator.CurrentIndex].DirectoryName;
        if (string.IsNullOrEmpty(currentFolder))
        {
            return null;
        }

        string? parentFolder;
        var initialDirectory = Settings.StartUp.StartUpDirectory;
        if (!string.IsNullOrWhiteSpace(initialDirectory))
        {
            parentFolder = Path.GetDirectoryName(initialDirectory);
        }
        else
        {
            parentFolder = Path.GetDirectoryName(currentFolder);
        }

        if (string.IsNullOrEmpty(parentFolder))
        {
            return null;
        }

        var parentDirectories = Directory.GetDirectories(parentFolder, "*", SearchOption.AllDirectories);
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (parentDirectories.Length > 1)
        {
            return NextDirectoryInCurrentDirectories(parentDirectories, currentFolder, false, next, vm);
        }

        return null;
    }
    
    private static List<FileInfo>? NextDirectoryInCurrentDirectories(string[] directories, string currentFolder,
        bool isCurrentDirectory, bool next, MainViewModel vm)
    {
        directories.Sort((x, y) => vm.PlatformService.CompareStrings(x, y));
        var dirCount = directories.Length;
        int directoryIndex;
        if (isCurrentDirectory)
        {
            directoryIndex = next ? -1 : 0;
        }
        else
        {
            directoryIndex = Array.IndexOf(directories, currentFolder);
            if (directoryIndex == -1)
            {
                return null;
            }
        }
        
        var indexChange = next ? 1 : -1;

        for (var i = 1; i <= dirCount; i++)
        {
            var nextIndex = (directoryIndex + i * indexChange + dirCount) % dirCount;
            var fileList = vm.PlatformService.GetFiles(new FileInfo(directories[nextIndex]));
            if (fileList is { Count: > 0 })
            {
                return fileList;
            }
        }
        return null;
    }
}
