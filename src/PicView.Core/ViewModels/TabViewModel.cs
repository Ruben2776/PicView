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
using PicView.Core.ImageTransformations;
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
public class TabViewModel(MainWindowViewModel parentWindowContext, IFileWatcherService? fileWatcherService = null) : IDisposable
{
    #region Tab logic

    /// <summary>
    /// The Main Window that owns this tab. Only that window's <see cref="TabOverviewViewModel"/> changes it, when the
    /// tab moves there.
    /// </summary>
    public MainWindowViewModel ParentWindowContext { get; internal set; } = parentWindowContext;

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
    
    public Gallery.GalleryCoordinator? GalleryCoordinator { get; private set; }
    
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

    /// <summary>
    /// Shows <paramref name="primary"/>, and <paramref name="secondary"/> beside it when viewing side by side.
    /// Without a secondary, any previous one is cleared. This is the one way to change the shown models: it sets
    /// <see cref="SecondaryModel"/> before <see cref="Model"/>, because the UI reacts to <see cref="Model"/> changing
    /// and reads the secondary model then, and sets the bindable properties so they never lag behind the models.
    /// Showing models from files means the tab no longer shows a single image, so <see cref="SingleImageType"/> is
    /// reset.
    /// </summary>
    public void Display(ImageModel primary, ImageModel? secondary = null)
    {
        SingleImageType = SingleImageType.None;
        ShowModels(primary, secondary);
    }

    /// <summary>
    /// Shows a single image that is not browsed with the tab's files, such as a clipboard image, in its own
    /// <paramref name="model"/>. Sets <see cref="SingleImageType"/> before the model, because the UI reads it when
    /// the model changes.
    /// </summary>
    public void DisplaySingleImage(ImageModel model, SingleImageType type)
    {
        SingleImageType = type;
        ShowModels(model, null);
    }

    private void ShowModels(ImageModel primary, ImageModel? secondary)
    {
        SecondaryModel = secondary;
        SecondaryImage.Value = secondary?.Image;
        SecondaryImageType.Value = secondary?.ImageType;
        SecondaryFileInfo.Value = secondary?.FileInfo;

        Model = primary;
        FileInfo.Value = primary.FileInfo;
        Image.Value = primary.Image;
        ImageType.Value = primary.ImageType;
    }
    /// <summary>
    /// The tab's Quarter Turn and flip.
    /// </summary>
    public TabOrientation Orientation { get; } = new(parentWindowContext?.Translation);
    public BindableReactiveProperty<bool> ShouldCropBeEnabled { get; } = new(false);
    public BindableReactiveProperty<bool> ShouldOptimizeImageBeEnabled { get; } = new(false);
    public BindableReactiveProperty<double> InitialZoom { get; } = new(1);
    public BindableReactiveProperty<int> ZoomLevel { get; } = new();

    #endregion

    #region Navigation properties
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.IImageIterator"/>>
    public IImageIterator? ImageIterator { get; private set; }
    public ArchiveExtractionService? ArchiveExtractionService { get; } = new();
    public BindableReactiveProperty<bool> IsArchiveExtracting { get; } = new(false);
    public BindableReactiveProperty<string?> ArchiveExtractionProgressText { get; } = new(null, StringComparer.OrdinalIgnoreCase);
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
    public BindableReactiveProperty<string?>? Title { get; } = new();
    /// <summary>
    /// The tooltip displayed when hovering over the title.
    /// </summary>
    public BindableReactiveProperty<string?>? TitleTooltip { get; } = new();
    /// <summary>
    /// The title displayed in the taskbar or task manager.
    /// </summary>
    public BindableReactiveProperty<string>? WindowTitle { get; } = new();
    /// <summary>
    /// The title displayed in the tab.
    /// </summary>
    public BindableReactiveProperty<string?> TabTitle { get; } = new(string.Empty, StringComparer.OrdinalIgnoreCase);
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
                    nameTitle = Model?.FileInfo?.FullName ?? TabTitle.CurrentValue ?? string.Empty;
                    break;
            }
            var zoom = ZoomLevel.CurrentValue;
            var singleTitles = ImageTitleFormatter.GenerateTitleForSingleImage(width, height, nameTitle, zoom);
            Title.Value = singleTitles.BaseTitle;
            WindowTitle.Value = singleTitles.TitleWithAppName;
            TitleTooltip.Value = singleTitles.FilePathTitle;
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

    public void Initialize(SharedImageCache cache, IThumbnailCache thumbCache, IThumbnailLoader thumbnailLoader, IFileWatcherService? fileWatcherService = null, IThumbnailCache? thumbnailCache = null)
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
        GalleryCoordinator = new Gallery.GalleryCoordinator(this, thumbnailLoader, ThumbnailCache ?? thumbCache);
    }

    public void InitializeImageIterator(IReadOnlyList<FileInfo> files, SharedImageCache cache, IThumbnailCache thumbCache,  IThumbnailLoader thumbnailLoader, IFileWatcherService? fileWatcherService = null, IThumbnailCache? thumbnailCache = null)
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
        GalleryCoordinator ??= new Gallery.GalleryCoordinator(this, thumbnailLoader, ThumbnailCache ?? thumbCache);
        if (Model?.FileInfo is null)
        {
#if DEBUG
            DebugHelper.LogDebug(nameof(TabViewModel), nameof(InitializeImageIterator), $"Model.FileInfo is null for tab {Id}");
#endif
            return;
        }
        var index = SideBySidePair.IndexOf(files, Model.FileInfo) ?? -1;
        ImageIterator.Initialize(files, index);

        if (index > -1 && index < files.Count)
        {
            cache.Put(Id, files[index], Model);
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

    /// <summary>
    /// Closes this tab in the Main Window that owns it now, which also disposes it.
    /// </summary>
    public void CloseTab()
    {
        IsClosing = true; // Signal it to be removed from the UI
        ParentWindowContext.WindowTabs.CloseTab(this);
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
        GalleryCoordinator?.Dispose();

        NavigationCts.Dispose();
        Orientation.Dispose();
        Disposables.Dispose();
        
        GC.SuppressFinalize(this);
    }

}
