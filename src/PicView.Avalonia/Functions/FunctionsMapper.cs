using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Clipboard;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.Crop;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.FileSystem;
using PicView.Avalonia.ImageHandling;
using PicView.Avalonia.ImageTransformations;
using PicView.Avalonia.SettingsManagement;
using PicView.Avalonia.UI;
using PicView.Avalonia.Views.UC;
using PicView.Avalonia.Input;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.Wallpaper;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.FileHistory;
using PicView.Core.FileSorting;
using PicView.Core.Gallery;
using PicView.Core.IPlatform;
using PicView.Core.Keybindings;
using PicView.Core.Navigation;
using PicView.Core.ProcessHandling;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Functions;

public class FunctionsMapper(MainWindowViewModel vm, MainWindow mainWindow) : IFunctionsMapper
{
    public Func<ValueTask>? GetFunctionByName(string functionName)
    {
        // Remember to have exact matching names, or it will be null
        return functionName switch
        {
            // Navigation values
            "Next" => Next,
            "Prev" => Prev,
            
            "NextFolder" => NextFolder,
            "PrevFolder" => PrevFolder,
            
            "Up" => Up,
            "Down" => Down,
            
            "Last" => Last,
            "First" => First,
            
            "Next10" => Next10,
            "Prev10" => Prev10,
            
            "Next100" => Next100,
            "Prev100" => Prev100,

            "Search" => Search,

            "NextArchive" => NextArchive,
            "PrevArchive" => PrevArchive,
            
            // Rotate
            "RotateLeft" => RotateLeft,
            "RotateRight" => RotateRight,
            "Rotate0" => Rotate0,
            "Rotate90" => Rotate90,
            "Rotate180" => Rotate180,
            "Rotate270" => Rotate270,

            // Scroll
            "ScrollUp" => ScrollUp,
            "ScrollDown" => ScrollDown,
            "ScrollToTop" => ScrollToTop,
            "ScrollToBottom" => ScrollToBottom,

            // Zoom
            "ZoomIn" => ZoomIn,
            "ZoomOut" => ZoomOut,
            "ResetZoom" => ResetZoom,
            "ResetZoomAndRotations" => ResetZoomAndRotations,
            "ChangeCtrlZoom" => ChangeCtrlZoom,

            // Toggles
            "ToggleScroll" => ToggleScroll,
            "ToggleLooping" => ToggleLooping,
            "ToggleGallery" => ToggleGallery,

            // Scale Window
            "AutoFitWindow" => AutoFitWindow,
            "NormalWindow" => NormalWindow,

            // Window functions
            "Fullscreen" => Fullscreen,
            "ToggleFullscreen" => ToggleFullscreen,
            "SetTopMost" => SetTopMost,
            "Close" => Close,
            "ToggleInterface" => ToggleInterface,
            "NewWindow" => NewWindow,
            "Center" => Center,
            "Maximize" => Maximize,
            "Restore" => Restore,

            // Windows
            "AboutWindow" => AboutWindow,
            "CheckForUpdates" => CheckForUpdates,
            "EffectsWindow" => EffectsWindow,
            "ImageInfoWindow" => ImageInfoWindow,
            "ResizeWindow" => ResizeWindow,
            "SettingsWindow" => SettingsWindow,
            "KeybindingsWindow" => KeybindingsWindow,
            "BatchResizeWindow" => BatchResizeWindow,
            "FileAssociationsWindow" => FileAssociationsWindow,
            "ConvertWindow" => ConvertWindow,

            // Open functions
            "Open" => Open,
            "OpenWith" => OpenWith,
            "OpenInExplorer" => OpenInExplorer,
            "Save" => Save,
            "SaveAs" => SaveAs,
            "SaveAsPDF" => SaveAsPDF,
            "Print" => Print,
            "Reload" => Reload,

            // Copy functions
            "CopyFile" => CopyFile,
            "CopyFilePath" => CopyFilePath,
            "CopyImage" => CopyImage,
            "CopyBase64" => CopyBase64,
            "DuplicateFile" => DuplicateFile,
            "CutFile" => CutFile,
            "Paste" => Paste,

            // File functions
            "DeleteFile" => DeleteFile,
            "DeleteFilePermanently" => DeleteFilePermanently,
            "Rename" => Rename,
            "ShowFileProperties" => ShowFileProperties,
            "ShowSettingsFile" => ShowSettingsFile,
            "ShowKeybindingsFile" => ShowKeybindingsFile,
            "ShowRecentHistoryFile" => ShowRecentHistoryFile,
            
            // Sorting functions
            "SortFilesByName" => SortFilesByName,
            "SortFilesByCreationTime" => SortFilesByCreationTime,
            "SortFilesByLastAccessTime" => SortFilesByLastAccessTime,
            "SortFilesByLastWriteTime" => SortFilesByLastWriteTime,
            "SortFilesBySize" => SortFilesBySize,
            "SortFilesByExtension" => SortFilesByExtension,
            "SortFilesRandomly" => SortFilesRandomly,
            
            "SortFilesAscending" => SortFilesAscending,
            "SortFilesDescending" => SortFilesDescending,
            
            // Image functions
            "ResizeImage" => ResizeImage,
            "Crop" => Crop,
            "Flip" => Flip,
            "OptimizeImage" => OptimizeImage,
            "Stretch" => ZoomToFit,

            // Set stars
            "Set0Star" => Set0Star,
            "Set1Star" => Set1Star,
            "Set2Star" => Set2Star,
            "Set3Star" => Set3Star,
            "Set4Star" => Set4Star,
            "Set5Star" => Set5Star,
            
            // Background and lock screen image
            "SetAsLockScreen" => SetAsLockScreen,
            "SetAsLockscreenCentered" => SetAsLockscreenCentered,
            "SetAsWallpaper" => SetAsWallpaper,
            "SetAsWallpaperFitted" => SetAsWallpaperFitted,
            "SetAsWallpaperStretched" => SetAsWallpaperStretched,
            "SetAsWallpaperFilled" => SetAsWallpaperFilled,
            "SetAsWallpaperCentered" => SetAsWallpaperCentered,
            "SetAsWallpaperTiled" => SetAsWallpaperTiled,
            
            // Tabs
            "NewTab" => NewTab,
            "CloseTab" => CloseTab,

            // Misc
            "ChangeBackground" => ChangeBackground,
            "SideBySide" => SideBySide,
            "GalleryClick" => GalleryClick,
            "Slideshow" => Slideshow,
            "ColorPicker" => ColorPicker,
            "Restart" => Restart,

            _ => null
        };
    }

    #region Functions

    #region Navigation, zoom and rotation

    /// <inheritdoc cref="Core.ViewModels.TabOverviewViewModel.NextFile()" />
    public async ValueTask Next() =>
        await vm.WindowTabs.NavigateDirectionalAsync(MainKeyboardShortcuts.IsKeyHeldDown,
            NavigateTo.Next).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateToNextFolderAsync" />
    public async ValueTask NextFolder() =>
        await vm.WindowTabs.NextFolder().ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateToNextArchiveAsync" />
    public async ValueTask NextArchive() =>
        await vm.WindowTabs.NextArchive().ConfigureAwait(false);
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateByIncrementsAsync" />
    public async ValueTask Last() =>
        await vm.WindowTabs.LastFile().ConfigureAwait(false);

    /// <inheritdoc cref="Core.ViewModels.TabOverviewViewModel.PrevFile()" />
    public async ValueTask Prev() =>
        await vm.WindowTabs.NavigateDirectionalAsync(MainKeyboardShortcuts.IsKeyHeldDown,
            NavigateTo.Previous).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateToPreviousFolderAsync" />
    public async ValueTask PrevFolder() =>
        await vm.WindowTabs.PrevFolder().ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateToPreviousArchiveAsync" />
    public async ValueTask PrevArchive() =>
        await vm.WindowTabs.PrevArchive().ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateByIncrementsAsync" />
    public async ValueTask First() =>
        await vm.WindowTabs.FirstFile().ConfigureAwait(false);
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateByIncrementsAsync" />
    public async ValueTask Next10() =>
        await vm.WindowTabs.Next10().ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateByIncrementsAsync" />
    public async ValueTask Next100() =>
        await vm.WindowTabs.Next100().ConfigureAwait(false);
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateByIncrementsAsync" />
    public async ValueTask Prev10() =>
        await vm.WindowTabs.Prev10().ConfigureAwait(false);
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.NavigateByIncrementsAsync" />
    public async ValueTask Prev100() =>
        await vm.WindowTabs.Prev100().ConfigureAwait(false);

    public async ValueTask Search() =>
        await Dispatcher.UIThread.InvokeAsync(mainWindow.AddFileSearchDialog);
    
    public async ValueTask Up()
    {
        if (vm.WindowTabs.ActiveTab.Value.Gallery.IsGalleryExpanded.Value)
        {
            await vm.WindowTabs.NavigateDirectionalAsync(MainKeyboardShortcuts.IsKeyHeldDown, NavigateTo.Up).ConfigureAwait(false);
            return;
        }

        await RotateRight();
    }

    /// <inheritdoc cref="RotationManager.RotateRight(MainWindowViewModel)" />
    public ValueTask RotateRight()
    {
        RotationManager.RotateRight(vm, mainWindow);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="RotationManager.RotateLeft(MainWindowViewModel)" />
    public ValueTask RotateLeft()
    {
        RotationManager.RotateLeft(vm, mainWindow);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc cref="RotationManager.Rotate(MainWindowViewModel, int)" />
    public ValueTask Rotate0()
    {
        RotationManager.Rotate(vm, 0, mainWindow);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc cref="RotationManager.Rotate(MainWindowViewModel, int)" />
    public ValueTask Rotate90()
    {
        RotationManager.Rotate(vm, 90, mainWindow);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc cref="RotationManager.Rotate(MainWindowViewModel, int)" />
    public ValueTask Rotate180()
    {
        RotationManager.Rotate(vm, 180, mainWindow);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc cref="RotationManager.Rotate(MainWindowViewModel, int)" />
    public ValueTask Rotate270()
    {
        RotationManager.Rotate(vm, 270, mainWindow);
        return ValueTask.CompletedTask;
    }
    /// <inheritdoc cref="RotationManager.Flip(MainWindowViewModel)" />
    public ValueTask Flip()
    {
        RotationManager.Flip(vm);
        return ValueTask.CompletedTask;
    }

    public async ValueTask Down()
    {
        if (vm.WindowTabs.ActiveTab.Value.Gallery.IsGalleryExpanded.Value)
        {
            await vm.WindowTabs.NavigateDirectionalAsync(MainKeyboardShortcuts.IsKeyHeldDown, NavigateTo.Down).ConfigureAwait(false);
            return;
        }

        await RotateLeft();
    }
    
    public async ValueTask ScrollDown()
    {
        if (vm.WindowTabs.ActiveTab.Value.CurrentView.CurrentValue is not ImageViewer imageViewer)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            imageViewer.ImageScrollViewer.LineDown();
        });
    }
    
    public async ValueTask ScrollUp()
    {
        if (vm.WindowTabs.ActiveTab.Value.CurrentView.CurrentValue is not ImageViewer imageViewer)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            imageViewer.ImageScrollViewer.LineUp();
        });
    }

    public async ValueTask ScrollToTop()
    {
        if (vm.WindowTabs.ActiveTab.Value.CurrentView.CurrentValue is not ImageViewer imageViewer)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            imageViewer.ImageScrollViewer.ScrollToHome();
        });
    }

    public async ValueTask ScrollToBottom()
    {
        if (vm.WindowTabs.ActiveTab.Value.CurrentView.CurrentValue is not ImageViewer imageViewer)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            imageViewer.ImageScrollViewer.ScrollToEnd();
        });
    }

    public ValueTask ZoomIn()
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer)
        {
            imageViewer.ZoomIn();
        }
        return ValueTask.CompletedTask;
    }

    public ValueTask ZoomOut()
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.CurrentView.CurrentValue is ImageViewer imageViewer)
        {
            imageViewer.ZoomOut();
        }
        return ValueTask.CompletedTask;
    }
    
    public ValueTask ResetZoomAndRotations()
    {
        RotationManager.ResetZoomAndRotations(vm, mainWindow);
        return ValueTask.CompletedTask;
    }

    public ValueTask ResetZoom()
    {
        RotationManager.ResetZoom(vm, mainWindow);
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Toggle UI functions
    
    public ValueTask ToggleDropDownMenu()
    {
        vm.TopTitlebarViewModel.ToggleDropDownMenu(default);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="SettingsUpdater.ToggleScroll(MainWindowViewModel)" />
    public async ValueTask ToggleScroll()
    {
        await SettingsUpdater.ToggleScroll(vm, mainWindow).ConfigureAwait(false);
    }

    /// <inheritdoc cref="SettingsUpdater.ToggleCtrlZoom(MainWindowViewModel)" />
    public async ValueTask ChangeCtrlZoom()
    {
        await SettingsUpdater.ToggleCtrlZoom(vm).ConfigureAwait(false);
    }

    /// <inheritdoc cref="SettingsUpdater.ToggleLooping(MainWindowViewModel)" />
    public async ValueTask ToggleLooping()
    {
        await SettingsUpdater.ToggleLooping(vm).ConfigureAwait(false);
    }
    
    /// <inheritdoc cref="ToggleUIVisibility.ToggleInterface(MainWindowViewModel)" />
    public async ValueTask ToggleInterface()
    {
        await ToggleUIVisibility.ToggleInterface(vm, mainWindow).ConfigureAwait(false);
    }
    
    /// <inheritdoc cref="ToggleUIVisibility.ToggleHoverBar(MainWindowViewModel)" />
    public async ValueTask ToggleHoverBar()
    {
        await ToggleUIVisibility.ToggleHoverBar(vm).ConfigureAwait(false);
    }
    
    /// <inheritdoc cref="SettingsUpdater.ToggleSubdirectories(MainWindowViewModel)" />
    public async ValueTask ToggleSubdirectories() =>
        await SettingsUpdater.ToggleSubdirectories(vm).ConfigureAwait(false);

    /// <inheritdoc cref="ToggleUIVisibility.ToggleBottomBar(MainWindowViewModel)" />
    public async ValueTask ToggleBottomToolbar()
    {
        await ToggleUIVisibility.ToggleBottomBar(vm, mainWindow).ConfigureAwait(false);
    }
    
    /// <inheritdoc cref="SettingsUpdater.ToggleTaskbarProgress(MainWindowViewModel)" />
    public async ValueTask ToggleTaskbarProgress()
    {
        await SettingsUpdater.ToggleTaskbarProgress(vm).ConfigureAwait(false);
    }
    
    /// <inheritdoc cref="SettingsUpdater.ToggleConstrainBackgroundColor()" />
    public async ValueTask ToggleConstrainBackgroundColor()
    {
        await SettingsUpdater.ToggleConstrainBackgroundColor().ConfigureAwait(false);
    }
    
    #endregion

    #region Gallery functions

    public ValueTask ToggleGallery()
    {
        vm.WindowTabs.ActiveTab.CurrentValue.Gallery.ToggleGalleryCommand.Execute(Unit.Default);
        return ValueTask.CompletedTask;
    }

    public ValueTask OpenCloseDockedGallery()
    {
        vm.WindowTabs.ActiveTab.CurrentValue.Gallery.ContractToDockedOrCloseGalleryCommand.Execute(Unit.Default);
        return ValueTask.CompletedTask;
    }
    
    public ValueTask CloseGallery()
    {
        vm.WindowTabs.ActiveTab.CurrentValue.Gallery.CloseGalleryCommand.Execute(Unit.Default);
        return ValueTask.CompletedTask;
    }
    
    public async ValueTask GalleryClick()
    {
        var tab = vm.WindowTabs.ActiveTab.CurrentValue;
        await GalleryLoader.ToggleGalleryAndLoadItem(tab, tab.Gallery.SelectedGalleryItemIndex.Value);
    }

    #endregion
    
    #region Windows and window functions
    
    /// <inheritdoc cref="DialogManager.HandleShouldClosing" />
    public async ValueTask Close()
    {
        await mainWindow.HandleShouldClosing(vm);
    }
    
    public ValueTask Exit()
    {
        mainWindow.Close();
        return ValueTask.CompletedTask;
    }

    public ValueTask Center()
    {
        UIHelper.Center(vm);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="Interfaces.IPlatformWindowService.MaximizeRestore" />
    public async ValueTask Maximize()
    {
        await vm.PlatformWindowService.MaximizeRestore();
    }
    
    /// <inheritdoc cref="Interfaces.IPlatformWindowService.Restore" />
    public async ValueTask Restore()
    {
        await vm.PlatformWindowService.Restore();
    }

    public ValueTask Minimize()
    {
        vm.PlatformWindowService.Minimize();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="ProcessHelper.StartNewProcess()" />
    public async ValueTask NewWindow() =>
        await Task.Run(ProcessHelper.StartNewProcess).ConfigureAwait(false);

    public ValueTask ShowStartUpMenu()
    {
        ViewChangeHelper.SwitchToStartUpMenu(vm);
        return ValueTask.CompletedTask;
    }

    public ValueTask AboutWindow()
    {
        vm?.PlatformWindowService?.ShowAboutWindow();
        return ValueTask.CompletedTask;
    }

    public async ValueTask CheckForUpdates()
    {
        await Dispatcher.UIThread.InvokeAsync(() => vm.PlatformWindowService?.ShowAboutWindow());
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        await core.AboutView.UpdateCurrentVersion();
    }

    public ValueTask ConvertWindow()
    {
        vm?.PlatformWindowService?.ShowConvertWindow();
        return ValueTask.CompletedTask;
    }

    public ValueTask KeybindingsWindow()
    {
        vm?.PlatformWindowService?.ShowKeybindingsWindow();
        return ValueTask.CompletedTask;
    }

    public ValueTask EffectsWindow()
    {
        vm?.PlatformWindowService?.ShowEffectsWindow();
        return ValueTask.CompletedTask;
    }

    public ValueTask ImageInfoWindow()
    {
        vm?.PlatformWindowService?.ShowImageInfoWindow();
        return ValueTask.CompletedTask;
    }

    public ValueTask ResizeWindow()
    {
        vm?.PlatformWindowService?.ShowSingleImageResizeWindow();
        return ValueTask.CompletedTask;
    }

    public async ValueTask BatchResizeWindow()
    {
        await vm.PlatformWindowService.ShowBatchResizeWindow();
    }

    public ValueTask FileAssociationsWindow()
    {
        vm?.PlatformWindowService?.ShowFileAssociationsWindow();
        return ValueTask.CompletedTask;
    }

    public ValueTask SettingsWindow() =>
        vm.PlatformWindowService.ShowSettingsWindow();

    #endregion Windows

    #region Image Scaling and Window Behavior
    
    /// <inheritdoc cref="SettingsUpdater.ToggleZoomToFit" />
    public async ValueTask ZoomToFit()
    {
        await SettingsUpdater.ToggleZoomToFit(vm, mainWindow);
    }
    
    /// <inheritdoc cref="WindowFunctions.ToggleAutoFit()" />
    public async ValueTask AutoFitWindow() =>
        await WindowFunctions.ToggleAutoFit();

    /// <inheritdoc cref="WindowFunctions.SetManualWindows" />
    public ValueTask NormalWindow()
    {
        WindowFunctions.SetManualWindows();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="Interfaces.IPlatformWindowService.ToggleFullscreen" />
    public async ValueTask ToggleFullscreen() =>
        await vm.PlatformWindowService.ToggleFullscreen().ConfigureAwait(false);
    
    // This shouldn't be here, but keep as alias and backwards compatibility.
    public ValueTask Fullscreen() =>
        ToggleFullscreen();

    /// <inheritdoc cref="WindowFunctions.ToggleTopMost(MainWindowViewModel)" />
    public async ValueTask SetTopMost() =>
        await WindowFunctions.ToggleTopMost(vm).ConfigureAwait(false);

    #endregion

    #region File funnctions

    /// <inheritdoc cref=" UINavigationHelper.OpenLastFile(MainWindowViewModel)" />
    public async ValueTask OpenLastFile() =>
        await UINavigationHelper.OpenLastFile(mainWindow, vm).ConfigureAwait(false);

    public async ValueTask OpenPreviousFileHistoryEntry() =>
        await UINavigationHelper.OpenPreviousFileHistoryEntry(mainWindow, vm).ConfigureAwait(false);

    public async ValueTask OpenNextFileHistoryEntry() =>
        await UINavigationHelper.OpenNextFileHistoryEntry(mainWindow, vm).ConfigureAwait(false);

    public async ValueTask Print()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        await core.PlatformService.Print(vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo?.FullName);
    }
    
    public async ValueTask SaveAsPDF() =>
        await PdfExport.SavePdfWithFilePicker(vm);

    /// <inheritdoc cref="FilePicker.SelectAndLoadFile(MainWindowViewModel)" />
    public async ValueTask Open()
    {
        await FilePicker.SelectAndLoadFile(mainWindow, vm).ConfigureAwait(false);
    }

    /// <inheritdoc cref="FileManager.OpenWith(string)" />
    public ValueTask OpenWith()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return ValueTask.CompletedTask;
        }

        core.PlatformService.OpenWith(vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo?.FullName);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="FileManager.LocateOnDisk(string)" />
    public ValueTask OpenInExplorer()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return ValueTask.CompletedTask;
        }

        core.PlatformService.LocateOnDisk(vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo?.FullName);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="FileSaverHelper.SaveCurrentFile(MainWindowViewModel)" />
    public async ValueTask Save()
    {
        await FileSaverHelper.SaveCurrentFile(vm).ConfigureAwait(false);
    }
    
    /// <inheritdoc cref="FileSaverHelper.SaveFileAs(MainWindowViewModel)" />
    public async ValueTask SaveAs()
    {
        await FileSaverHelper.SaveFileAs(vm).ConfigureAwait(false);
    }
    
    /// <inheritdoc cref="FileManager.DeleteFileWithOptionalDialog" />
    public async ValueTask DeleteFile()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        const bool recycle = true;
        await FileManager
            .DeleteFileWithOptionalDialog(recycle, vm.WindowTabs.ActiveTab.CurrentValue.Model
                .FileInfo?.FullName, core.PlatformService, mainWindow)
            .ConfigureAwait(false);
    }
    
    /// <inheritdoc cref="FileManager.DeleteFileWithOptionalDialog" />
    public async ValueTask DeleteFilePermanently()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        const bool recycle = false;
        await FileManager
            .DeleteFileWithOptionalDialog(recycle, vm.WindowTabs.ActiveTab.CurrentValue.Model
                .FileInfo?.FullName, core.PlatformService, mainWindow)
            .ConfigureAwait(false);
    }

    public ValueTask Rename()
    {
        RenameHelper.Rename(vm, mainWindow);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc cref="FileManager.ShowFileProperties(string)" />
    public async ValueTask ShowFileProperties()
    {
        await Task.Run(() =>
            FileManager.ShowFileProperties(vm.WindowTabs.ActiveTab.CurrentValue.Model
                .FileInfo?.FullName)).ConfigureAwait(false);
    }

    #endregion

    #region Copy and Paste functions

    /// <inheritdoc cref="ClipboardFileOperations.CopyFileToClipboard(string, MainWindow)" />
    public async ValueTask CopyFile() =>
        await ClipboardFileOperations.CopyFileToClipboard(vm.WindowTabs.ActiveTab.CurrentValue.Model
            .FileInfo?.FullName, mainWindow).ConfigureAwait(false);

    /// <inheritdoc cref="ClipboardTextOperations.CopyTextToClipboard(string, MainWindow)" />
    public async ValueTask CopyFilePath() =>
        await ClipboardTextOperations.CopyTextToClipboard(vm.WindowTabs.ActiveTab.CurrentValue.Model
            .FileInfo?.FullName, mainWindow).ConfigureAwait(false);

    /// <inheritdoc cref="ClipboardImageOperations.CopyImageToClipboard(MainWindowViewModel, MainWindow)" />
    public async ValueTask CopyImage() =>
        await ClipboardImageOperations.CopyImageToClipboard(vm, mainWindow).ConfigureAwait(false);

    /// <inheritdoc cref="ClipboardImageOperations.CopyBase64ToClipboard(string, MainWindow)" />
    public async ValueTask CopyBase64() => 
        await ClipboardImageOperations.CopyBase64ToClipboard(vm.WindowTabs.ActiveTab.CurrentValue?.FileInfo?.CurrentValue?.FullName, mainWindow).ConfigureAwait(false);

    /// <inheritdoc cref="ClipboardFileOperations.Duplicate(string, MainWindowViewModel, MainWindow)" />
    public async ValueTask DuplicateFile() =>
        await ClipboardFileOperations.Duplicate(vm.WindowTabs.ActiveTab.CurrentValue.Model
            .FileInfo?.FullName, vm, mainWindow).ConfigureAwait(false);

    /// <inheritdoc cref="ClipboardFileOperations.CutFile(string)" />
    public async ValueTask CutFile() =>
        await ClipboardFileOperations.CutFile(vm.WindowTabs.ActiveTab.CurrentValue.Model
            .FileInfo?.FullName).ConfigureAwait(false);

    /// <inheritdoc cref="ClipboardPasteOperations.Paste(MainWindowViewModel, MainWindow)" />
    public async ValueTask Paste() =>
        await ClipboardPasteOperations.Paste(vm, mainWindow).ConfigureAwait(false);
    
    #endregion

    #region Image Functions
    
    /// <inheritdoc cref="BackgroundManager.ChangeBackground(MainWindowViewModel)" />
    public async ValueTask ChangeBackground() =>
        await BackgroundManager.ChangeBackgroundAsync(vm).ConfigureAwait(false);

    /// <inheritdoc cref="SettingsUpdater.ToggleSideBySide" />
    public async ValueTask SideBySide() =>
        await SettingsUpdater.ToggleSideBySide(mainWindow).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.IImageIterator.ReloadAsync"/>
    public async ValueTask Reload() =>
        await vm.WindowTabs.ActiveTab.CurrentValue.ImageIterator.ReloadAsync().ConfigureAwait(false);

    public async ValueTask ResizeImage() =>
        await ResizeWindow();

    /// <inheritdoc cref="CropManager.StartCropControlAsync(MainWindowViewModel, MainWindow)" />
    public async ValueTask Crop() =>
        await CropManager.StartCropControlAsync(vm, mainWindow).ConfigureAwait(false);

    /// <inheritdoc cref="ImageOptimizer.OptimizeImageAsync(MainWindowViewModel)" />
    public async ValueTask OptimizeImage() =>
        await ImageOptimizer.OptimizeImageAsync(vm).ConfigureAwait(false);

    /// <inheritdoc cref="Navigation.Slideshow.StartSlideshow(MainWindowViewModel)" />
    public async ValueTask Slideshow() =>   
        await Navigation.Slideshow.StartSlideshow(vm).ConfigureAwait(false);

    public ValueTask ColorPicker()
    {
        throw new NotImplementedException();
    }
    
    #endregion

    #region Sorting

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    public async ValueTask SortFilesByName() =>
        await vm.WindowTabs.SortAsync(SortFilesBy.Name).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    public async ValueTask SortFilesByCreationTime() =>
        await vm.WindowTabs.SortAsync(SortFilesBy.CreationTime).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    public async ValueTask SortFilesByLastAccessTime() =>
        await vm.WindowTabs.SortAsync(SortFilesBy.LastAccessTime).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    public async ValueTask SortFilesByLastWriteTime() =>
        await vm.WindowTabs.SortAsync(SortFilesBy.LastWriteTime).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    public async ValueTask SortFilesBySize() =>
        await vm.WindowTabs.SortAsync(SortFilesBy.FileSize).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    public async ValueTask SortFilesByExtension() =>
        await vm.WindowTabs.SortAsync(SortFilesBy.Extension).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    public async ValueTask SortFilesRandomly() =>
        await vm.WindowTabs.SortAsync(SortFilesBy.Random).ConfigureAwait(false);

    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    public async ValueTask SortFilesAscending() =>
        await vm.WindowTabs.SortAsync(ascending: true).ConfigureAwait(false);
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.INavigationService.SortAsync(TabViewModel, bool, CancellationTokenSource)" />
    public async ValueTask SortFilesDescending() =>
        await vm.WindowTabs.SortAsync(ascending: false).ConfigureAwait(false);

    #endregion Sorting

    #region Rating

    public async ValueTask Set0Star() =>
        await SetExifRatingHelper.Set0Star(vm);

    public async ValueTask Set1Star() =>
        await SetExifRatingHelper.Set1Star(vm);

    public async ValueTask Set2Star() =>
        await SetExifRatingHelper.Set2Star(vm);

    public async ValueTask Set3Star() =>
        await SetExifRatingHelper.Set3Star(vm);

    public async ValueTask Set4Star() =>
        await SetExifRatingHelper.Set4Star(vm);

    public async ValueTask Set5Star() =>
        await SetExifRatingHelper.Set5Star(vm);

    #endregion

    #region Wallpaper and lockscreen image

    public async ValueTask SetAsWallpaper() =>
        await SetAsWallpaperFilled();

    public async ValueTask SetAsWallpaperTiled() =>
        await WallpaperManager.SetAsWallpaper(vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo.FullName, WallpaperStyle.Tile, vm);

    public async ValueTask SetAsWallpaperCentered() =>
        await WallpaperManager.SetAsWallpaper(vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo.FullName, WallpaperStyle.Center, vm);

    public async ValueTask SetAsWallpaperStretched() =>
        await WallpaperManager.SetAsWallpaper(vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo.FullName, WallpaperStyle.Stretch, vm);

    public async ValueTask SetAsWallpaperFitted() =>
        await WallpaperManager.SetAsWallpaper(vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo.FullName, WallpaperStyle.Fit, vm);

    public async ValueTask SetAsWallpaperFilled() =>
        await WallpaperManager.SetAsWallpaper(vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo.FullName, WallpaperStyle.Fill, vm);

    public async ValueTask SetAsLockscreenCentered()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }
        await Task.Run(() => core.PlatformService.SetAsLockScreen(vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo.FullName)).ConfigureAwait(false);
    }

    public async ValueTask SetAsLockScreen()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return;
        }

        await Task.Run(() =>
            core.PlatformService.SetAsLockScreen(vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo
                .FullName)).ConfigureAwait(false);
    }

    #endregion

    #region Tabs

    public ValueTask NewTab()
    {
        if (vm.WindowTabs.ActiveTab.CurrentValue.Gallery.IsGalleryDocked.CurrentValue)
        {
            // TODO: Consecutive tabs or windows currently not supported when gallery is enabled
            return ValueTask.CompletedTask;
        }
        vm.WindowTabs.CreateTab();
        return ValueTask.CompletedTask;
    }
    
    public ValueTask CloseTab()
    {
        vm.WindowTabs.CloseTab();
        return ValueTask.CompletedTask;
    }
    
    public ValueTask StopRepeatedNavigation()
    {
        vm?.WindowTabs?.StopRepeatedNavigation();
        return ValueTask.CompletedTask;
    }

    #endregion

    #region Other settings

    /// <inheritdoc cref="SettingsUpdater.ResetSettings()" />
    public async ValueTask ResetSettings() =>
        await SettingsUpdater.ResetSettings();

    public ValueTask Restart()
    {
        AppFunctions.Restart(vm.WindowTabs.ActiveTab.CurrentValue);
        return ValueTask.CompletedTask;
    }
    
    public ValueTask ShowSettingsFile()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return ValueTask.CompletedTask;
        }
        core.PlatformService.OpenWith(CurrentSettingsPath);
        return ValueTask.CompletedTask;
    }
    
    public ValueTask ShowKeybindingsFile()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return ValueTask.CompletedTask;
        }
        core.PlatformService.OpenWith(KeybindingFunctions.CurrentKeybindingsPath);
        return ValueTask.CompletedTask;
    }
    
    public ValueTask ShowRecentHistoryFile()
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return ValueTask.CompletedTask;
        }
        core.PlatformService.OpenWith(FileHistoryManager.CurrentFileHistoryFile);
        return ValueTask.CompletedTask;
    }
    
    public async ValueTask ToggleOpeningInSameWindow() =>
        await SettingsUpdater.ToggleOpeningInSameWindow().ConfigureAwait(false);
    
    public async ValueTask ToggleFileHistory() =>
        await SettingsUpdater.ToggleFileHistory(vm).ConfigureAwait(false);
    
    public async ValueTask ToggleShowFullPathInTitleBar() =>
        await SettingsUpdater.ToggleShowFullPathInTitleBar(vm).ConfigureAwait(false);

    #endregion
    
    #endregion
}
