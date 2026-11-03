using PicView.Core.ArchiveHandling;
using PicView.Core.DebugTools;
using PicView.Core.FileHistory;
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
public class TabViewModel(MainWindowViewModel parentWindowContext, TabServices? services = null,
    IArchiveEntryWriter? archiveEntryWriter = null) : IDisposable
{
    #region Tab logic

    /// <summary>
    /// The services this tab was created with, which are the same for every tab. Null for a tab created before they
    /// were composed.
    /// </summary>
    internal TabServices? Services { get; } = services;

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

    /// <summary>
    /// The tab's Titles, derived from what it shows when they are read (ADR-0015). Made on first use, since they
    /// subscribe to the tab's bindables.
    /// </summary>
    public TabTitles Titles
    {
        get
        {
            if (_titles is null)
            {
                LazyInitializer.EnsureInitialized(ref _titles, () => new TabTitles(this));
            }

            return _titles;
        }
    }

    private TabTitles? _titles;

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
            // Cropping replaces the titlebar text until the crop closes, and turns Next and Prev off
            Titles.Mark();
            DeriveCanNavigate();
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

    // A load that ends after the tab closed shows into a disposed tab, which stays harmless
    private void Publish(ShownChange change)
    {
        if (_shown.IsDisposed)
        {
            return;
        }

        // The factor reset rides the shown mark, so a bound tab builds its titles once per image (ADR-0016)
        if (change is ShownChange.Image && Settings.Zoom.ResetZoomOnChange)
        {
            Zoom.ResetFactor();
        }

        // The titles are marked after the image bindable is set and before the UI reacts
        Titles.OnShown(change);
        _shown.OnNext(change);
    }

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
        Publish(ShownChange.Image);
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
        Publish(ShownChange.Image);
    }

    // The title of the Single Image the tab shows, which its View History entry keeps
    private string? _singleImageTitle;

    /// <summary>The title <see cref="DisplaySingleImage"/> was given, which the Titles name a Single Image by.</summary>
    internal string? SingleImageTitle => _singleImageTitle;

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
        Publish(ShownChange.Image);
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

        Publish(ShownChange.File);
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
    /// <summary>
    /// The tab's Zoom Level and its two inputs, the Fit Scale and the Zoom Factor (ADR-0016).
    /// </summary>
    public TabZoom Zoom { get; } = new();

    #endregion

    #region Navigation properties
    
    /// <inheritdoc cref="Core.Navigation.Interfaces.IImageIterator"/>>
    public IImageIterator? ImageIterator { get; private set; }

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
                LazyInitializer.EnsureInitialized(ref _archives, () =>
                {
                    var openings = new ArchiveOpenings(this, archiveEntryWriter);
                    // Replays the current state at once, while _archives is still null: that first derive reads "not
                    // extracting", which the initial state is anyway
                    _archiveStateSubscription = openings.State.AsObservable().Subscribe(_ => DeriveCanNavigate());
                    return openings;
                });
            }

            return _archives;
        }
    }

    private ArchiveOpenings? _archives;
    private IDisposable? _archiveStateSubscription;

    /// <summary>
    /// The tab's Load, the one owner of every load of a source into it (ADR-0019), made on first use from the services
    /// the tab was created with.
    /// </summary>
    /// <exception cref="InvalidOperationException">The tab was created without services.</exception>
    public TabLoad Load
    {
        get
        {
            if (_load is null)
            {
                var services = Services ?? throw new InvalidOperationException("The tab has no services");
                LazyInitializer.EnsureInitialized(ref _load, () => new TabLoad(this, services));
            }

            return _load;
        }
    }

    private TabLoad? _load;
    public IThumbnailCache? ThumbnailCache { get; private set; }

    private IFileWatcherService? _fileWatcherService = services?.Watcher;
    
    public BindableReactiveProperty<int> NavigationIndex { get; } = new(0);
    public BindableReactiveProperty<int> MaxIndex { get; } = new(0);
    /// <summary>Whether Next runs, derived by the tab (ADR-0017). Nothing outside the tab writes it.</summary>
    public IReadOnlyBindableReactiveProperty<bool> CanNavigateForwards => _canNavigateForwards;

    /// <summary>Whether Prev runs, derived by the tab (ADR-0017). Nothing outside the tab writes it.</summary>
    public IReadOnlyBindableReactiveProperty<bool> CanNavigateBackwards => _canNavigateBackwards;

    private readonly BindableReactiveProperty<bool> _canNavigateForwards = new();
    private readonly BindableReactiveProperty<bool> _canNavigateBackwards = new();

    /// <summary>
    /// The iterator's publication of where it is: the shown file's index and the count of the tab's files, set
    /// together so the derived flags never see one without the other.
    /// </summary>
    internal void SetPosition(int index, int count)
    {
        NavigationIndex.Value = index;
        MaxIndex.Value = count;
        DeriveCanNavigate();
    }

    /// <summary>
    /// The Looping and side-by-side settings have no change notification; their toggles call this for the active tab.
    /// </summary>
    public void RefreshCanNavigate() => DeriveCanNavigate();

    /// <summary>
    /// Whether Next and Prev run, derived from the tab's own state (ADR-0017): no iterator or a crop means neither; a
    /// list of one and nothing extracting means neither; else forwards needs an image ahead (a pair when side-by-side
    /// is on), or looping, or an extraction still listing pages; backwards needs an image behind, or looping.
    /// </summary>
    private void DeriveCanNavigate()
    {
        bool forwards;
        bool backwards;
        if (ImageIterator is null || Crop is not null)
        {
            forwards = false;
            backwards = false;
        }
        else
        {
            var index = NavigationIndex.CurrentValue;
            var count = MaxIndex.CurrentValue;
            // The field, not the property: a derive inside the property's factory must not create it twice
            var extracting = _archives?.State.Value is ExtractionState.Extracting;
            if (count <= 1 && !extracting)
            {
                forwards = false;
                backwards = false;
            }
            else
            {
                forwards = SideBySidePair.CanNavigateForwards(index, count, extracting);
                backwards = Settings.UIProperties.Looping || index > 0;
            }
        }

        _canNavigateForwards.Value = forwards;
        _canNavigateBackwards.Value = backwards;
    }
    
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

    private bool _isDisposed;

    /// <summary>
    /// Whether the tab was disposed, after which no load changes it.
    /// </summary>
    internal bool IsDisposed => Volatile.Read(ref _isDisposed);

    #endregion

    #region Titles

    /// <summary>
    /// Puts the tab in the Loading Title Phase, until the next image shows or <see cref="EndLoading(int)"/>.
    /// </summary>
    /// <returns>The ticket that <see cref="EndLoading(int)"/> takes.</returns>
    public int SetLoading() => Titles.EnterLoading();

    /// <summary>Puts the tab in the Downloading Title Phase with <paramref name="text"/>, such as a progress report.</summary>
    /// <returns>The ticket that <see cref="EndLoading(int)"/> takes.</returns>
    public int SetDownloading(string text) => Titles.Enter(TitlePhase.Downloading, text);

    /// <summary>
    /// Puts the tab in the Error Title Phase with <paramref name="text"/>, until the next load or shown image. An Error
    /// has no end call.
    /// </summary>
    public void SetError(string text) => Titles.Enter(TitlePhase.Error, text);

    /// <summary>
    /// Ends Loading or Downloading when the load that entered it with <paramref name="ticket"/> gave up without showing
    /// anything, unless a newer phase was entered since. An Error stays.
    /// </summary>
    public void EndLoading(int ticket) => Titles.EndLoading(ticket);

    /// <summary>
    /// Ends whatever Loading or Downloading is current, for a tab that becomes fresh. Public only because its one caller
    /// is in PicView.Avalonia, which Core does not expose its internals to; it goes once the Load module owns the phase.
    /// </summary>
    public void EndLoading() => Titles.EndLoading();

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
    /// <returns>The ticket of the Loading it entered, which <see cref="EndLoading(int)"/> takes.</returns>
    public int ShowLoading(object? thumbnail)
    {
        var ticket = SetLoading();
        ShowStandIn(thumbnail);
        SecondaryImage.Value = null;
        return ticket;
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
        DeriveCanNavigate();
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
        DeriveCanNavigate();
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
        if (!CanNavigateForwards.Value)
        {
            return;
        }
        await ImageIterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, NavigationCts).ConfigureAwait(false);
    }

    public async Task Prev()
    {
        if (!CanNavigateBackwards.Value)
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
        // No files, so the position goes too: a new iterator must not light the buttons from the old list's place
        SetPosition(-1, 0);
    }
    
    public void Dispose()
    {
        // First, so a load still running stops at its next check instead of showing into a disposed tab
        Volatile.Write(ref _isDisposed, true);
        _fileWatcherService?.Unwatch(this);
        ThumbnailCache?.RemoveOwner(Id);

        ImageIterator?.Dispose();
        _archiveStateSubscription?.Dispose();
        Archives.Cleanup();
        GalleryCoordinator?.Dispose();

        NavigationCts.Dispose();
        Orientation.Dispose();
        Zoom.Dispose();
        Hookups?.Dispose();
        _titles?.Dispose();
        _shown.Dispose();
        
        GC.SuppressFinalize(this);
    }

}
