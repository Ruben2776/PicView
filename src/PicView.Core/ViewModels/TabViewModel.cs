using System.Diagnostics;
using PicView.Core.ArchiveHandling;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Preloading;
using PicView.Core.Titles;
using PicView.Core.Generators;
using PicView.Core.ImageDecoding;
using PicView.Core.IPlatform;
using PicView.Core.Sizing;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// Represents the state, data, and context for a single navigation tab.
/// <para>
/// This class acts as the holder for the tab's specific <see cref="ImageIterator"/>, the current 
/// <see cref="ImageModel"/>, and the visual properties (Title, Tooltip). It manages the 
/// lifecycle of resources specific to this tab instance.
/// </para>
/// </summary>
public class TabViewModel(Action<TabViewModel> closeTab, MainWindowViewModel parentWindowContext, IFileWatcherService? fileWatcherService = null) : IDisposable
{
    #region Tab logic
    
    /// The MainWindowViewModel that currently "owns" this tab
    public MainWindowViewModel ParentWindowContext { get; set; } = parentWindowContext;

    /// Unique identifier for this tab.
    public uint Id { get; } = TabIDGenerator.GetNextId();

    // ReSharper disable once MemberCanBeMadeStatic.Global
#pragma warning disable CA1822
    public double TabHeight => SizeDefaults.TabHeight;
#pragma warning restore CA1822
    
    public CompositeDisposable Disposables { get; } = new();
    public bool IsInitialized { get; set; }
    
    public bool IsClosing { get; private set; }
    public bool IsSelected { get; set; }
    
    public SingleImageType SingleImageType { get; set; }
    public string? SourceURL { get; set; }
    
    #endregion

    #region UI view models
    
    public BindableReactiveProperty<object?> CurrentView { get; } = new(null);
    public HoverbarViewModel Hoverbar { get; } = new();
    public GalleryViewModel Gallery { get; } = new();
    
    public CropViewModel? Crop { get; set; }
    public ICropService? CropService { get; set; }
    
    #endregion
    
    #region Image properties
    public ImageModel Model { get; set; } = new();
    public BindableReactiveProperty<object?> Image { get; } = new(null);
    public BindableReactiveProperty<ImageType?> ImageType { get; } = new(null);
    public BindableReactiveProperty<FileInfo?> FileInfo { get; } = new(null);
    public ImageModel? SecondaryModel { get; set; } = new();
    public BindableReactiveProperty<object?> SecondaryImage { get; } = new(null);
    public BindableReactiveProperty<ImageType?> SecondaryImageType { get; } = new(null);
    public BindableReactiveProperty<FileInfo?> SecondaryFileInfo { get; } = new(null);
    public BindableReactiveProperty<int> RotationAngle { get; } = new(0);
    public BindableReactiveProperty<bool> IsRotated0 { get; } = new(true);
    public BindableReactiveProperty<bool> IsRotated90 { get; } = new(false);
    public BindableReactiveProperty<bool> IsRotated180 { get; } = new(false);
    public BindableReactiveProperty<bool> IsRotated270 { get; } = new(false);
    public BindableReactiveProperty<bool> ShouldCropBeEnabled { get; } = new(false);
    public BindableReactiveProperty<bool> ShouldOptimizeImageBeEnabled { get; } = new(false);
    public BindableReactiveProperty<double> ScaleX { get; } = new(1);
    public BindableReactiveProperty<double> InitialZoom { get; } = new(1);
    public BindableReactiveProperty<int> ZoomLevel { get; } = new();

    #endregion

    #region Navigation properties
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.IImageIterator"/>>
    public IImageIterator? ImageIterator { get; private set; }
    public ArchiveExtractionService? ArchiveExtractionService { get; } = new();
    public BindableReactiveProperty<bool> IsArchiveExtracting { get; } = new(false);
    public BindableReactiveProperty<string?> ArchiveExtractionProgressText { get; } = new(null);
    public IThumbnailCache? ThumbnailCache { get; private set; }

    private IFileWatcherService? _fileWatcherService = fileWatcherService;
    
    public BindableReactiveProperty<int> NavigationIndex { get; } = new(0);
    public BindableReactiveProperty<int> MaxIndex { get; } = new(0);
    public BindableReactiveProperty<bool> CanNavigateForwards { get; } = new();
    public BindableReactiveProperty<bool> CanNavigateBackwards { get; } = new();
    public bool IsFileWatcherNavigationEnabled { get; set; } = true;
    
    /// <summary>
    /// Should be used when changing directory or closing the tab
    /// </summary>
    private CancellationTokenSource NavigationCts { get; set; } = new();
    
    #endregion

    #region Titles

    /// <summary>
    /// The main title displayed in the window title bar.
    /// </summary>
    public BindableReactiveProperty<string>? Title { get; } = new();
    /// <summary>
    /// The tooltip displayed when hovering over the title.
    /// </summary>
    public BindableReactiveProperty<string>? TitleTooltip { get; } = new();
    /// <summary>
    /// The title displayed in the taskbar or task manager.
    /// </summary>
    public BindableReactiveProperty<string>? WindowTitle { get; } = new();
    /// <summary>
    /// The title displayed in the tab.
    /// </summary>
    public BindableReactiveProperty<string> TabTitle { get; } = new(string.Empty, StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The tooltip displayed when hovering over the tab.
    /// </summary>
    public BindableReactiveProperty<string> TabTooltip { get; } = new(string.Empty, StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Updates the window title and tab title based on the current image model.
    /// </summary>
    public void UpdateTabTitle()
    {
        if (!IsSelected)
        {
            return;
        }
        
        var width = Model.PixelWidth;
        var height = Model.PixelHeight;
        
        if (ImageIterator is null || ImageIterator.CurrentIndex is -1 || SingleImageType is not SingleImageType.None)
        {
            if (Image.CurrentValue is null)
            {
                SetNewTabTitle();
            }
            else
            {
                SetSingleTitle();
                // TabTitle/TabTooltip are not touched by SetSingleTitle
                AppendMotionPhotoMarkerIfNeeded(includeTabTitles: false);
            }

            return;
        }
        
        var index = ImageIterator.CurrentIndex;
        var windowTitles = GetTitles();
        if (Settings.UIProperties.ShowFullPathInTitleBar)
        {
            Title.Value = windowTitles.FilePathTitle;
        }
        else
        {
            Title.Value = windowTitles.BaseTitle;
        }
        WindowTitle.Value = windowTitles.TitleWithAppName;

        TitleTooltip.Value = windowTitles.FilePathTitle;
        if (Settings.ImageScaling.ShowImageSideBySide && SecondaryModel is not null)
        {
            TabTitle.Value = StringExtensions.CombineWithSeparator(Model.FileInfo.Name, SecondaryModel.FileInfo.Name);
            TabTooltip.Value = StringExtensions.CombineWithSeparator(Model.FileInfo.FullName, SecondaryModel.FileInfo.FullName);
        }
        else
        {
            TabTitle.Value = Model.FileInfo.Name;
            TabTooltip.Value = Model.FileInfo.FullName;
        }

        AppendMotionPhotoMarkerIfNeeded(includeTabTitles: true);
        
        return;
        
        WindowTitles GetTitles()
        {
            var zoom = ZoomLevel.CurrentValue;
            var firstInfo = new ImageTitleInfo(Model.FileInfo, width, height, index, ImageIterator.Files.Count);
            if (Model.TiffNavigation is { } tiff)
            {
                return ImageTitleFormatter.GenerateTiffTitleStrings(firstInfo, zoom, tiff.CurrentPage, tiff.PageCount);
            }

            if (Settings.ImageScaling.ShowImageSideBySide && SecondaryModel is not null)
            {
                var secondIndex = ImageIterator.SecondaryCurrentIndex;
                var secondInfo = new ImageTitleInfo(SecondaryModel.FileInfo, SecondaryModel.PixelWidth, SecondaryModel.PixelHeight, secondIndex, ImageIterator.Files.Count);
                return ImageTitleFormatter.GenerateTitleForSideBySide(firstInfo, secondInfo, zoom, ImageIterator.Files);
            }

            return ImageTitleFormatter.GenerateTitleStrings(firstInfo, zoom);
        }
        
        void SetSingleTitle()
        {
            string nameTitle;
            switch (SingleImageType)
            {
                case SingleImageType.Clipboard:
                    Debug.Assert(TranslationManager.Translation != null);
                    Debug.Assert(TranslationManager.Translation.ClipboardImage != null);
                    nameTitle = TranslationManager.Translation.ClipboardImage;
                    break;
                case SingleImageType.Url:
                    nameTitle = SourceURL ?? string.Empty;
                    break;
                default:
                    nameTitle = Model?.FileInfo?.Name ?? string.Empty;
                    break;
            }
            var zoom = ZoomLevel.CurrentValue;
            var singleTitles = ImageTitleFormatter.GenerateTitleForSingleImage(width, height, nameTitle, zoom);
            Title.Value = singleTitles.BaseTitle;
            WindowTitle.Value = singleTitles.TitleWithAppName;
            TitleTooltip.Value = singleTitles.FilePathTitle;
        }

        // Appends a localized " (Motion Photo)" suffix to the computed titles when the
        // current image (or the secondary one in side-by-side mode) carries a video.
        void AppendMotionPhotoMarkerIfNeeded(bool includeTabTitles)
        {
            if (Model?.MotionPhoto is null && SecondaryModel?.MotionPhoto is null)
            {
                return;
            }

            var marker = TranslationManager.Translation.MotionPhoto;
            if (string.IsNullOrEmpty(marker))
            {
                return;
            }

            var suffix = $" ({marker})";
            Title.Value += suffix;
            WindowTitle.Value += suffix;
            TitleTooltip.Value += suffix;
            if (includeTabTitles)
            {
                TabTitle.Value += suffix;
                TabTooltip.Value += suffix;
            }
        }

    }
    
    public void SetNewTabTitle()
    {
        var title = TranslationManager.Translation.NoImage;
        if (string.IsNullOrEmpty(title))
        {
            return;
        }
        WindowTitle.Value = StringExtensions.CombineWithAppName(title);
        Title.Value = title;
        TitleTooltip.Value = title;
        TabTitle.Value = title;
     }
    
    public void SetLoading()
    {
        WindowTitle.Value = StringExtensions.CombineWithAppName(TranslationManager.Translation.Loading);
        Title.Value = TranslationManager.Translation.Loading;
        TitleTooltip.Value = TranslationManager.Translation.Loading;
        TabTitle.Value = TranslationManager.Translation.Loading;
    }
    
    #endregion

    #region Initialization

    public void Initialize(IImageCache cache, IThumbnailCache thumbCache, IThumbnailLoader thumbnailLoader, IFileWatcherService? fileWatcherService = null, IThumbnailCache? thumbnailCache = null)
    {
        if (fileWatcherService != null)
        {
            _fileWatcherService = fileWatcherService;
        }
        if (thumbnailCache != null)
        {
            ThumbnailCache = thumbnailCache;
        }
        ImageIterator = new ImageIterator(cache, thumbCache, thumbnailLoader, this);
    }

    public void InitializeImageIterator(IReadOnlyList<FileInfo> files, IImageCache cache, IThumbnailCache thumbCache,  IThumbnailLoader thumbnailLoader, IFileWatcherService? fileWatcherService = null, IThumbnailCache? thumbnailCache = null)
    {
        if (fileWatcherService != null)
        {
            _fileWatcherService = fileWatcherService;
        }
        if (thumbnailCache != null)
        {
            ThumbnailCache = thumbnailCache;
        }
        ImageIterator ??= new ImageIterator(cache, thumbCache, thumbnailLoader, this);
        if (Model?.FileInfo is null)
        {
#if DEBUG
            DebugHelper.LogDebug(nameof(TabViewModel), nameof(InitializeImageIterator), $"Model.FileInfo is null for tab {Id}");
#endif
            return;
        }
        var index = files.FindIndex(x => x.FullName.Equals(Model.FileInfo.FullName));
        ImageIterator.Initialize(files, index);

        if (index > -1 && index < files.Count)
        {
            cache.TryAdd(Id, index, new PreLoadValue(Model), files.Count, false, out _);
        }
        
        var directory = files.Count > 0 ? files[0].DirectoryName : null;
        _fileWatcherService?.Watch(this, directory);
    }
    
    #endregion

    public async Task Next()
    {
        if (!CanNavigateForwards.CurrentValue)
        {
            return;
        }
        await ImageIterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, NavigationCts).ConfigureAwait(false);
    }

    public async Task Prev()
    {
        if (!CanNavigateBackwards.CurrentValue)
        {
            return;
        }
        await ImageIterator.NavigateAsync(NavigateTo.Previous, SkipAmount.One, NavigationCts).ConfigureAwait(false);
    }

    public void CloseTab()
    {
        IsClosing = true; // Signal it to be removed from the UI
        closeTab(this);
        // Dispose has already been called in the CloseTab method
    }

    public CancellationTokenSource GetTabCancellation()
    {
        if ( !NavigationCts.IsCancellationRequested)
        {
            return NavigationCts;
        }

        var oldCts = NavigationCts;
        NavigationCts = new CancellationTokenSource();
        oldCts.Dispose();
        return NavigationCts;
    }
    
    public void ResetNavigationCts()
    {
        NavigationCts?.Cancel();
        NavigationCts?.Dispose();
        NavigationCts = new CancellationTokenSource();
    }

    public void DisposeImageIterator()
    {
        ImageIterator?.Dispose();
        ImageIterator = null;
        FileInfo.Value = null;
        SecondaryFileInfo.Value = null;
        SecondaryModel = null;
        CanNavigateBackwards.Value = false;
        CanNavigateForwards.Value = false;
    }
    
    public void Dispose()
    {
        _fileWatcherService?.Unwatch(this);
        ThumbnailCache?.RemoveOwner(Id);

        ImageIterator?.Dispose();
        ArchiveExtractionService.Cleanup();

        NavigationCts.Dispose();
        Disposables.Dispose();
        
        GC.SuppressFinalize(this);
    }

}
