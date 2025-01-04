using PicView.Avalonia.ViewModels;
using PicView.Core.Config;
using PicView.Core.Navigation;

namespace PicView.Avalonia.Navigation;


// TODO: This file needs to me removed and the FileHistory class needs to use interfaces instead.


public static class FileHistoryNavigation
{
    private static FileHistory? _fileHistory;

    public static void Add(string file)
    {
        _fileHistory ??= new FileHistory();
        _fileHistory.Add(file);
    }

    public static void Remove(string file)
    {
        _fileHistory ??= new FileHistory();
        _fileHistory.Remove(file);
    }

    public static void Rename(string oldPath, string newPath)
    {
        _fileHistory ??= new FileHistory();
        _fileHistory.Rename(oldPath, newPath);
    }

    public static bool Contains(string file)
    {
        _fileHistory ??= new FileHistory();
        return _fileHistory.Contains(file);
    }

    public static string GetLastFile()
    {
        _fileHistory ??= new FileHistory();
        return _fileHistory.GetLastFile() ?? string.Empty;
    }

    public static int GetCount()
    {
        return _fileHistory?.GetCount() ?? 0;
    }

    internal static async Task OpenLastFileAsync(MainViewModel vm)
    {
        _fileHistory ??= new FileHistory();
        
        if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config/recent.txt")) == false)
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ruben2776/PicView/Config/recent.txt")) == false)
            {
                await LoadLastFileFromSettingsOrNotAsync();
                return;
            }
        }
   
        var entry = _fileHistory.GetLastFile();

        if (entry is null)
        {
            await LoadLastFileFromSettingsOrNotAsync();
            return;
        }

        await NavigationHelper.LoadPicFromStringAsync(entry, vm);
        return;
        
        async Task LoadLastFileFromSettingsOrNotAsync()
        {
            if (!string.IsNullOrWhiteSpace(SettingsHelper.Settings.StartUp.LastFile))
            {
                await NavigationHelper.LoadPicFromStringAsync(SettingsHelper.Settings.StartUp.LastFile, vm);
            }
            else
            {
                ErrorHandling.ShowStartUpMenu(vm);
            }
        }
    }

    public static async Task NextAsync(MainViewModel vm)
    {
        if (!NavigationHelper.CanNavigate(vm))
        {
            await OpenLastFileAsync(vm).ConfigureAwait(false);
            return;
        }

        if (vm.ImageIterator is null)
        {
            await OpenLastFileAsync(vm).ConfigureAwait(false);
            return;
        }

        var index = vm.ImageIterator.CurrentIndex;
        await LoadEntryAsync(vm, index, true).ConfigureAwait(false);
    }

    public static async Task PrevAsync(MainViewModel vm)
    {
        if (!NavigationHelper.CanNavigate(vm))
        {
            await OpenLastFileAsync(vm).ConfigureAwait(false);
            return;
        }
        
        if (vm.ImageIterator is null)
        {
            await OpenLastFileAsync(vm).ConfigureAwait(false);
            return;
        }

        var index = vm.ImageIterator.CurrentIndex;
        await LoadEntryAsync(vm, index, false).ConfigureAwait(false);
    }

    public static async Task LoadEntryAsync(MainViewModel vm, int index, bool next)
    {
        var imagePaths = vm.ImageIterator.ImagePaths;

        _fileHistory ??= new FileHistory();
        string? nextEntry;
        if (next)
        {
            nextEntry = await Task.FromResult(_fileHistory.GetNextEntry(SettingsHelper.Settings.UIProperties.Looping, index, imagePaths)).ConfigureAwait(false);
        }
        else
        {
            nextEntry = await Task.FromResult(_fileHistory.GetPreviousEntry(SettingsHelper.Settings.UIProperties.Looping, index, imagePaths)).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(nextEntry))
        {
            return;
        }

        if (imagePaths.Contains(nextEntry))
        {
            if (nextEntry == imagePaths[index])
            {
                return;
            }

            await NavigationHelper.Navigate(imagePaths.IndexOf(nextEntry), vm).ConfigureAwait(false);
            return;
        }
        await NavigationHelper.LoadPicFromStringAsync(nextEntry, vm);
    }
    
    public static void WriteToFile()
    {
        _fileHistory?.WriteToFile();
    }

    public static string GetFileLocation(int i)
    {
        _fileHistory ??= new FileHistory();
        return _fileHistory.GetEntryAt(i) ?? string.Empty;
    }
}
