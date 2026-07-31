using PicView.Core.IPlatform;
using PicView.Core.Sizing;
using R3;

namespace PicView.Core.ViewModels;

public class MainWindowViewModel : IDisposable
{
    #region Properties
    public IFunctionsMapper? Mapper { get; set; }
    public IPlatformWindowService? PlatformWindowService { get; }
    
    public TranslationViewModel Translation { get;  } 
    public GallerySharedSettingsViewModel GallerySettings { get; }
    public GlobalSettingsViewModel GlobalSettings { get; }
    public TopTitlebarViewModel TopTitlebarViewModel { get; }  = new();
    public TabOverviewViewModel WindowTabs { get; }
    public ToolTipViewModel? ToolTip { get; set; }
    public PrintPreviewViewModel? PrintPreview { get; set; }
    public ImageInfoWindowViewModel? InfoWindow { get; set; } 
    public ExifViewModel? Exif { get; set; }
    public ResizeImageViewModel? ResizeImageViewModel { get; set; }

    #region Window state
    public bool IsNavigationButtonLeftClicked { get; set; }
    public bool IsNavigationButtonRightClicked { get; set; }
    
    public bool IsClickArrowLeftClicked { get; set; }
    public bool IsClickArrowRightClicked { get; set; }

    public bool IsBottomToolbarRightRotationClicked { get; set; }
    public bool IsBottomToolbarLeftRotationClicked { get; set; }
    public BindableReactiveProperty<bool> IsBottomToolbarShown { get; } = new(Settings.UIProperties.ShowBottomNavBar);
    public BindableReactiveProperty<bool> IsAutoFit { get; } = new(Settings.WindowProperties.AutoFit);
    public BindableReactiveProperty<bool> IsSideBySide { get; } = new(Settings.ImageScaling.ShowImageSideBySide);
    
    public BindableReactiveProperty<double> ScrollViewerWidth { get; } = new(double.NaN);
    
    public BindableReactiveProperty<double> ScrollViewerHeight { get; } = new(double.NaN);

    public static int WindowMinWidth => SizeDefaults.WindowMinSize;
    public static int WindowMinHeight => SizeDefaults.WindowMinSize;

    public BindableReactiveProperty<double> WindowMaxWidth { get; } = new(double.NaN);

    public BindableReactiveProperty<double> WindowMaxHeight { get; } = new(double.NaN);

    /// <summary>
    /// The width to scale the image to
    /// </summary>
    public BindableReactiveProperty<double> ImageWidth { get; } = new(double.NaN);

    /// <summary>
    /// The height to scale the image to
    /// </summary>
    public BindableReactiveProperty<double> ImageHeight { get; } = new(double.NaN);

    public BindableReactiveProperty<double> TitlebarHeight { get; } = new();

    public BindableReactiveProperty<double> BottombarHeight { get; } = new();

    public BindableReactiveProperty<bool> IsFullscreen { get; } = new();

    public BindableReactiveProperty<bool> IsMaximized { get; } = new();

    public BindableReactiveProperty<bool> ShouldRestoreBeShown { get; } = new();

    public BindableReactiveProperty<bool> ShouldMaximizeBeShown { get; } = new(true);

    public BindableReactiveProperty<bool> IsLoadingIndicatorShown { get; } = new();

    public BindableReactiveProperty<bool> IsUIShown { get; } = new();
    public BindableReactiveProperty<bool> IsTopToolbarShown { get; } = new();

    public BindableReactiveProperty<bool> IsEditableTitlebarOpen { get; } = new();
    
    public BindableReactiveProperty<bool> IsScrollingEnabled { get; } = new(Settings.Zoom.ScrollEnabled);

    public BindableReactiveProperty<bool> IsZoomedToFit { get; } = new(Settings.ImageScaling.ZoomToFit);
    
    public BindableReactiveProperty<bool> IsTopMost { get; } = new(Settings.WindowProperties.TopMost);
    
    public BindableReactiveProperty<object?> ChangeCtrlZoomImage { get; } = new();
    
    #endregion
    
    #endregion

    #region Commands

    #region Navigation

    public ReactiveCommand NextCommand { get; }
    private async ValueTask Next(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Next(); }

    public ReactiveCommand NextFolderCommand { get; }
    private async ValueTask NextFolder(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.NextFolder(); }

    public ReactiveCommand NextArchiveCommand { get; }
    private async ValueTask NextArchive(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.NextArchive(); }

    public ReactiveCommand LastCommand { get; }
    private async ValueTask Last(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Last(); }

    public ReactiveCommand PrevCommand { get; }
    private async ValueTask Prev(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Prev(); }

    public ReactiveCommand PrevFolderCommand { get; }
    private async ValueTask PrevFolder(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.PrevFolder(); }

    public ReactiveCommand PrevArchiveCommand { get; }
    private async ValueTask PrevArchive(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.PrevArchive(); }

    public ReactiveCommand FirstCommand { get; }
    private async ValueTask First(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.First(); }

    public ReactiveCommand Next10Command { get; }
    private async ValueTask Next10(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Next10(); }

    public ReactiveCommand Next100Command { get; }
    private async ValueTask Next100(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Next100(); }

    public ReactiveCommand Prev10Command { get; }
    private async ValueTask Prev10(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Prev10(); }

    public ReactiveCommand Prev100Command { get; }
    private async ValueTask Prev100(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Prev100(); }

    #endregion

    #region Viewport / Zoom

    public ReactiveCommand SearchCommand { get; }
    private async ValueTask Search(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Search(); }

    public ReactiveCommand UpCommand { get; }
    private async ValueTask Up(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Up(); }

    public ReactiveCommand RotateRightCommand { get; }
    private async ValueTask RotateRight(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.RotateRight(); }

    public ReactiveCommand RotateLeftCommand { get; }
    private async ValueTask RotateLeft(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.RotateLeft(); }
    
    public ReactiveCommand Rotate0Command { get; }
    private async ValueTask Rotate0(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Rotate0(); }
    
    public ReactiveCommand Rotate90Command { get; }
    private async ValueTask Rotate90(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Rotate90(); }
    
    public ReactiveCommand Rotate180Command { get; }
    private async ValueTask Rotate180(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Rotate180(); }
    
    public ReactiveCommand Rotate270Command { get; }
    private async ValueTask Rotate270(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Rotate270(); }
    
    public ReactiveCommand DownCommand { get; }
    private async ValueTask Down(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Down(); }

    public ReactiveCommand ScrollDownCommand { get; }
    private async ValueTask ScrollDown(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ScrollDown(); }

    public ReactiveCommand ScrollUpCommand { get; }
    private async ValueTask ScrollUp(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ScrollUp(); }

    public ReactiveCommand ScrollToTopCommand { get; }
    private async ValueTask ScrollToTop(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ScrollToTop(); }

    public ReactiveCommand ScrollToBottomCommand { get; }
    private async ValueTask ScrollToBottom(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ScrollToBottom(); }

    public ReactiveCommand ZoomInCommand { get; }
    private async ValueTask ZoomIn(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ZoomIn(); }

    public ReactiveCommand ZoomOutCommand { get; }
    private async ValueTask ZoomOut(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ZoomOut(); }

    public ReactiveCommand ResetZoomCommand { get; }
    private async ValueTask ResetZoom(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ResetZoom(); }
    public ReactiveCommand ResetZoomAndRotationsCommand { get; }
    private async ValueTask ResetZoomAndRotations(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ResetZoomAndRotations(); }

    public ReactiveCommand ToggleScrollCommand { get; }
    private async ValueTask ToggleScroll(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleScroll(); }

    public ReactiveCommand ChangeCtrlZoomCommand { get; }
    private async ValueTask ChangeCtrlZoom(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ChangeCtrlZoom(); }

    #endregion

    #region Interface Toggles

    public ReactiveCommand ToggleLoopingCommand { get; }
    private async ValueTask ToggleLooping(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleLooping(); }

    public ReactiveCommand ToggleInterfaceCommand { get; }
    private async ValueTask ToggleInterface(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleInterface(); }
    
    public ReactiveCommand ToggleHoverBarCommand { get; }
    private async ValueTask ToggleHoverBar(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleHoverBar(); }

    public ReactiveCommand ToggleSubdirectoriesCommand { get; }
    private async ValueTask ToggleSubdirectories(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleSubdirectories(); }

    public ReactiveCommand ToggleBottomToolbarCommand { get; }
    private async ValueTask ToggleBottomToolbar(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleBottomToolbar(); }

    public ReactiveCommand ToggleTaskbarProgressCommand { get; }
    private async ValueTask ToggleTaskbarProgress(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleTaskbarProgress(); }

    public ReactiveCommand ToggleConstrainBackgroundColorCommand { get; }
    private async ValueTask ToggleConstrainBackgroundColor(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleConstrainBackgroundColor(); }

    public ReactiveCommand ToggleGalleryCommand { get; }
    private async ValueTask ToggleGallery(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleGallery(); }

    public ReactiveCommand OpenCloseBottomGalleryCommand { get; }
    private async ValueTask OpenCloseBottomGallery(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.OpenCloseDockedGallery(); }

    public ReactiveCommand CloseGalleryCommand { get; }
    private async ValueTask CloseGallery(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.CloseGallery(); }

    public ReactiveCommand GalleryClickCommand { get; }
    private async ValueTask GalleryClick(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.GalleryClick(); }
    
    public ReactiveCommand ToggleDockedGalleryInHiddenUICommand { get; }
    private async ValueTask ToggleDockedGalleryInHiddenUI(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleDockedGalleryInHiddenUI(); }

    #endregion

    #region Windows & Dialogs
    public ReactiveCommand AboutWindowCommand { get; }
    private async ValueTask AboutWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.AboutWindow(); }

    public ReactiveCommand ConvertWindowCommand { get; }
    private async ValueTask ConvertWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ConvertWindow(); }

    public ReactiveCommand KeybindingsWindowCommand { get; }
    private async ValueTask KeybindingsWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.KeybindingsWindow(); }

    public ReactiveCommand EffectsWindowCommand { get; }
    private async ValueTask EffectsWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.EffectsWindow(); }

    public ReactiveCommand ImageInfoWindowCommand { get; }
    private async ValueTask ImageInfoWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ImageInfoWindow(); }

    public ReactiveCommand ResizeWindowCommand { get; }
    private async ValueTask ResizeWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ResizeWindow(); }

    public ReactiveCommand BatchResizeWindowCommand { get; }
    private async ValueTask BatchResizeWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.BatchResizeWindow(); }

    public ReactiveCommand FileAssociationsWindowCommand { get; }
    private async ValueTask FileAssociationsWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.FileAssociationsWindow(); }

    public ReactiveCommand SettingsWindowCommand { get; }
    private async ValueTask SettingsWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SettingsWindow(); }
    
    public ReactiveCommand CheckForUpdatesCommand { get; }
    private async ValueTask CheckForUpdates(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.CheckForUpdates(); }

    #endregion

    #region Window State

    public ReactiveCommand ZoomToFitCommand { get; }
    private async ValueTask ZoomToFit(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ZoomToFit(); }

    public ReactiveCommand AutoFitWindowCommand { get; }
    private async ValueTask AutoFitWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.AutoFitWindow(); }

    public ReactiveCommand NormalWindowCommand { get; }
    private async ValueTask NormalWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.NormalWindow(); }

    public ReactiveCommand ToggleFullscreenCommand { get; }
    private async ValueTask ToggleFullscreen(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleFullscreen(); }

    public ReactiveCommand FullscreenCommand { get; }
    private async ValueTask Fullscreen(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Fullscreen(); }

    public ReactiveCommand SetTopMostCommand { get; }
    private async ValueTask SetTopMost(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SetTopMost(); }

    public ReactiveCommand CloseCommand { get; }
    private async ValueTask Close(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Close(); }

    public ReactiveCommand ExitCommand { get; }
    private async ValueTask Exit(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Exit(); }

    public ReactiveCommand CenterCommand { get; }
    private async ValueTask Center(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Center(); }

    public ReactiveCommand MaximizeCommand { get; }
    private async ValueTask Maximize(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Maximize(); }

    public ReactiveCommand MinimizeCommand { get; }
    private async ValueTask Minimize(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Minimize(); }

    public ReactiveCommand RestoreCommand { get; }
    private async ValueTask Restore(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Restore(); }

    public ReactiveCommand NewWindowCommand { get; }
    private async ValueTask NewWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.NewWindow(); }

    #endregion

    #region File Operations

    public ReactiveCommand OpenLastFileCommand { get; }
    private async ValueTask OpenLastFile(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.OpenLastFile(); }

    public ReactiveCommand OpenPreviousFileHistoryEntryCommand { get; }
    private async ValueTask OpenPreviousFileHistoryEntry(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.OpenPreviousFileHistoryEntry(); }

    public ReactiveCommand OpenNextFileHistoryEntryCommand { get; }
    private async ValueTask OpenNextFileHistoryEntry(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.OpenNextFileHistoryEntry(); }

    public ReactiveCommand PrintCommand { get; }
    private async ValueTask Print(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Print(); }

    public ReactiveCommand OpenCommand { get; }
    private async ValueTask Open(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Open(); }

    public ReactiveCommand OpenWithCommand { get; }
    private async ValueTask OpenWith(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.OpenWith(); }

    public ReactiveCommand OpenInExplorerCommand { get; }
    private async ValueTask OpenInExplorer(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.OpenInExplorer(); }

    public ReactiveCommand SaveCommand { get; }
    private async ValueTask Save(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Save(); }

    public ReactiveCommand SaveAsCommand { get; }
    private async ValueTask SaveAs(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SaveAs(); }
    public ReactiveCommand SaveAsPDFCommand { get; }
    private async ValueTask SaveAsPDF(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SaveAsPDF(); }

    public ReactiveCommand DeleteFileCommand { get; }
    private async ValueTask DeleteFile(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.DeleteFile(); }

    public ReactiveCommand DeleteFilePermanentlyCommand { get; }
    private async ValueTask DeleteFilePermanently(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.DeleteFilePermanently(); }

    public ReactiveCommand RenameCommand { get; }
    private async ValueTask Rename(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Rename(); }

    public ReactiveCommand ShowFilePropertiesCommand { get; }
    private async ValueTask ShowFileProperties(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ShowFileProperties(); }

    #endregion

    #region Clipboard & Edit

    public ReactiveCommand CopyFileCommand { get; }
    private async ValueTask CopyFile(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.CopyFile(); }

    public ReactiveCommand CopyFilePathCommand { get; }
    private async ValueTask CopyFilePath(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.CopyFilePath(); }

    public ReactiveCommand CopyImageCommand { get; }
    private async ValueTask CopyImage(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.CopyImage(); }

    public ReactiveCommand CopyBase64Command { get; }
    private async ValueTask CopyBase64(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.CopyBase64(); }

    public ReactiveCommand DuplicateFileCommand { get; }
    private async ValueTask DuplicateFile(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.DuplicateFile(); }

    public ReactiveCommand CutFileCommand { get; }
    private async ValueTask CutFile(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.CutFile(); }

    public ReactiveCommand PasteCommand { get; }
    private async ValueTask Paste(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Paste(); }

    #endregion

    #region Image Operations

    public ReactiveCommand ChangeBackgroundCommand { get; }
    private async ValueTask ChangeBackground(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ChangeBackground(); }

    public ReactiveCommand SideBySideCommand { get; }
    private async ValueTask SideBySide(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SideBySide(); }

    public ReactiveCommand ReloadCommand { get; }
    private async ValueTask Reload(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Reload(); }

    public ReactiveCommand ResizeImageCommand { get; }
    private async ValueTask ResizeImage(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ResizeImage(); }

    public ReactiveCommand CropCommand { get; }
    private async ValueTask StartCrop(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Crop(); }

    public ReactiveCommand FlipCommand { get; }
    private async ValueTask Flip(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Flip(); }

    public ReactiveCommand OptimizeImageCommand { get; }
    private async ValueTask OptimizeImage(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.OptimizeImage(); }

    public ReactiveCommand SlideshowCommand { get; }
    private async ValueTask Slideshow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Slideshow(); }

    public ReactiveCommand ColorPickerCommand { get; }
    private async ValueTask ColorPicker(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ColorPicker(); }

    #endregion

    #region Sorting

    public ReactiveCommand SortFilesByNameCommand { get; }
    private async ValueTask SortFilesByName(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SortFilesByName(); }

    public ReactiveCommand SortFilesByCreationTimeCommand { get; }
    private async ValueTask SortFilesByCreationTime(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SortFilesByCreationTime(); }

    public ReactiveCommand SortFilesByLastAccessTimeCommand { get; }
    private async ValueTask SortFilesByLastAccessTime(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SortFilesByLastAccessTime(); }

    public ReactiveCommand SortFilesByLastWriteTimeCommand { get; }
    private async ValueTask SortFilesByLastWriteTime(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SortFilesByLastWriteTime(); }

    public ReactiveCommand SortFilesBySizeCommand { get; }
    private async ValueTask SortFilesBySize(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SortFilesBySize(); }

    public ReactiveCommand SortFilesByExtensionCommand { get; }
    private async ValueTask SortFilesByExtension(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SortFilesByExtension(); }

    public ReactiveCommand SortFilesRandomlyCommand { get; }
    private async ValueTask SortFilesRandomly(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SortFilesRandomly(); }

    public ReactiveCommand SortFilesAscendingCommand { get; }
    private async ValueTask SortFilesAscending(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SortFilesAscending(); }

    public ReactiveCommand SortFilesDescendingCommand { get; }
    private async ValueTask SortFilesDescending(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SortFilesDescending(); }

    #endregion

    #region Ratings

    public ReactiveCommand Set0StarCommand { get; }
    private async ValueTask Set0Star(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Set0Star(); }

    public ReactiveCommand Set1StarCommand { get; }
    private async ValueTask Set1Star(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Set1Star(); }

    public ReactiveCommand Set2StarCommand { get; }
    private async ValueTask Set2Star(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Set2Star(); }

    public ReactiveCommand Set3StarCommand { get; }
    private async ValueTask Set3Star(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Set3Star(); }

    public ReactiveCommand Set4StarCommand { get; }
    private async ValueTask Set4Star(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Set4Star(); }

    public ReactiveCommand Set5StarCommand { get; }
    private async ValueTask Set5Star(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Set5Star(); }

    #endregion

    #region Wallpaper

    public ReactiveCommand SetAsWallpaperCommand { get; }
    private async ValueTask SetAsWallpaper(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SetAsWallpaper(); }

    public ReactiveCommand SetAsWallpaperTiledCommand { get; }
    private async ValueTask SetAsWallpaperTiled(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SetAsWallpaperTiled(); }

    public ReactiveCommand SetAsWallpaperCenteredCommand { get; }
    private async ValueTask SetAsWallpaperCentered(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SetAsWallpaperCentered(); }

    public ReactiveCommand SetAsWallpaperStretchedCommand { get; }
    private async ValueTask SetAsWallpaperStretched(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SetAsWallpaperStretched(); }

    public ReactiveCommand SetAsWallpaperFittedCommand { get; }
    private async ValueTask SetAsWallpaperFitted(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SetAsWallpaperFitted(); }

    public ReactiveCommand SetAsWallpaperFilledCommand { get; }
    private async ValueTask SetAsWallpaperFilled(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SetAsWallpaperFilled(); }

    public ReactiveCommand SetAsLockscreenCenteredCommand { get; }
    private async ValueTask SetAsLockscreenCentered(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SetAsLockscreenCentered(); }

    public ReactiveCommand SetAsLockScreenCommand { get; }
    private async ValueTask SetAsLockScreen(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.SetAsLockScreen(); }

    #endregion

    #region Tabs

    public ReactiveCommand NewTabCommand { get; }
    private async ValueTask NewTab(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.NewTab(); }

    public ReactiveCommand CloseTabCommand { get; }
    private async ValueTask CloseTab(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.CloseTab(); }

    #endregion

    #region System & Settings

    public ReactiveCommand ResetSettingsCommand { get; }
    private async ValueTask ResetSettings(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ResetSettings(); }

    public ReactiveCommand RestartCommand { get; }
    private async ValueTask Restart(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.Restart(); }

    public ReactiveCommand ShowSettingsFileCommand { get; }
    private async ValueTask ShowSettingsFile(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ShowSettingsFile(); }

    public ReactiveCommand ShowKeybindingsFileCommand { get; }
    private async ValueTask ShowKeybindingsFile(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ShowKeybindingsFile(); }

    public ReactiveCommand ShowRecentHistoryFileCommand { get; }
    private async ValueTask ShowRecentHistoryFile(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ShowRecentHistoryFile(); }

    public ReactiveCommand ToggleOpeningInSameWindowCommand { get; }
    private async ValueTask ToggleOpeningInSameWindow(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleOpeningInSameWindow(); }

    public ReactiveCommand ToggleFileHistoryCommand { get; }
    private async ValueTask ToggleFileHistory(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleFileHistory(); }
    
    public ReactiveCommand ToggleShowFullPathInTitleBarCommand { get; }
    private async ValueTask ToggleShowFullPathInTitleBar(Unit unit, CancellationToken cancellationToken) { if (Mapper is null) return; await Mapper.ToggleShowFullPathInTitleBar(); }

    #endregion
    
    #endregion

    public MainWindowViewModel(TranslationViewModel translations, IPlatformWindowService windowService, GlobalSettingsViewModel globalSettings, GallerySharedSettingsViewModel gallerySettings)
    {
        Translation = translations;
        PlatformWindowService = windowService;
        GlobalSettings = globalSettings;
        GallerySettings = gallerySettings;
        WindowTabs = new TabOverviewViewModel(this);
        
        // Navigation
        NextCommand = new ReactiveCommand(Next);
        NextFolderCommand = new ReactiveCommand(NextFolder);
        NextArchiveCommand = new ReactiveCommand(NextArchive);
        LastCommand = new ReactiveCommand(Last);
        PrevCommand = new ReactiveCommand(Prev);
        PrevFolderCommand = new ReactiveCommand(PrevFolder);
        PrevArchiveCommand = new ReactiveCommand(PrevArchive);
        FirstCommand = new ReactiveCommand(First);
        Next10Command = new ReactiveCommand(Next10);
        Next100Command = new ReactiveCommand(Next100);
        Prev10Command = new ReactiveCommand(Prev10);
        Prev100Command = new ReactiveCommand(Prev100);

        // Viewport / Zoom
        SearchCommand = new ReactiveCommand(Search);
        UpCommand = new ReactiveCommand(Up);
        RotateRightCommand = new ReactiveCommand(RotateRight);
        RotateLeftCommand = new ReactiveCommand(RotateLeft);
        Rotate0Command = new ReactiveCommand(Rotate0);
        Rotate90Command = new ReactiveCommand(Rotate90);
        Rotate180Command = new ReactiveCommand(Rotate180);
        Rotate270Command = new ReactiveCommand(Rotate270);
        DownCommand = new ReactiveCommand(Down);
        ScrollDownCommand = new ReactiveCommand(ScrollDown);
        ScrollUpCommand = new ReactiveCommand(ScrollUp);
        ScrollToTopCommand = new ReactiveCommand(ScrollToTop);
        ScrollToBottomCommand = new ReactiveCommand(ScrollToBottom);
        ZoomInCommand = new ReactiveCommand(ZoomIn);
        ZoomOutCommand = new ReactiveCommand(ZoomOut);
        ResetZoomCommand = new ReactiveCommand(ResetZoom);
        ResetZoomAndRotationsCommand = new ReactiveCommand(ResetZoomAndRotations);
        ToggleScrollCommand = new ReactiveCommand(ToggleScroll);
        ChangeCtrlZoomCommand = new ReactiveCommand(ChangeCtrlZoom);

        // Interface Toggles
        ToggleLoopingCommand = new ReactiveCommand(ToggleLooping);
        ToggleInterfaceCommand = new ReactiveCommand(ToggleInterface);
        ToggleHoverBarCommand = new ReactiveCommand(ToggleHoverBar);
        ToggleSubdirectoriesCommand = new ReactiveCommand(ToggleSubdirectories);
        ToggleBottomToolbarCommand = new ReactiveCommand(ToggleBottomToolbar);
        ToggleTaskbarProgressCommand = new ReactiveCommand(ToggleTaskbarProgress);
        ToggleConstrainBackgroundColorCommand = new ReactiveCommand(ToggleConstrainBackgroundColor);
        ToggleGalleryCommand = new ReactiveCommand(ToggleGallery);
        OpenCloseBottomGalleryCommand = new ReactiveCommand(OpenCloseBottomGallery);
        CloseGalleryCommand = new ReactiveCommand(CloseGallery);
        GalleryClickCommand = new ReactiveCommand(GalleryClick);
        ToggleDockedGalleryInHiddenUICommand = new ReactiveCommand(ToggleDockedGalleryInHiddenUI);

        // Windows & Dialogs
        AboutWindowCommand = new ReactiveCommand(AboutWindow);
        CheckForUpdatesCommand = new ReactiveCommand(CheckForUpdates);
        ConvertWindowCommand = new ReactiveCommand(ConvertWindow);
        KeybindingsWindowCommand = new ReactiveCommand(KeybindingsWindow);
        EffectsWindowCommand = new ReactiveCommand(EffectsWindow);
        ImageInfoWindowCommand = new ReactiveCommand(ImageInfoWindow);
        ResizeWindowCommand = new ReactiveCommand(ResizeWindow);
        BatchResizeWindowCommand = new ReactiveCommand(BatchResizeWindow);
        FileAssociationsWindowCommand = new ReactiveCommand(FileAssociationsWindow);
        SettingsWindowCommand = new ReactiveCommand(SettingsWindow);

        // Window State
        ZoomToFitCommand = new ReactiveCommand(ZoomToFit);
        AutoFitWindowCommand = new ReactiveCommand(AutoFitWindow);
        NormalWindowCommand = new ReactiveCommand(NormalWindow);
        ToggleFullscreenCommand = new ReactiveCommand(ToggleFullscreen);
        FullscreenCommand = new ReactiveCommand(Fullscreen);
        SetTopMostCommand = new ReactiveCommand(SetTopMost);
        CloseCommand = new ReactiveCommand(Close);
        ExitCommand = new ReactiveCommand(Exit);
        CenterCommand = new ReactiveCommand(Center);
        MaximizeCommand = new ReactiveCommand(Maximize);
        MinimizeCommand = new ReactiveCommand(Minimize);
        RestoreCommand = new ReactiveCommand(Restore);
        NewWindowCommand = new ReactiveCommand(NewWindow);

        // File Operations
        OpenLastFileCommand = new ReactiveCommand(OpenLastFile);
        OpenPreviousFileHistoryEntryCommand = new ReactiveCommand(OpenPreviousFileHistoryEntry);
        OpenNextFileHistoryEntryCommand = new ReactiveCommand(OpenNextFileHistoryEntry);
        PrintCommand = new ReactiveCommand(Print);
        OpenCommand = new ReactiveCommand(Open);
        OpenWithCommand = new ReactiveCommand(OpenWith);
        OpenInExplorerCommand = new ReactiveCommand(OpenInExplorer);
        SaveCommand = new ReactiveCommand(Save);
        SaveAsCommand = new ReactiveCommand(SaveAs);
        SaveAsPDFCommand = new ReactiveCommand(SaveAsPDF);
        DeleteFileCommand = new ReactiveCommand(DeleteFile);
        DeleteFilePermanentlyCommand = new ReactiveCommand(DeleteFilePermanently);
        RenameCommand = new ReactiveCommand(Rename);
        ShowFilePropertiesCommand = new ReactiveCommand(ShowFileProperties);

        // Clipboard & Edit
        CopyFileCommand = new ReactiveCommand(CopyFile);
        CopyFilePathCommand = new ReactiveCommand(CopyFilePath);
        CopyImageCommand = new ReactiveCommand(CopyImage);
        CopyBase64Command = new ReactiveCommand(CopyBase64);
        DuplicateFileCommand = new ReactiveCommand(DuplicateFile);
        CutFileCommand = new ReactiveCommand(CutFile);
        PasteCommand = new ReactiveCommand(Paste);

        // Image Operations
        ChangeBackgroundCommand = new ReactiveCommand(ChangeBackground);
        SideBySideCommand = new ReactiveCommand(SideBySide);
        ReloadCommand = new ReactiveCommand(Reload);
        ResizeImageCommand = new ReactiveCommand(ResizeImage);
        CropCommand = new ReactiveCommand(StartCrop);
        FlipCommand = new ReactiveCommand(Flip);
        OptimizeImageCommand = new ReactiveCommand(OptimizeImage);
        SlideshowCommand = new ReactiveCommand(Slideshow);
        ColorPickerCommand = new ReactiveCommand(ColorPicker);

        // Sorting
        SortFilesByNameCommand = new ReactiveCommand(SortFilesByName);
        SortFilesByCreationTimeCommand = new ReactiveCommand(SortFilesByCreationTime);
        SortFilesByLastAccessTimeCommand = new ReactiveCommand(SortFilesByLastAccessTime);
        SortFilesByLastWriteTimeCommand = new ReactiveCommand(SortFilesByLastWriteTime);
        SortFilesBySizeCommand = new ReactiveCommand(SortFilesBySize);
        SortFilesByExtensionCommand = new ReactiveCommand(SortFilesByExtension);
        SortFilesRandomlyCommand = new ReactiveCommand(SortFilesRandomly);
        SortFilesAscendingCommand = new ReactiveCommand(SortFilesAscending);
        SortFilesDescendingCommand = new ReactiveCommand(SortFilesDescending);

        // Ratings
        Set0StarCommand = new ReactiveCommand(Set0Star);
        Set1StarCommand = new ReactiveCommand(Set1Star);
        Set2StarCommand = new ReactiveCommand(Set2Star);
        Set3StarCommand = new ReactiveCommand(Set3Star);
        Set4StarCommand = new ReactiveCommand(Set4Star);
        Set5StarCommand = new ReactiveCommand(Set5Star);

        // Wallpaper
        SetAsWallpaperCommand = new ReactiveCommand(SetAsWallpaper);
        SetAsWallpaperTiledCommand = new ReactiveCommand(SetAsWallpaperTiled);
        SetAsWallpaperCenteredCommand = new ReactiveCommand(SetAsWallpaperCentered);
        SetAsWallpaperStretchedCommand = new ReactiveCommand(SetAsWallpaperStretched);
        SetAsWallpaperFittedCommand = new ReactiveCommand(SetAsWallpaperFitted);
        SetAsWallpaperFilledCommand = new ReactiveCommand(SetAsWallpaperFilled);
        SetAsLockscreenCenteredCommand = new ReactiveCommand(SetAsLockscreenCentered);
        SetAsLockScreenCommand = new ReactiveCommand(SetAsLockScreen);

        // Tabs
        NewTabCommand = new ReactiveCommand(NewTab);
        CloseTabCommand = new ReactiveCommand(CloseTab);

        // System & Settings
        ResetSettingsCommand = new ReactiveCommand(ResetSettings);
        RestartCommand = new ReactiveCommand(Restart);
        ShowSettingsFileCommand = new ReactiveCommand(ShowSettingsFile);
        ShowKeybindingsFileCommand = new ReactiveCommand(ShowKeybindingsFile);
        ShowRecentHistoryFileCommand = new ReactiveCommand(ShowRecentHistoryFile);
        ToggleOpeningInSameWindowCommand = new ReactiveCommand(ToggleOpeningInSameWindow);
        ToggleFileHistoryCommand = new ReactiveCommand(ToggleFileHistory);
        ToggleShowFullPathInTitleBarCommand = new ReactiveCommand(ToggleShowFullPathInTitleBar);
    }

    public void Dispose()
    {
        Disposable.Dispose(
            ResizeImageViewModel,
            ScrollViewerWidth,
            ScrollViewerHeight,
            WindowMaxWidth,
            WindowMaxHeight,
            ImageWidth,
            ImageHeight,
            TitlebarHeight,
            BottombarHeight,
            IsFullscreen,
            IsMaximized,
            ShouldRestoreBeShown,
            ShouldMaximizeBeShown,
            IsLoadingIndicatorShown,
            IsUIShown,
            IsTopToolbarShown,
            IsEditableTitlebarOpen,
            NextCommand,
            NextFolderCommand,
            NextArchiveCommand,
            LastCommand,
            PrevCommand,
            PrevFolderCommand,
            PrevArchiveCommand,
            FirstCommand,
            Next10Command,
            Next100Command,
            Prev10Command,
            Prev100Command,
            SearchCommand,
            UpCommand,
            RotateRightCommand,
            RotateLeftCommand,
            Rotate0Command,
            Rotate90Command,
            Rotate180Command,
            Rotate270Command,
            DownCommand,
            ScrollDownCommand,
            ScrollUpCommand,
            ScrollToTopCommand,
            ScrollToBottomCommand,
            ZoomInCommand,
            ZoomOutCommand,
            ResetZoomCommand,
            ResetZoomAndRotationsCommand,
            ToggleScrollCommand,
            ChangeCtrlZoomCommand,
            ToggleLoopingCommand,
            ToggleInterfaceCommand,
            ToggleHoverBarCommand,
            ToggleSubdirectoriesCommand,
            ToggleBottomToolbarCommand,
            ToggleTaskbarProgressCommand,
            ToggleConstrainBackgroundColorCommand,
            ToggleGalleryCommand,
            OpenCloseBottomGalleryCommand,
            CloseGalleryCommand,
            GalleryClickCommand,
            ToggleDockedGalleryInHiddenUICommand,
            AboutWindowCommand,
            ConvertWindowCommand,
            KeybindingsWindowCommand,
            EffectsWindowCommand,
            ImageInfoWindowCommand,
            ResizeWindowCommand,
            BatchResizeWindowCommand,
            SettingsWindowCommand,
            CheckForUpdatesCommand,
            ZoomToFitCommand,
            AutoFitWindowCommand,
            NormalWindowCommand,
            ToggleFullscreenCommand,
            FullscreenCommand,
            SetTopMostCommand,
            CloseCommand,
            ExitCommand,
            CenterCommand,
            MaximizeCommand,
            MinimizeCommand,
            RestoreCommand,
            NewWindowCommand,
            OpenLastFileCommand,
            OpenPreviousFileHistoryEntryCommand,
            OpenNextFileHistoryEntryCommand,
            PrintCommand,
            OpenCommand,
            OpenWithCommand,
            OpenInExplorerCommand,
            SaveCommand,
            SaveAsCommand,
            SaveAsPDFCommand,
            DeleteFileCommand,
            DeleteFilePermanentlyCommand,
            RenameCommand,
            ShowFilePropertiesCommand,
            CopyFileCommand,
            CopyFilePathCommand,
            CopyImageCommand,
            CopyBase64Command,
            DuplicateFileCommand,
            CutFileCommand,
            PasteCommand,
            ChangeBackgroundCommand,
            SideBySideCommand,
            ReloadCommand,
            ResizeImageCommand,
            CropCommand,
            FlipCommand,
            OptimizeImageCommand,
            SlideshowCommand,
            ColorPickerCommand,
            SortFilesByNameCommand,
            SortFilesByCreationTimeCommand,
            SortFilesByLastAccessTimeCommand,
            SortFilesByLastWriteTimeCommand,
            SortFilesBySizeCommand,
            SortFilesByExtensionCommand,
            SortFilesRandomlyCommand,
            SortFilesAscendingCommand,
            SortFilesDescendingCommand,
            Set0StarCommand,
            Set1StarCommand,
            Set2StarCommand,
            Set3StarCommand,
            Set4StarCommand,
            Set5StarCommand,
            SetAsWallpaperCommand,
            SetAsWallpaperTiledCommand,
            SetAsWallpaperCenteredCommand,
            SetAsWallpaperStretchedCommand,
            SetAsWallpaperFittedCommand,
            SetAsWallpaperFilledCommand,
            SetAsLockscreenCenteredCommand,
            SetAsLockScreenCommand,
            NewTabCommand,
            CloseTabCommand,
            ResetSettingsCommand,
            RestartCommand,
            ShowSettingsFileCommand,
            ShowKeybindingsFileCommand,
            ShowRecentHistoryFileCommand,
            ToggleOpeningInSameWindowCommand,
            ToggleFileHistoryCommand,
            ToggleShowFullPathInTitleBarCommand,
            IsBottomToolbarShown,
            IsAutoFit,
            IsSideBySide,
            IsFullscreen,
            IsScrollingEnabled,
            IsZoomedToFit
        );
        GC.SuppressFinalize(this);
    }
}
