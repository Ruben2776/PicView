using System.Diagnostics;
using PicView.Core.ArchiveHandling;
using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.FileHistory;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Navigation.Tiff;
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
public class TabViewModel(MainWindowViewModel parentWindowContext, IFileWatcherService? fileWatcherService = null,
    IArchiveEntryWriter? archiveEntryWriter = null) : IDisposable
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
    
    /// <summary>
    /// What the owning Main Window does when this tab changes, while it is attached there. Only the window's
    /// <see cref="TabOverviewViewModel"/> sets it.
    /// </summary>
    internal IDisposable? Hookups { get; set; }

    /// <summary>
    /// Whether the tab has been opened, which attaches it to its window. A tab that never browsed files is not.
    /// </summary>
    public bool IsInitialized { get; internal set; }

    /// <summary>
    /// Whether this is a Fresh Tab: one that has never browsed files, or whose files were all deleted.
    /// </summary>
    public bool IsFresh => !IsInitialized || _hasLostItsFiles;

    private bool _hasLostItsFiles;
    
    public bool IsClosing { get; private set; }
    public bool IsSelected { get; set; }
    
    /// <summary>
    /// What the tab shows instead of a file it browses. A Single Image hides the gallery without changing its mode.
    /// </summary>
    public SingleImageType SingleImageType
    {
        get => _singleImageType;
        set
        {
            _singleImageType = value;
            UpdateGalleryHidden();
        }
    }

    private SingleImageType _singleImageType;

    public string? SourceURL { get; set; }

    /// <summary>
    /// What this tab has shown, which Back and Forward step through. The tab records into it each time it shows an image.
    /// </summary>
    public ViewHistory ViewHistory { get; } = new();

    #endregion

    #region UI view models
    
    public BindableReactiveProperty<object?> CurrentView { get; } = new(null);
    public HoverbarViewModel Hoverbar { get; } = new();
    public GalleryViewModel Gallery { get; } = new();
    
    /// <summary>
    /// The crop the tab is in, if any. Cropping hides the gallery without changing its mode.
    /// </summary>
    public CropViewModel? Crop
    {
        get => _crop;
        set
        {
            _crop = value;
            UpdateGalleryHidden();
        }
    }

    private CropViewModel? _crop;

    public ICropService? CropService { get; set; }

    private void UpdateGalleryHidden() =>
        Gallery.Switcher.SetHidden(_crop is not null || _singleImageType is not SingleImageType.None);
    
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
    /// What changed in what the tab shows: once per <see cref="Display"/>, <see cref="DisplaySingleImage"/> and
    /// <see cref="ShowPage"/>, after the models, their bindables and the View History hold the new values, and once per
    /// shown file that changed on disk. It fires on the thread that showed the image, so a subscriber that touches the
    /// UI hops to its thread itself. This is the one change the UI reacts to.
    /// </summary>
    public Observable<ShownChange> Shown => _shown;

    private readonly Subject<ShownChange> _shown = new();

    /// <summary>
    /// Shows <paramref name="primary"/>, and <paramref name="secondary"/> beside it when viewing side by side.
    /// Without a secondary, any previous one is cleared. This is the one way to change the shown models: it sets
    /// <see cref="SecondaryModel"/> before <see cref="Model"/>, because the UI reacts to <see cref="Model"/> changing
    /// and reads the secondary model then, and sets the bindable properties so they never lag behind the models.
    /// Showing models from files means the tab no longer shows a single image, so <see cref="SingleImageType"/> is
    /// reset. A page asked for with <see cref="RequestPage"/> is set on the primary model first, when it is that file.
    /// Once shown, the primary model is recorded in <see cref="ViewHistory"/> and File History, and becomes the
    /// <see cref="LastFile"/> when this is the active tab of the Active Main Window.
    /// </summary>
    public void Display(ImageModel primary, ImageModel? secondary = null)
    {
        SingleImageType = SingleImageType.None;
        ApplyRequestedPage(primary);
        ShowModels(primary, secondary);
        RecordShown();
        _shown.OnNext(ShownChange.Image);
    }

    /// <summary>
    /// Shows a single image that is not browsed with the tab's files, such as a clipboard image, in its own
    /// <paramref name="model"/>. Sets <see cref="SingleImageType"/> before the model, because the UI reads it when
    /// the model changes. Once shown, it is recorded in <see cref="ViewHistory"/> with <paramref name="title"/>, which
    /// shows it again later.
    /// </summary>
    public void DisplaySingleImage(ImageModel model, SingleImageType type, string? title = null)
    {
        SingleImageType = type;
        _singleImageTitle = title;
        ShowModels(model, null);
        RecordShown();
        _shown.OnNext(ShownChange.Image);
    }

    // The title of the Single Image the tab shows, which its View History entry keeps
    private string? _singleImageTitle;

    /// <summary>
    /// Turns the multi-page TIFF the tab shows to <paramref name="page"/>, and records it. Does nothing when the tab shows
    /// no multi-page TIFF, or the TIFF has no such page. The window picks the page up from the model's image.
    /// </summary>
    public void ShowPage(int page)
    {
        // A load may show another model meanwhile, so the page goes to the model it was checked against
        var model = Model;
        if (model.TiffNavigation is not { } tiff || page < 0 || page >= tiff.PageCount)
        {
            return;
        }

        SetPage(model, tiff, page);
        // The page is shown only while that model is still the shown one
        if (ReferenceEquals(model, Model))
        {
            Image.Value = model.Image;
        }
        RecordShown();
        _shown.OnNext(ShownChange.Image);
    }

    /// <summary>
    /// Asks the next <see cref="Display"/> to show <paramref name="filePath"/> at <paramref name="page"/>, so opening a
    /// TIFF at a page never shows another page first, or records it. That Display takes the request whichever file it
    /// shows, and a newer request replaces this one.
    /// </summary>
    public void RequestPage(string filePath, int page) =>
        Interlocked.Exchange(ref _pageRequest, new PageRequest(filePath, page));

    private PageRequest? _pageRequest;

    private sealed record PageRequest(string FilePath, int Page);

    private void ApplyRequestedPage(ImageModel primary)
    {
        // Taken whether or not it fits, so a request that goes unused can't turn a page when the file shows much later
        if (Interlocked.Exchange(ref _pageRequest, null) is not { } request)
        {
            return;
        }

        if (primary.TiffNavigation is { } tiff
            && primary.FileInfo is { } file
            && string.Equals(file.FullName, request.FilePath, StringComparison.OrdinalIgnoreCase)
            && request.Page >= 0 && request.Page < tiff.PageCount)
        {
            SetPage(primary, tiff, request.Page);
        }
    }

    private static void SetPage(ImageModel model, TiffNavigationInfo tiff, int page)
    {
        tiff.CurrentPage = page;
        if (tiff.Pages is { } pages && page < pages.Length)
        {
            model.Image = pages[page];
        }
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
    /// Records what the tab shows now in its <see cref="ViewHistory"/>, and a file, an archive or a URL in File History
    /// and <see cref="LastFile"/>. Each page of a multi-page TIFF or of an archive is its own entry in the View History.
    /// It runs after the image is shown, so recording never delays it: it reads no disk, never waits for the UI thread
    /// and allocates nothing for a file.
    /// </summary>
    private void RecordShown()
    {
        // Read once, as a load on another thread can show another model meanwhile, and its page must not be recorded
        // with this one's path
        var model = Model;
        var type = SingleImageType;
        ViewHistoryEntry entry;
        if (type is not SingleImageType.None)
        {
            entry = ViewHistoryEntry.ForSingleImage(model, type, _singleImageTitle,
                type is SingleImageType.Url ? SourceURL : null);
        }
        else if (model.FileInfo is not { } file)
        {
            // An empty model, as when the tab becomes fresh again
            return;
        }
        else if (Archives.ArchivePageOf(file.FullName) is { } archivePage)
        {
            // Leaving an archive shows the next file before its extraction is cleaned up, so only a file in an
            // extraction folder is an archive page. Its folder tells whose, as another archive may be opening meanwhile
            entry = ViewHistoryEntry.ForArchivePage(archivePage.Archive, archivePage.Entry);
        }
        else
        {
            // Only a multi-page TIFF has pages of its own
            var page = model.TiffNavigation is { PageCount: > 1 } tiff ? tiff.CurrentPage : -1;
            entry = ViewHistoryEntry.ForFile(file.FullName, page);
        }

        if (ViewHistory.Record(entry) && entry.HistoryPath is { } historyPath)
        {
            FileHistoryManager.Add(historyPath);
            LastFile.Record(this, historyPath);
        }
    }

    /// <summary>
    /// Whether <paramref name="path"/> is a Shown File: the file of the shown image, or of the one beside it when
    /// viewing side by side.
    /// </summary>
    public bool IsShownFile(string path) =>
        IsFileAt(Model, path) || IsFileAt(SecondaryModel, path);

    /// <summary>
    /// Follows the Shown File at <paramref name="path"/> to <paramref name="file"/>, as when it is renamed, overwritten
    /// or changed on disk: its model's file and, when given, its pixel size, the bindable that shows the file, and the
    /// title. The shown images are unchanged, nothing is recorded, and <see cref="Shown"/> fires a
    /// <see cref="ShownChange.File"/>.
    /// </summary>
    /// <returns>Whether <paramref name="path"/> was a Shown File. Nothing changes when it was not.</returns>
    public bool UpdateShownFile(string path, FileInfo file, uint? pixelWidth = null, uint? pixelHeight = null)
    {
        // Read once, as a load on another thread can show other models meanwhile
        var primary = Model;
        var secondary = SecondaryModel;
        if (IsFileAt(primary, path))
        {
            Follow(primary);
            FileInfo.Value = file;
        }
        else if (IsFileAt(secondary, path))
        {
            Follow(secondary!);
            SecondaryFileInfo.Value = file;
        }
        else
        {
            return false;
        }

        UpdateTabTitle();
        _shown.OnNext(ShownChange.File);
        return true;

        void Follow(ImageModel model)
        {
            model.FileInfo = file;
            if (pixelWidth is { } width)
            {
                model.PixelWidth = width;
            }
            if (pixelHeight is { } height)
            {
                model.PixelHeight = height;
            }
        }
    }

    private static bool IsFileAt(ImageModel? model, string path) =>
        model?.FileInfo is { } file && string.Equals(file.FullName, path, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Where the archive page <paramref name="entry"/> is in the tab's files, so a step Back or Forward moves to it
    /// there: the tab is still in that archive, and the page is extracted and listed. Null when the archive has to open
    /// again at the page. For an archive that local software extracted whole it checks the disk, so call it off the UI
    /// thread.
    /// </summary>
    public int? IndexOfArchivePage(in ViewHistoryEntry entry)
    {
        if (entry is not { Kind: ViewHistoryKind.ArchivePage, Path: { } archive, ArchiveEntry: { } archiveEntry }
            || Archives.OpenArchive is not { } openArchive
            || !string.Equals(openArchive, archive, StringComparison.OrdinalIgnoreCase)
            || Archives.ExtractedPathOf(archiveEntry) is not { } extracted
            || ImageIterator is not { } iterator)
        {
            return null;
        }

        return SideBySidePair.IndexOf(iterator.Files, new FileInfo(extracted));
    }

    /// <summary>
    /// The tab's Quarter Turn and flip.
    /// </summary>
    public TabOrientation Orientation { get; } = new(parentWindowContext?.Translation);
    public BindableReactiveProperty<bool> ShouldCropBeEnabled { get; } = new(false);
    public BindableReactiveProperty<bool> ShouldOptimizeImageBeEnabled { get; } = new(false);
    public BindableReactiveProperty<bool> ShouldPrintBeEnabled { get; } = new(true);
    public BindableReactiveProperty<double> InitialZoom { get; } = new(1);
    public BindableReactiveProperty<int> ZoomLevel { get; } = new();

    #endregion

    #region Navigation properties
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.IImageIterator"/>>
    public IImageIterator? ImageIterator { get; private set; }
    private ArchiveOpenings? _archives;

    /// <summary>
    /// The tab's Archive Openings: the archives it has opened, the one it reads, and where their extraction stands
    /// (ADR-0013).
    /// </summary>
    public ArchiveOpenings Archives
    {
        get
        {
            if (_archives is null)
            {
                Interlocked.CompareExchange(ref _archives, new ArchiveOpenings(this, archiveEntryWriter), null);
            }

            return _archives;
        }
    }
    public IThumbnailCache? ThumbnailCache { get; private set; }

    private IFileWatcherService? _fileWatcherService = fileWatcherService;
    
    public BindableReactiveProperty<int> NavigationIndex { get; } = new(0);
    public BindableReactiveProperty<int> MaxIndex { get; } = new(0);
    public BindableReactiveProperty<bool> CanNavigateForwards { get; } = new();
    public BindableReactiveProperty<bool> CanNavigateBackwards { get; } = new();
    
    /// <summary>
    /// Should be used when changing directory or closing the tab
    /// </summary>
    private CancellationTokenSource NavigationCts { get; set; } = new();

    private int _loadVersion;

    /// <summary>
    /// Starts loading a new source into the tab, which supersedes any load still running.
    /// </summary>
    /// <returns>The load's version, to check with <see cref="IsLatestLoad"/>.</returns>
    internal int BeginLoad() => Interlocked.Increment(ref _loadVersion);

    /// <summary>
    /// Whether no load began after the one with <paramref name="version"/>, so it may still change the tab's image,
    /// files and gallery.
    /// </summary>
    internal bool IsLatestLoad(int version) => Volatile.Read(ref _loadVersion) == version;

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
            else if (ImageIterator is not null || SingleImageType is not SingleImageType.None)
            {
                SetSingleTitle();
            }
            // Else a file whose navigation has not started yet, as at startup: it sets the indexed title when it does

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
                    nameTitle = _singleImageTitle ?? Model?.FileInfo?.FullName ?? TabTitle.CurrentValue ?? string.Empty;
                    break;
            }
            var zoom = ZoomLevel.CurrentValue;
            var singleTitles = ImageTitleFormatter.GenerateTitleForSingleImage(width, height, nameTitle, zoom);
            Title.Value = singleTitles.BaseTitle;
            WindowTitle.Value = singleTitles.TitleWithAppName;
            TitleTooltip.Value = singleTitles.FilePathTitle;
            TabTitle.Value = nameTitle;
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

    /// <summary>
    /// Shows <paramref name="image"/> in place of the shown model's image, as a Stand-in: a thumbnail while the next
    /// image loads, or an Effects Render. What the tab shows is unchanged: neither model changes, nothing is recorded
    /// and <see cref="Shown"/> does not fire.
    /// </summary>
    public void ShowStandIn(object? image) => Image.Value = image;

    /// <summary>
    /// Shows <paramref name="thumbnail"/> as the Stand-in while the next image loads, with the Loading title, and clears
    /// the secondary image, which is already clear when nothing is shown beside the image.
    /// </summary>
    public void ShowLoading(object? thumbnail)
    {
        SetLoading();
        ShowStandIn(thumbnail);
        SecondaryImage.Value = null;
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
        GalleryCoordinator?.Dispose();
        GalleryCoordinator = new Gallery.GalleryCoordinator(this, thumbnailLoader, ThumbnailCache ?? thumbCache);
        ImageIterator = new ImageIterator(cache, thumbCache, thumbnailLoader, this, GalleryCoordinator.FilesChanged);
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
        GalleryCoordinator ??= new Gallery.GalleryCoordinator(this, thumbnailLoader, ThumbnailCache ?? thumbCache);
        ImageIterator ??= new ImageIterator(cache, thumbCache, thumbnailLoader, this, GalleryCoordinator.FilesChanged);
        // Browsing files again, so it is no longer fresh
        _hasLostItsFiles = false;
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
    /// Follows a file event in a folder this tab browses: its file list, the file it shows, its title and its gallery.
    /// </summary>
    public ValueTask OnFolderChangedAsync(FolderChange change, CancellationToken ct = default) =>
        (_folderChanges ??= new FolderChanges(this)).HandleAsync(change, ct);

    private FolderChanges? _folderChanges;

    /// <summary>
    /// Makes the tab fresh again once all the files it browsed were deleted: it stops following their folder, forgets
    /// its files, the image it shows and its gallery, and shows the start-up menu.
    /// </summary>
    internal async ValueTask BecomeFreshAsync()
    {
        _fileWatcherService?.Unwatch(this);
        ResetNavigationCts();
        ThumbnailCache?.RemoveOwner(Id);
        DisposeImageIterator();
        Display(new ImageModel());
        SetNewTabTitle();
        _hasLostItsFiles = true;

        if (ParentWindowContext?.ActionRunner is { } actionRunner)
        {
            await actionRunner.ShowStartUpMenu(this).ConfigureAwait(false);
        }
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
        Archives.Cleanup();
        GalleryCoordinator?.Dispose();

        NavigationCts.Dispose();
        Orientation.Dispose();
        Hookups?.Dispose();
        _shown.Dispose();
        
        GC.SuppressFinalize(this);
    }

}
