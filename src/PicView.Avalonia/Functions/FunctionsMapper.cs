using System.Collections.Frozen;
using Avalonia.Threading;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.ImageTransformations;
using PicView.Avalonia.Input;
using PicView.Avalonia.SettingsManagement;
using PicView.Avalonia.UI;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Printing;
using PicView.Avalonia.Wallpaper;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.DebugTools;
using PicView.Core.Favorites;
using PicView.Core.FileHistory;
using PicView.Core.FileSorting;
using PicView.Core.IPlatform;
using PicView.Core.Keybindings;
using PicView.Core.Navigation;
using PicView.Avalonia.StartUp;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Functions;

/// <summary>
/// A pure functions map.
/// </summary>
/// <remarks>It is intentionally restricted to a pure dispatch table that performs only null checks
/// and delegates all orchestration and UI workflow logic to dedicated helper modules.</remarks>
public class FunctionsMapper : IActionRunner
{
    private readonly MainWindowViewModel _vm;
    private readonly MainWindow _mainWindow;

    /// <summary>
    /// The function of every Action, built once so that running a Keybind never allocates a delegate.
    /// </summary>
    private readonly FrozenDictionary<string, Func<ValueTask>> _functions;

    public FunctionsMapper(MainWindowViewModel vm, MainWindow mainWindow)
    {
        _vm = vm;
        _mainWindow = mainWindow;

        var functions = new Dictionary<string, Func<ValueTask>>(KeybindCategoryDefinition.ActionNames.Count,
            StringComparer.Ordinal);
        foreach (var name in KeybindCategoryDefinition.ActionNames)
        {
            if (CreateFunction(name) is { } function)
            {
                functions[name] = function;
            }
        }

        _functions = functions.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public ValueTask Run(string action)
    {
        if (_functions.TryGetValue(action, out var function))
        {
            return function();
        }

        DebugHelper.LogDebug(nameof(FunctionsMapper), nameof(Run), $"Unknown Action \"{action}\"");
        return ValueTask.CompletedTask;
    }

    internal Func<ValueTask>? GetFunctionByName(string functionName) =>
        _functions.GetValueOrDefault(functionName);

    private Func<ValueTask>? CreateFunction(string functionName)
    {
        // Remember to have exact matching names, or it will be null
        return functionName switch
        {
            // Navigation values
            Actions.Next => Next,
            Actions.Prev => Prev,
            
            Actions.NextFolder => NextFolder,
            Actions.PrevFolder => PrevFolder,
            
            Actions.Up => Up,
            Actions.Down => Down,
            
            Actions.Last => Last,
            Actions.First => First,
            
            Actions.Next10 => Next10,
            Actions.Prev10 => Prev10,
            
            Actions.Next100 => Next100,
            Actions.Prev100 => Prev100,

            Actions.Search => Search,

            Actions.NextArchive => NextArchive,
            Actions.PrevArchive => PrevArchive,
            
            Actions.FileHistoryNext => FileHistoryNext,
            Actions.FileHistoryPrev => FileHistoryPrev,
            
            // Rotate
            Actions.RotateLeft => RotateLeft,
            Actions.RotateRight => RotateRight,
            Actions.Rotate0 => Rotate0,
            Actions.Rotate90 => Rotate90,
            Actions.Rotate180 => Rotate180,
            Actions.Rotate270 => Rotate270,

            // Scroll
            Actions.ScrollUp => ScrollUp,
            Actions.ScrollDown => ScrollDown,
            Actions.ScrollToTop => ScrollToTop,
            Actions.ScrollToBottom => ScrollToBottom,

            // Zoom
            Actions.ZoomIn => ZoomIn,
            Actions.ZoomOut => ZoomOut,
            Actions.ResetZoom => ResetZoom,
            Actions.ResetZoomAndRotations => ResetZoomAndRotations,

            // Toggles
            Actions.ToggleScroll => ToggleScroll,
            Actions.ToggleLooping => ToggleLooping,
            Actions.ToggleGallery => ToggleGallery,

            // Scale Window
            Actions.AutoFitWindow => AutoFitWindow,
            Actions.NormalWindow => NormalWindow,

            // Window functions
            Actions.ToggleFullscreen => ToggleFullscreen,
            Actions.SetTopMost => SetTopMost,
            Actions.Close => Close,
            Actions.ToggleInterface => ToggleInterface,
            Actions.NewWindow => NewWindow,
            Actions.Center => Center,
            Actions.Maximize => Maximize,
            Actions.Restore => Restore,

            // Windows
            Actions.AboutWindow => AboutWindow,
            Actions.CheckForUpdates => CheckForUpdates,
            Actions.EffectsWindow => EffectsWindow,
            Actions.ImageInfoWindow => ImageInfoWindow,
            Actions.ResizeWindow => ResizeWindow,
            Actions.SettingsWindow => SettingsWindow,
            Actions.KeybindingsWindow => KeybindingsWindow,
            Actions.BatchResizeWindow => BatchResizeWindow,
            Actions.FileAssociationsWindow => FileAssociationsWindow,
            Actions.ConvertWindow => ConvertWindow,

            // Open functions
            Actions.Open => Open,
            Actions.OpenWith => OpenWith,
            Actions.OpenInExplorer => OpenInExplorer,
            Actions.Save => Save,
            Actions.SaveAs => SaveAs,
            Actions.SaveAsPDF => SaveAsPDF,
            Actions.Print => Print,
            Actions.Reload => Reload,

            // Copy functions
            Actions.CopyFile => CopyFile,
            Actions.CopyFilePath => CopyFilePath,
            Actions.CopyImage => CopyImage,
            Actions.CopyBase64 => CopyBase64,
            Actions.DuplicateFile => DuplicateFile,
            Actions.CutFile => CutFile,
            Actions.Paste => Paste,

            // File functions
            Actions.DeleteFile => DeleteFile,
            Actions.DeleteFilePermanently => DeleteFilePermanently,
            Actions.Rename => Rename,
            Actions.ShowFileProperties => ShowFileProperties,
            Actions.ShowSettingsFile => ShowSettingsFile,
            Actions.ShowKeybindingsFile => ShowKeybindingsFile,
            Actions.ShowRecentHistoryFile => ShowRecentHistoryFile,
            Actions.ShowFavoritesFile => ShowFavoritesFile,
            
            // Sorting functions
            Actions.SortFilesByName => SortFilesByName,
            Actions.SortFilesByCreationTime => SortFilesByCreationTime,
            Actions.SortFilesByLastAccessTime => SortFilesByLastAccessTime,
            Actions.SortFilesByLastWriteTime => SortFilesByLastWriteTime,
            Actions.SortFilesBySize => SortFilesBySize,
            Actions.SortFilesByExtension => SortFilesByExtension,
            Actions.SortFilesRandomly => SortFilesRandomly,
            
            Actions.SortFilesAscending => SortFilesAscending,
            Actions.SortFilesDescending => SortFilesDescending,
            
            // Image functions
            Actions.ResizeImage => ResizeImage,
            Actions.Crop => Crop,
            Actions.Flip => Flip,
            Actions.OptimizeImage => OptimizeImage,
            Actions.ZoomToFit => ZoomToFit,

            // Set stars
            Actions.Set0Star => Set0Star,
            Actions.Set1Star => Set1Star,
            Actions.Set2Star => Set2Star,
            Actions.Set3Star => Set3Star,
            Actions.Set4Star => Set4Star,
            Actions.Set5Star => Set5Star,
            
            // Background and lock screen image
            Actions.SetAsLockScreen => SetAsLockScreen,
            Actions.SetAsLockscreenCentered => SetAsLockScreen,
            Actions.SetAsWallpaper => SetAsWallpaper,
            Actions.SetAsWallpaperFitted => SetAsWallpaperFitted,
            Actions.SetAsWallpaperStretched => SetAsWallpaperStretched,
            Actions.SetAsWallpaperFilled => SetAsWallpaperFilled,
            Actions.SetAsWallpaperCentered => SetAsWallpaperCentered,
            Actions.SetAsWallpaperTiled => SetAsWallpaperTiled,
            
            // Tabs
            Actions.NewTab => NewTab,
            Actions.CloseTab => CloseTab,

            // Misc
            Actions.ChangeBackground => ChangeBackground,
            Actions.SideBySide => SideBySide,
            Actions.Slideshow => Slideshow,
            Actions.ColorPicker => ColorPicker,
            Actions.Restart => Restart,

            // Hidden Actions of buttons, menus and settings
            Actions.CloseWindow => CloseWindow,
            Actions.Minimize => Minimize,
            Actions.OpenCloseDockedGallery => OpenCloseDockedGallery,
            Actions.ResetSettings => ResetSettings,
            Actions.ToggleBottomToolbar => ToggleBottomToolbar,
            Actions.ToggleConstrainBackgroundColor => ToggleConstrainBackgroundColor,
            Actions.ToggleFileHistory => ToggleFileHistory,
            Actions.ToggleHoverBar => ToggleHoverBar,
            Actions.ToggleOpeningInSameWindow => ToggleOpeningInSameWindow,
            Actions.ToggleShowFullPathInTitleBar => ToggleShowFullPathInTitleBar,
            Actions.ToggleSubdirectories => ToggleSubdirectories,
            Actions.ToggleTaskbarProgress => ToggleTaskbarProgress,

            _ when KeybindCategoryDefinition.FavoriteSlot(functionName) is { } slot => () => OpenFavorite(slot),

            _ => null
        };
    }

    #region Functions

    #region Navigation, zoom and rotation

    /// <inheritdoc cref="Core.ViewModels.TabOverviewViewModel.NextFile()" />
    private async ValueTask Next() =>
        await _vm.WindowTabs.NavigateDirectionalAsync(_mainWindow.KeybindRouter.IsKeyHeldDown,
            NavigateTo.Next).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateToNextFolderAsync" />
    private async ValueTask NextFolder() =>
        await _vm.WindowTabs.NextFolder().ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateToNextArchiveAsync" />
    private async ValueTask NextArchive() =>
        await _vm.WindowTabs.NextArchive().ConfigureAwait(false);
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateByIncrementsAsync" />
    private async ValueTask Last() =>
        await _vm.WindowTabs.LastFile().ConfigureAwait(false);

    /// <inheritdoc cref="Core.ViewModels.TabOverviewViewModel.PrevFile()" />
    private async ValueTask Prev() =>
        await _vm.WindowTabs.NavigateDirectionalAsync(_mainWindow.KeybindRouter.IsKeyHeldDown,
            NavigateTo.Previous).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateToPreviousFolderAsync" />
    private async ValueTask PrevFolder() =>
        await _vm.WindowTabs.PrevFolder().ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateToPreviousArchiveAsync" />
    private async ValueTask PrevArchive() =>
        await _vm.WindowTabs.PrevArchive().ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateByIncrementsAsync" />
    private async ValueTask First() =>
        await _vm.WindowTabs.FirstFile().ConfigureAwait(false);
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateByIncrementsAsync" />
    private async ValueTask Next10() =>
        await _vm.WindowTabs.Next10().ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateByIncrementsAsync" />
    private async ValueTask Next100() =>
        await _vm.WindowTabs.Next100().ConfigureAwait(false);
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateByIncrementsAsync" />
    private async ValueTask Prev10() =>
        await _vm.WindowTabs.Prev10().ConfigureAwait(false);
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateByIncrementsAsync" />
    private async ValueTask Prev100() =>
        await _vm.WindowTabs.Prev100().ConfigureAwait(false);

    private async ValueTask FileHistoryNext() =>
        await UINavigationHelper.OpenNextFileHistoryEntry(_mainWindow, _vm).ConfigureAwait(false);

    private async ValueTask FileHistoryPrev() =>
        await UINavigationHelper.OpenPreviousFileHistoryEntry(_mainWindow, _vm).ConfigureAwait(false);

    public ValueTask StopRepeatedNavigation()
    {
        _vm.WindowTabs.StopRepeatedNavigation();
        return ValueTask.CompletedTask;
    }

    private async ValueTask Search() =>
        await Dispatcher.UIThread.InvokeAsync(_mainWindow.AddFileSearchDialog);
    
    private ValueTask Up() => ArrowKeyNavigation.Up(_vm, _mainWindow);

    /// <inheritdoc cref="RotationManager.RotateRight(MainWindowViewModel, MainWindow)" />
    private ValueTask RotateRight()
    {
        RotationManager.RotateRight(_vm, _mainWindow);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="RotationManager.RotateLeft(MainWindowViewModel, MainWindow)" />
    private ValueTask RotateLeft()
    {
        RotationManager.RotateLeft(_vm, _mainWindow);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc cref="RotationManager.Rotate(MainWindowViewModel, int, MainWindow)" />
    private ValueTask Rotate0()
    {
        RotationManager.Rotate(_vm, 0, _mainWindow);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc cref="RotationManager.Rotate(MainWindowViewModel, int, MainWindow)" />
    private ValueTask Rotate90()
    {
        RotationManager.Rotate(_vm, 1, _mainWindow);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc cref="RotationManager.Rotate(MainWindowViewModel, int, MainWindow)" />
    private ValueTask Rotate180()
    {
        RotationManager.Rotate(_vm, 2, _mainWindow);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc cref="RotationManager.Rotate(MainWindowViewModel, int, MainWindow)" />
    private ValueTask Rotate270()
    {
        RotationManager.Rotate(_vm, 3, _mainWindow);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc cref="RotationManager.Flip(MainWindowViewModel)" />
    private ValueTask Flip()
    {
        RotationManager.Flip(_vm);
        return ValueTask.CompletedTask;
    }

    private ValueTask Down() => ArrowKeyNavigation.Down(_vm, _mainWindow);

    private ValueTask ScrollDown() => ViewerActions.ScrollLine(_vm, down: true);

    private ValueTask ScrollUp() => ViewerActions.ScrollLine(_vm, down: false);

    private ValueTask ScrollToTop() => ViewerActions.ScrollToTop(_vm);

    private ValueTask ScrollToBottom() => ViewerActions.ScrollToBottom(_vm);

    private ValueTask ZoomIn() => ViewerActions.ZoomIn(_vm);

    private ValueTask ZoomOut() => ViewerActions.ZoomOut(_vm);

    private ValueTask ResetZoomAndRotations()
    {
        RotationManager.ResetZoomAndRotations(_vm, _mainWindow);
        return ValueTask.CompletedTask;
    }

    private ValueTask ResetZoom()
    {
        RotationManager.ResetZoom(_vm);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Gallery functions

    private ValueTask ToggleGallery()
    {
        _vm.WindowTabs.ActiveTab.CurrentValue.Gallery.Switcher.ToggleExpanded();
        return ValueTask.CompletedTask;
    }

    private ValueTask OpenCloseDockedGallery() => _vm.WindowTabs.ActiveTab.CurrentValue.Gallery.Switcher.Contract();

    #endregion
    
    #region Windows and window functions
    
    /// <inheritdoc cref="MainWindow.HandleShouldClosing" />
    private async ValueTask Close()
    {
        await _mainWindow.HandleShouldClosing(_vm).ConfigureAwait(false);
    }
    
    private ValueTask CloseWindow()
    {
        _mainWindow.Close();
        return ValueTask.CompletedTask;
    }

    private ValueTask Center()
    {
        UIHelper.Center(_vm, _mainWindow);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="WindowModeSwitcher.ToggleMaximized" />
    private ValueTask Maximize() =>
        _mainWindow.WindowModeSwitcher.ToggleMaximized();
    
    /// <inheritdoc cref="WindowModeSwitcher.LeaveTopMode" />
    private ValueTask Restore() =>
        _mainWindow.WindowModeSwitcher.LeaveTopMode();

    private ValueTask Minimize()
    {
        _mainWindow.Minimize();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="MainWindowLifecycle.OpenNewAsync" />
    private async ValueTask NewWindow()
    {
        if (_mainWindow.Lifecycle is { } lifecycle)
        {
            await lifecycle.OpenNewAsync(_mainWindow).ConfigureAwait(false);
        }
    }

    private ValueTask AboutWindow()
    {
        _mainWindow.MainWindowInitializer.ShowAboutWindow();
        return ValueTask.CompletedTask;
    }

    private ValueTask CheckForUpdates() => PlatformActions.CheckForUpdates(_mainWindow);

    private ValueTask ConvertWindow() =>
        _mainWindow.MainWindowInitializer.ShowConvertWindow();

    private ValueTask KeybindingsWindow()
    {
        _mainWindow.MainWindowInitializer.ShowKeybindingsWindow();
        return ValueTask.CompletedTask;
    }

    private ValueTask EffectsWindow()
    {
        _ = _mainWindow.MainWindowInitializer.ShowEffectsWindow();
        return ValueTask.CompletedTask;
    }

    private ValueTask ImageInfoWindow()
    {
        _mainWindow.MainWindowInitializer.ShowImageInfoWindow(_vm);
        return ValueTask.CompletedTask;
    }

    private ValueTask ResizeWindow() =>
        _mainWindow.MainWindowInitializer.ShowSingleImageResizeWindow();

    private async ValueTask BatchResizeWindow()
    {
        await _mainWindow.MainWindowInitializer.ShowBatchResizeWindow().ConfigureAwait(false);
    }

    private ValueTask FileAssociationsWindow()
    {
        _mainWindow.MainWindowInitializer.ShowFileAssociationsWindow();
        return ValueTask.CompletedTask;
    }

    private ValueTask SettingsWindow() =>
        _mainWindow.MainWindowInitializer.ShowSettingsWindow();

    #endregion Windows

    #region Image Scaling and Window Behavior
    
    private ValueTask ZoomToFit() => ToggleHelper.Toggle(_vm.GlobalSettings.IsZoomedToFit);
    
    /// <inheritdoc cref="WindowFunctions.ToggleAutoFit" />
    private async ValueTask AutoFitWindow() =>
        await WindowFunctions.ToggleAutoFit(_vm).ConfigureAwait(false);

    /// <summary>
    /// Turns AutoFit off in every window.
    /// </summary>
    private async ValueTask NormalWindow() =>
        await WindowFunctions.SetAutoFitInEveryWindow(false).ConfigureAwait(false);

    /// <inheritdoc cref="WindowModeSwitcher.ToggleFullscreen" />
    private ValueTask ToggleFullscreen() =>
        _mainWindow.WindowModeSwitcher.ToggleFullscreen();
    
    /// <summary>
    /// Turns top most on or off for every Main Window.
    /// </summary>
    private ValueTask SetTopMost() => ToggleHelper.Toggle(_vm.GlobalSettings.IsTopMost);

    #endregion
    
    
    #region Toggle UI functions

    private ValueTask ToggleScroll() => ToggleHelper.ToggleScroll(_vm, _mainWindow);

    private ValueTask ToggleLooping() => ToggleHelper.ToggleLooping(_vm, _mainWindow);

    private ValueTask ToggleInterface() => ToggleHelper.ToggleInterface(_vm, _mainWindow);

    private ValueTask ToggleHoverBar() => ToggleHelper.ToggleHoverBar(_vm);

    private ValueTask ToggleSubdirectories() => ToggleHelper.ToggleSubdirectories(_vm);

    private ValueTask ToggleBottomToolbar() => ToggleHelper.ToggleBottomToolbar(_vm);

    private ValueTask ToggleTaskbarProgress() => ToggleHelper.ToggleTaskbarProgress(_vm);

    private ValueTask ToggleConstrainBackgroundColor() => ToggleHelper.ToggleConstrainBackgroundColor(_vm);

    #endregion

    #region File funnctions

    /// <inheritdoc cref="UINavigationHelper.OpenPathAsync(MainWindow, MainWindowViewModel, string?, string?)" />
    public virtual async ValueTask OpenPathAsync(string path) =>
        await UINavigationHelper.OpenPathAsync(_mainWindow, _vm, path).ConfigureAwait(false);

    /// <summary>
    /// Opens the folder of the favorite in <paramref name="slot"/>. Does nothing when the slot is free.
    /// </summary>
    private ValueTask OpenFavorite(int slot) =>
        FavoritesManager.PathForSlot(slot) is { } path ? OpenPathAsync(path) : ValueTask.CompletedTask;

    /// <inheritdoc cref="PrintJobStarter.PrintAsync(MainWindowViewModel)" />
    private async ValueTask Print() =>
        await _mainWindow.Printing.PrintAsync(_vm).ConfigureAwait(false);

    /// <inheritdoc cref="PrintJobStarter.SaveAsPdfAsync(MainWindowViewModel)" />
    private async ValueTask SaveAsPDF() =>
        await _mainWindow.Printing.SaveAsPdfAsync(_vm).ConfigureAwait(false);

    /// <inheritdoc cref="FilePicker.SelectAndLoadFile(MainWindow, MainWindowViewModel)" />
    private async ValueTask Open() => 
        await FilePicker.SelectAndLoadFile(_mainWindow, _vm).ConfigureAwait(false);

    /// <inheritdoc cref="FileManager.OpenWith(string)" />
    private ValueTask OpenWith() => PlatformActions.OpenWith(_vm);

    /// <inheritdoc cref="FileManager.LocateOnDisk(string)" />
    private ValueTask OpenInExplorer() => PlatformActions.OpenInExplorer(_vm);

    /// <inheritdoc cref="FileSavingService.SaveAsync(TabViewModel)" />
    private async ValueTask Save() =>
        await FileSaver.SaveCurrentFile(_vm).ConfigureAwait(false);

    /// <inheritdoc cref="FileSavingService.SaveAsAsync(TabViewModel)" />
    private async ValueTask SaveAs() =>
        await FileSaver.SaveFileAs(_vm).ConfigureAwait(false);

    /// <inheritdoc cref="FileManager.DeleteFileWithOptionalDialog" />
    private ValueTask DeleteFile() => PlatformActions.DeleteFile(_vm, _mainWindow, recycle: true);
    
    /// <inheritdoc cref="FileManager.DeleteFileWithOptionalDialog" />
    private ValueTask DeleteFilePermanently() => PlatformActions.DeleteFile(_vm, _mainWindow, recycle: false);

    private ValueTask Rename()
    {
        RenameHelper.Rename(_vm, _mainWindow);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="FileManager.ShowFileProperties(string)" />
    private ValueTask ShowFileProperties() => PlatformActions.ShowFileProperties(_vm);

    #endregion

    #region Copy and Paste functions

    /// <inheritdoc cref="ICopyClipboard.CopyFileAsync(string)" />
    private async ValueTask CopyFile()
    {
        if (_mainWindow.ClipboardCopyService is null)
        {
            return;
        }
        await _mainWindow.ClipboardCopyService
            .CopyFileAsync(_vm.WindowTabs.ActiveTab.CurrentValue?.FileInfo?.CurrentValue?.FullName)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="ClipboardCopyService.CopyTextAsync(string)" />
    private async ValueTask CopyFilePath()
    {
        if (_mainWindow.ClipboardCopyService is null)
        {
            return;
        }
        await _mainWindow.ClipboardCopyService
            .CopyTextAsync(_vm.WindowTabs.ActiveTab.CurrentValue?.FileInfo?.CurrentValue?.FullName)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="ICopyClipboard.CopyImageAsync(TabViewModel)" />
    private async ValueTask CopyImage()
    {
        if (_mainWindow.ClipboardCopyService is null || _vm.WindowTabs.ActiveTab.CurrentValue is not { } tab)
        {
            return;
        }
        await _mainWindow.ClipboardCopyService
            .CopyImageAsync(tab)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="ICopyClipboard.CopyBase64Async(string)" />
    private async ValueTask CopyBase64()
    {
        if (_mainWindow.ClipboardCopyService is null)
        {
            return;
        }
        await _mainWindow.ClipboardCopyService
            .CopyBase64Async(_vm.WindowTabs.ActiveTab.CurrentValue?.FileInfo?.CurrentValue?.FullName)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="FileManager.DuplicateFileAsync(string, string, MainWindowViewModel, MainWindow)" />
    private async ValueTask DuplicateFile()
    {
        await FileManager.DuplicateFileAsync(
            _vm.WindowTabs.ActiveTab.CurrentValue?.FileInfo?.CurrentValue?.FullName,
            _vm.WindowTabs.ActiveTab.CurrentValue?.FileInfo?.CurrentValue?.FullName,
            _vm,
            _mainWindow).ConfigureAwait(false);
    }

    /// <inheritdoc cref="ICopyClipboard.CutFileAsync(string)" />
    private async ValueTask CutFile()
    {
        if (_mainWindow.ClipboardCopyService is null)
        {
            return;
        }
        await _mainWindow.ClipboardCopyService
            .CutFileAsync(_vm.WindowTabs.ActiveTab.CurrentValue?.FileInfo?.CurrentValue?.FullName)
            .ConfigureAwait(false);
    }

    /// <inheritdoc cref="IPasteClipboard.PasteAsync(MainWindowViewModel)" />
    private async ValueTask Paste()
    {
        if (_mainWindow.ClipboardPasteService is null)
        {
            return;
        }
        await _mainWindow.ClipboardPasteService.PasteAsync(_vm).ConfigureAwait(false);
    }
    
    #endregion

    #region Image Functions
    
    /// <inheritdoc cref="BackgroundManager.ChangeBackground(MainWindowViewModel)" />
    private async ValueTask ChangeBackground() =>
        await BackgroundManager.ChangeBackgroundAsync(_vm).ConfigureAwait(false);

    private ValueTask SideBySide() => ToggleHelper.Toggle(_vm.GlobalSettings.IsSideBySide);

    /// <inheritdoc cref="Core.Navigation.Interfaces.IImageIterator.ReloadAsync"/>
    private async ValueTask Reload()
    {
        // A tab that never browsed files, such as a New Tab, has nothing to reload
        if (_vm.WindowTabs.ActiveTab.CurrentValue.ImageIterator is { } iterator)
        {
            await iterator.ReloadAsync().ConfigureAwait(false);
        }
    }

    private async ValueTask ResizeImage() =>
        await ResizeWindow().ConfigureAwait(false);

    /// <inheritdoc cref="CropManager.StartCropControlAsync(MainWindowViewModel, MainWindow)" />
    private async ValueTask Crop() =>
        await CropManager.StartCropControlAsync(_vm, _mainWindow).ConfigureAwait(false);

    /// <inheritdoc cref="ImageOptimizer.OptimizeImageAsync(MainWindowViewModel)" />
    private async ValueTask OptimizeImage() =>
        await ImageOptimizer.OptimizeImageAsync(_vm).ConfigureAwait(false);

    /// <inheritdoc cref="Navigation.Slideshow.StartSlideshow(MainWindow)" />
    private async ValueTask Slideshow() =>   
        await Navigation.Slideshow.StartSlideshow(_mainWindow).ConfigureAwait(false);

    private ValueTask ColorPicker()
    {
        return ValueTask.CompletedTask;
    }
    
    #endregion

    #region Sorting

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    private async ValueTask SortFilesByName() =>
        await _vm.WindowTabs.SortAsync(SortFilesBy.Name).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    private async ValueTask SortFilesByCreationTime() =>
        await _vm.WindowTabs.SortAsync(SortFilesBy.CreationTime).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    private async ValueTask SortFilesByLastAccessTime() =>
        await _vm.WindowTabs.SortAsync(SortFilesBy.LastAccessTime).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    private async ValueTask SortFilesByLastWriteTime() =>
        await _vm.WindowTabs.SortAsync(SortFilesBy.LastWriteTime).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    private async ValueTask SortFilesBySize() =>
        await _vm.WindowTabs.SortAsync(SortFilesBy.FileSize).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    private async ValueTask SortFilesByExtension() =>
        await _vm.WindowTabs.SortAsync(SortFilesBy.Extension).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    private async ValueTask SortFilesRandomly() =>
        await _vm.WindowTabs.SortAsync(SortFilesBy.Random).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    private async ValueTask SortFilesAscending() =>
        await _vm.WindowTabs.SortAsync(ascending: true).ConfigureAwait(false);
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    private async ValueTask SortFilesDescending() =>
        await _vm.WindowTabs.SortAsync(ascending: false).ConfigureAwait(false);

    #endregion Sorting

    #region Rating

    private async ValueTask Set0Star() =>
        await SetExifRatingHelper.Set0Star(_vm).ConfigureAwait(false);

    private async ValueTask Set1Star() =>
        await SetExifRatingHelper.Set1Star(_vm).ConfigureAwait(false);

    private async ValueTask Set2Star() =>
        await SetExifRatingHelper.Set2Star(_vm).ConfigureAwait(false);

    private async ValueTask Set3Star() =>
        await SetExifRatingHelper.Set3Star(_vm).ConfigureAwait(false);

    private async ValueTask Set4Star() =>
        await SetExifRatingHelper.Set4Star(_vm).ConfigureAwait(false);

    private async ValueTask Set5Star() =>
        await SetExifRatingHelper.Set5Star(_vm).ConfigureAwait(false);

    #endregion

    #region Wallpaper and lockscreen image

    private async ValueTask SetAsWallpaper() =>
        await SetAsWallpaperFilled().ConfigureAwait(false);

    private async ValueTask SetAsWallpaperTiled() =>
        await WallpaperManager.SetAsWallpaper(WallpaperStyle.Tile, _vm).ConfigureAwait(false);

    private async ValueTask SetAsWallpaperCentered() =>
        await WallpaperManager.SetAsWallpaper(WallpaperStyle.Center, _vm).ConfigureAwait(false);

    private async ValueTask SetAsWallpaperStretched() =>
        await WallpaperManager.SetAsWallpaper(WallpaperStyle.Stretch, _vm).ConfigureAwait(false);

    private async ValueTask SetAsWallpaperFitted() =>
        await WallpaperManager.SetAsWallpaper(WallpaperStyle.Fit, _vm).ConfigureAwait(false);

    private async ValueTask SetAsWallpaperFilled() =>
        await WallpaperManager.SetAsWallpaper(WallpaperStyle.Fill, _vm).ConfigureAwait(false);

    private ValueTask SetAsLockScreen() => PlatformActions.SetAsLockScreen(_vm);

    #endregion

    #region Tabs

    private ValueTask NewTab()
    {
        if (_vm.WindowTabs.ActiveTab.CurrentValue.Gallery.IsDocked.Value)
        {
            // TODO: Consecutive tabs or windows currently not supported when gallery is enabled
            return ValueTask.CompletedTask;
        }
        _vm.WindowTabs.CreateTab();
        return ValueTask.CompletedTask;
    }
    
    private ValueTask CloseTab()
    {
        _vm.WindowTabs.CloseTab();
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Other settings

    /// <inheritdoc cref="SettingsUpdater.ResetSettings" />
    private async ValueTask ResetSettings()
    {
        if (_mainWindow.Lifecycle is { } lifecycle)
        {
            await SettingsUpdater.ResetSettings(lifecycle).ConfigureAwait(false);
        }
    }

    /// <inheritdoc cref="AppLifecycle.RestartAsync" />
    private async ValueTask Restart()
    {
        if (_mainWindow.Lifecycle is { } lifecycle)
        {
            await lifecycle.RestartAsync().ConfigureAwait(false);
        }
    }
    
    private ValueTask ShowSettingsFile() => PlatformActions.ShowFile(_vm, CurrentSettingsPath);

    private ValueTask ShowKeybindingsFile() => PlatformActions.ShowFile(_vm, KeybindingFunctions.CurrentKeybindingsPath);

    private ValueTask ShowRecentHistoryFile() => PlatformActions.ShowFile(_vm, FileHistoryManager.CurrentFileHistoryFile);

    private ValueTask ShowFavoritesFile() => PlatformActions.ShowFile(_vm, FavoritesManager.CurrentFavoritesFile);
    
    private ValueTask ToggleOpeningInSameWindow() => ToggleHelper.Toggle(_vm.GlobalSettings.IsOpeningInSameWindow);

    private ValueTask ToggleFileHistory() => ToggleHelper.Toggle(_vm.GlobalSettings.IsFileHistoryEnabled);

    private ValueTask ToggleShowFullPathInTitleBar() => ToggleHelper.Toggle(_vm.GlobalSettings.IsShowingFullPathInTitleBar);

    #endregion
    
    #endregion
}


