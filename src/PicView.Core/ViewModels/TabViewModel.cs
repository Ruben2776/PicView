using PicView.Core.Animation;
using PicView.Core.ArchiveHandling;
using PicView.Core.DebugTools;
using PicView.Core.FileHistory;
using PicView.Core.FileSorting;
using PicView.Core.Gallery;
using PicView.Core.Generators;
using PicView.Core.ImageDecoding;
using PicView.Core.ImageTransformations;
using PicView.Core.IPlatform;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Navigation.Tiff;
using PicView.Core.Sizing;
using PicView.Core.Thumbnailing;
using PicView.Core.Titles;
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
public class TabViewModel : IDisposable
{
    public TabViewModel(MainWindowViewModel parentWindowContext, TabServices? services = null,
        IArchiveEntryWriter? archiveEntryWriter = null)
    {
        Services = services;
        ParentWindowContext = parentWindowContext;
        _archiveEntryWriter = archiveEntryWriter;
        _fileWatcherService = services?.Watcher;
        Orientation = new TabOrientation(parentWindowContext?.Translation);
        _currentWindowSettings = new ReactiveProperty<WindowSettings?>(parentWindowContext?.WindowSettings);
        // Follows the window from the start, so it is the window's Side-by-Side before anything reads it
        _isShowingPair = FollowWindowSetting(static w => w.IsSideBySide,
                WindowSettings.IsSideBySideWithoutWindow(Settings))
            .CombineLatest(_comparing, static (beside, compares) => beside || compares)
            .DistinctUntilChanged()
            .ToReadOnlyReactiveProperty();
        // The gallery is made before the tab knows its window, so it takes the window's look from here; a tab with no
        // window keeps the record's
        Gallery.FollowLook(FollowWindowSetting(static w => w.Gallery.Look,
            WindowGallerySettings.LookWithoutWindow(Settings?.Gallery)));
        // What the switcher remembers goes to the window the tab is in at that moment
        Gallery.Switcher.RemembersIn(() => _currentWindowSettings.CurrentValue?.Gallery);
    }

    #region Tab logic

    /// <summary>
    /// The services this tab was created with, which are the same for every tab. Null for a tab created before they
    /// were composed.
    /// </summary>
    internal TabServices? Services { get; }

    private readonly IArchiveEntryWriter? _archiveEntryWriter;

    /// <summary>
    /// The Main Window that owns this tab. Only that window's <see cref="TabOverviewViewModel"/> changes it, when the
    /// tab moves there.
    /// </summary>
    public MainWindowViewModel ParentWindowContext { get; internal set; }

    private readonly ReactiveProperty<WindowSettings?> _currentWindowSettings;

    /// <summary>
    /// The Window Settings of the Main Window this tab is in. Publishes again when the tab enters another window;
    /// null only for a tab without a window (tests).
    /// </summary>
    public ReadOnlyReactiveProperty<WindowSettings?> CurrentWindowSettings => _currentWindowSettings;

    /// <summary>
    /// Publishes the settings of <see cref="ParentWindowContext"/>. Called by <see cref="TabOverviewViewModel"/>, the
    /// one writer of a tab's window.
    /// </summary>
    internal void FollowWindow() => _currentWindowSettings.Value = ParentWindowContext?.WindowSettings;

    /// <summary>
    /// Follows one value of this tab's current window, switching when the tab enters another window; gives
    /// <paramref name="withoutWindow"/> for a tab with no window, and never subscribes to a closed window's settings.
    /// </summary>
    /// <remarks>
    /// Each window's value is published at once when the tab enters that window, so a follower catches up with the
    /// same handler as a change. A closed window's settings are disposed and refuse a subscriber, so a tab that still
    /// has them, as one moved out of a window that closed before the new window opened, follows nothing from them
    /// until it enters another window.
    /// </remarks>
    internal Observable<T> FollowWindowSetting<T>(Func<WindowSettings, Observable<T>> pick, T withoutWindow) =>
        _currentWindowSettings
            .Select(settings => settings is null ? Observable.Return(withoutWindow) : settings.Follow(pick))
            .Switch();

    private readonly ReactiveProperty<string?> _listedDirectory = new(null);

    /// <summary>
    /// The Listed Directory: the directory this tab's Load listed its files from, or the archive it opened, by the
    /// name <see cref="DirectoryOrders"/> keeps its File Order under. Null for a tab that lists nothing. Only the
    /// tab's Load and its folder changes set it, with the list.
    /// </summary>
    public ReadOnlyReactiveProperty<string?> ListedDirectory => _listedDirectory;

    /// <summary>
    /// Records the directory the tab's files were listed from, which the tab shares its File Order with every other
    /// tab that lists it. Null for a tab that lists nothing any more.
    /// </summary>
    /// <param name="carried">The order a directory no tab listed starts with, as a renamed folder keeps its own;
    /// the Default File Order otherwise.</param>
    internal void ListDirectory(string? directory, FileOrder? carried = null)
    {
        var previous = _listedDirectory.CurrentValue;
        if (Services?.Orders is not { } orders)
        {
            _listedDirectory.Value = DirectoryOrders.KeyOf(directory);
            return;
        }

        if (directory is null)
        {
            orders.Leave(this, previous);
            _listedDirectory.Value = null;
            return;
        }

        _listedDirectory.Value = orders.Enter(this, directory, previous, carried);
    }

    /// <summary>
    /// The ordering this tab lists with now: the File Order of its Listed Directory (the Default File Order for a tab
    /// that lists nothing) with its platform's name comparison (ordinal for a tab without services).
    /// </summary>
    internal FileOrdering CurrentOrdering() => OrderingFor(_listedDirectory.CurrentValue);

    /// <summary>
    /// The ordering a Load of this tab lists <paramref name="directory"/> with: that directory's File Order with the
    /// tab's name comparison.
    /// </summary>
    internal FileOrdering OrderingFor(string? directory)
    {
        // The record may not be loaded yet for a tab without services
        var order = Services?.Orders.OrderOf(directory) ?? FileOrder.FromAppSettings(Settings);
        // A tab's services may lack a platform in tests, as its Load allows
        Func<string, string, int> compareNames = Services?.Platform is { } platform
            ? platform.CompareStrings
            : string.CompareOrdinal;
        return new FileOrdering(order, compareNames);
    }

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
    /// The views this tab's Main Window makes for it while it is attached there (ADR-0022). Only the window's
    /// <see cref="TabOverviewViewModel"/> sets them.
    /// </summary>
    internal ITabViews? Views { get; private set; }

    internal void SetViews(ITabViews? views)
    {
        _viewsSubscription?.Dispose();
        Views = views;
        _viewsSubscription = views?.View.AsObservable().Subscribe(view => _currentView.Value = view);
        if (views is null)
        {
            _currentView.Value = null;
        }
    }

    private IDisposable? _viewsSubscription;

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
    
    /// <summary>
    /// The control this tab's Main Window shows for its Tab View, or null for a tab no window has attached. Derived from
    /// the window's view factory (ADR-0022); nothing assigns it.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<object?> CurrentView => _currentView;

    private readonly BindableReactiveProperty<object?> _currentView = new();

    /// <summary>
    /// Which view the tab shows: the Tab View (ADR-0022). Derived from the Crop, the Title Phase and whether the tab
    /// shows anything, in the tab's own methods; setting the same kind notifies nothing.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<TabViewKind> ViewKind => _viewKind;

    private readonly BindableReactiveProperty<TabViewKind> _viewKind = new(TabViewKind.StartUpMenu);

    private bool Shows => Model.Image is not null || Model.FileInfo is not null;

    // Reads _titles, not Titles, so a tab nothing binds still makes no TabTitles
    private void DeriveViewKind()
    {
        // A load that ends after the tab closed calls this on a disposed tab, which stays harmless
        if (_viewKind.IsDisposed)
        {
            return;
        }

        if (_crop is not null)
        {
            _viewKind.Value = TabViewKind.Crop;
            return;
        }

        if (_comparison is not null)
        {
            _viewKind.Value = TabViewKind.Comparison;
            return;
        }

        var phase = _titles?.Phase ?? TitlePhase.None;
        _viewKind.Value = Shows || phase is TitlePhase.Loading or TitlePhase.Downloading
            ? TabViewKind.ImageViewer
            : TabViewKind.StartUpMenu;
    }

    #region Comparison

    /// <summary>
    /// Whether this tab compares its pair. It stays on while the tab shows nothing, so the next image it shows
    /// compares. Only <see cref="SetComparing"/> writes it.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<bool> IsComparing => _comparing;

    /// <summary>
    /// Whether this tab shows a pair: the Side-by-Side of the window it is in, or its own comparing.
    /// </summary>
    public ReadOnlyReactiveProperty<bool> IsShowingPair => _isShowingPair;

    /// <summary>
    /// The tab's comparison, which it has exactly while it compares and shows something, or null.
    /// </summary>
    public ComparisonViewModel? Comparison => _comparison;

    private readonly BindableReactiveProperty<bool> _comparing = new(false);
    private readonly ReadOnlyReactiveProperty<bool> _isShowingPair;

    private ComparisonViewModel? _comparison;
    private readonly Lock _comparisonGate = new();

    /// <summary>
    /// Turns comparing on or off for this tab only. On while it shows something makes its <see cref="Comparison"/>;
    /// off disposes it.
    /// </summary>
    public void SetComparing(bool on)
    {
        if (IsDisposed)
        {
            return;
        }

        _comparing.Value = on;
        UpdateComparison(pairChanged: false);
    }

    // The tab has a comparison exactly while it compares and shows something. The kind is derived before an old
    // comparison is disposed, so its view leaves the window first
    private void UpdateComparison(bool pairChanged)
    {
        lock (_comparisonGate)
        {
            if (IsDisposed)
            {
                return;
            }

            var wanted = _comparing.Value && Shows;
            if (wanted && _comparison is null)
            {
                _comparison = new ComparisonViewModel(Model, SecondaryModel,
                    Services?.GlobalSettings is { } gs ? gs.SaveSoon : static () => { },
                    Services?.Cache is { } cache ? cache.Hold : null);
                DeriveViewKind();
            }
            else if (!wanted && _comparison is { } old)
            {
                _comparison = null;
                DeriveViewKind();
                old.Dispose();
            }
            else if (pairChanged && _comparison is { } comparison)
            {
                comparison.OnPairChanged(Model, SecondaryModel);
            }
        }
    }

    #endregion

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
            DeriveViewKind();
        }
    }

    private CropViewModel? _crop;

    public ICropService? CropService { get; set; }

    private void UpdateGalleryHidden() =>
        Gallery.Switcher.SetHidden(_crop is not null || _singleImageType is not SingleImageType.None);
    
    public GalleryCoordinator? GalleryCoordinator { get; private set; }
    
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
            // Here, before the shown mark, so the fitter reads the upright image in the Fit that follows it (ADR-0026)
            Orientation.Reset();
        }

        // A file PicView rewrote with the turn applied shows upright, in the same showing, so it never shows turned
        // twice. Any other image showing first ends the wait, as the user moved on
        if (change is ShownChange.Image && Interlocked.Exchange(ref _uprightOnShownPath, null) is { } upright &&
            string.Equals(Model.FileInfo?.FullName, upright, StringComparison.Ordinal))
        {
            Orientation.Reset();
        }

        // The titles are marked after the image bindable is set and before the UI reacts
        Titles.OnShown(change);
        DeriveViewKind();
        DerivePartnerIndex();
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
        SingleImageTitle = title;
        ShowModels(model, null);
        RecordShown();
        // A Single Image is shown upright and unflipped, whatever the zoom setting; a closed tab has no Orientation
        if (!IsDisposed)
        {
            Orientation.Reset();
        }
        Publish(ShownChange.Image);
    }

    // The title of the Single Image the tab shows, which its View History entry keeps

    /// <summary>The title <see cref="DisplaySingleImage"/> was given, which the Titles name a Single Image by.</summary>
    internal string? SingleImageTitle { get; private set; }

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
        SecondaryFileInfo.Value = secondary?.FileInfo;

        Model = primary;
        FileInfo.Value = primary.FileInfo;
        Image.Value = primary.Image;

        // After both images: a control that loses its player draws its image again, and the old one may be disposed by
        // now, as when the next file is in another directory. Before the image types, so what reacts to a type finds
        // the playback already there
        Animation.Show(primary, secondary);
        SecondaryImageType.Value = secondary?.ImageType;
        ImageType.Value = primary.ImageType;

        UpdateComparison(pairChanged: true);
        HoldShown();
    }

    // The cache references an image for the tab only while it is in the tab's preload window, and a preload that ends
    // after the tab turned round can evict the shown image from it. So the tab holds what it shows itself
    private readonly Lock _shownLeaseGate = new();
    private IDisposable? _shownLease;
    private IDisposable? _shownSecondaryLease;

    /// <summary>
    /// Holds the images the tab shows now in the cache, and lets go of the ones it held before. Call after the shown
    /// models change, and after a shown model is put in the cache, which has nothing to hold before that.
    /// </summary>
    internal void HoldShown()
    {
        // One at a time, and for what the tab shows by then rather than what a caller showed: two threads may show
        // different images at once, and whichever comes last must leave the shown one held
        lock (_shownLeaseGate)
        {
            var cache = IsDisposed ? null : Services?.Cache;
            // The new ones first, as showing the same image again must not let go of it in between
            var lease = Model is { } primary ? cache?.Hold(primary) : null;
            var secondaryLease = SecondaryModel is { } secondary ? cache?.Hold(secondary) : null;
            _shownLease?.Dispose();
            _shownSecondaryLease?.Dispose();
            _shownLease = lease;
            _shownSecondaryLease = secondaryLease;
        }
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
            entry = ViewHistoryEntry.ForSingleImage(model, type, SingleImageTitle,
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
            Animation.ReplacePrimary(file);
            FileInfo.Value = file;
        }
        else if (IsFileAt(secondary, path))
        {
            Follow(secondary!);
            Animation.ReplaceSecondary(file);
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
    /// again at the page. It looks the page's file up in what the open archive extracted, for an archive local software
    /// extracted whole as for any other, and reads nothing from the disk.
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
    public TabOrientation Orientation { get; }
    public BindableReactiveProperty<bool> ShouldCropBeEnabled { get; } = new(false);
    public BindableReactiveProperty<bool> ShouldOptimizeImageBeEnabled { get; } = new(false);
    public BindableReactiveProperty<bool> ShouldPrintBeEnabled { get; } = new(true);
    public BindableReactiveProperty<bool> ShouldCloseImageBeVisible { get; } = new(false);
    /// <summary>
    /// The tab's Zoom Level and its two inputs, the Fit Scale and the Zoom Factor (ADR-0016).
    /// </summary>
    public TabZoom Zoom { get; } = new();

    /// <summary>
    /// The playbacks of the animated images the tab shows, one per image shown beside each other.
    /// </summary>
    public TabAnimation Animation { get; } = new();

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
                    var openings = new ArchiveOpenings(this, _archiveEntryWriter);
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
            if (_load is not null)
            {
                return _load;
            }
            if (Services is null)
            {
                throw new InvalidOperationException("The tab has no services");
            }
            LazyInitializer.EnsureInitialized(ref _load, () => new TabLoad(this, Services));

            return _load;
        }
    }

    private TabLoad? _load;

    // Makes and keeps the tab's thumbnails, which it releases when it closes or becomes fresh
    public Thumbnails? Thumbnails => Services?.Thumbnails;

    private readonly IFileWatcherService? _fileWatcherService;
    
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
        DerivePartnerIndex();
        DeriveCanNavigate();
    }

    /// <summary>
    /// The index of the image shown beside the one at <see cref="NavigationIndex"/>, or -1 when it shows alone, so the
    /// gallery marks both images of a Side-by-Side Pair. Derived by the tab; nothing outside it writes it.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<int> PartnerIndex => _partnerIndex;

    private readonly BindableReactiveProperty<int> _partnerIndex = new(-1);

    // Derived from where the tab is and whether it shows a pair, by the one pair rule (ADR-0024)
    private void DerivePartnerIndex()
    {
        var partner = -1;
        if (SecondaryModel is not null)
        {
            var (showsPair, looping) = PairSettings();
            partner = SideBySidePair.PairAt(NavigationIndex.CurrentValue, MaxIndex.CurrentValue, showsPair, looping)
                .Partner ?? -1;
        }

        _partnerIndex.Value = partner;
    }

    private IDisposable? _settingsSubscription;

    // The re-sort: one run at a time, which runs again while it was asked for again meanwhile
    private readonly Lock _resortGate = new();
    private Task _resorting = Task.CompletedTask;
    private bool _isResortRunning;
    private bool _isResortRequested;
    private bool _isReshuffleRequested;
    private bool _isPairCheckRequested;

    /// <summary>
    /// The re-sort that runs now, or the last one, which ends once the tab's files are in its directory's File Order.
    /// </summary>
    internal Task Resorting
    {
        get
        {
            lock (_resortGate)
            {
                return _resorting;
            }
        }
    }

    /// <summary>
    /// Sorts the files this tab browses into its directory's File Order in memory, off the caller's thread, keeping the
    /// Shown File; with a pair shown, the pair at its new place is shown, without a Load. A tab without files changes
    /// nothing, as its next load lists in that order. A tab showing a Single Image sorts its files and shows nothing
    /// again. Asked again while a re-sort runs, that run sorts once more when it ends, so any number of changes
    /// meanwhile end in the latest order.
    /// </summary>
    /// <param name="reshuffle">Whether files already in a Random order are shuffled again.</param>
    /// <param name="checksPair">Whether a shown pair that is not the one the list gives its shown file is shown again
    /// even when the files are in the order already, as after a load showed a pair it chose before a re-sort.</param>
    /// <returns>The run, which ends once the files are in the order.</returns>
    internal Task ResortAsync(bool reshuffle = false, bool checksPair = false)
    {
        lock (_resortGate)
        {
            _isResortRequested = true;
            _isReshuffleRequested |= reshuffle;
            _isPairCheckRequested |= checksPair;
            if (!_isResortRunning)
            {
                _isResortRunning = true;
                _resorting = Task.Run(RunResortsAsync);
            }

            return _resorting;
        }
    }

    private async Task RunResortsAsync()
    {
        while (true)
        {
            bool reshuffle;
            bool checksPair;
            lock (_resortGate)
            {
                if (!_isResortRequested || IsDisposed)
                {
                    _isResortRunning = false;
                    _isResortRequested = false;
                    _isReshuffleRequested = false;
                    _isPairCheckRequested = false;
                    return;
                }

                _isResortRequested = false;
                reshuffle = _isReshuffleRequested;
                _isReshuffleRequested = false;
                checksPair = _isPairCheckRequested;
                _isPairCheckRequested = false;
            }

            try
            {
                await ResortOnceAsync(reshuffle, checksPair).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(TabViewModel), nameof(ResortAsync), e);
            }
        }
    }

    private async ValueTask ResortOnceAsync(bool reshuffle, bool checksPair)
    {
        if (IsDisposed || ImageIterator is not { Files.Count: > 0 } iterator)
        {
            return;
        }

        var ordering = CurrentOrdering();
        var isShuffledAgain = reshuffle && ordering.Order.By is SortFilesBy.Random;
        if (ordering.Order == iterator.Ordering.Order && !isShuffledAgain)
        {
            // In the order already, though a load may have shown a pair it chose before a re-sort it did not see
            if (checksPair && SingleImageType is SingleImageType.None && PairSettings().ShowsPair &&
                !IsShowingPairAt(iterator))
            {
                await iterator.ReloadAsync(clearCache: false).ConfigureAwait(false);
            }

            return;
        }

        iterator.Resort(ordering);
        // A tab closed meanwhile has released its images, and a Single Image is shown instead of the files, with the
        // gallery hidden, so neither shows the files again nor preloads them
        if (IsDisposed || SingleImageType is not SingleImageType.None)
        {
            return;
        }

        if (PairSettings().ShowsPair && !IsShowingPairAt(iterator))
        {
            // As turning Side-by-Side on: the shown file is iterated to again, which shows the pair that holds it
            await iterator.ReloadAsync(clearCache: false).ConfigureAwait(false);
            return;
        }

        // Its neighbours in the new order, as a relist preloads them
        var files = iterator.Files;
        var index = iterator.CurrentIndex;
        if (Services is { } services && index >= 0 && index < files.Count)
        {
            services.Cache.Preload(Id, files[index], false, GetTabCancellation().Token);
        }
    }

    // Whether the tab shows the pair at the iterator's position, as its list gives it (ADR-0024). The secondary index
    // is not asked, as Initialize does not set it
    private bool IsShowingPairAt(IImageIterator iterator)
    {
        var files = iterator.Files;
        // A step whose target is current but whose pair has not shown yet leaves the old pair on screen, which a
        // re-sort can leave a pair of the list though the position names the target
        if (Model?.FileInfo is not { } shown || SideBySidePair.IndexOf(files, shown) is not { } primary ||
            primary != iterator.CurrentIndex)
        {
            return false;
        }

        var (expected, partner) = SideBySidePair.PairAt(primary, files.Count, true, PairSettings().Looping);
        if (expected != primary)
        {
            return false;
        }

        var beside = SecondaryModel?.FileInfo;
        return partner is { } index
            ? beside is not null &&
              string.Equals(files[index].FullName, beside.FullName, StringComparison.OrdinalIgnoreCase)
            : beside is null;
    }

    /// <summary>
    /// Sorts the files again when a load listed them in an order their directory no longer has, as when it was sorted,
    /// or first listed by a tab while the Default File Order changed, while the load listed. Called by the tab's Load
    /// after it gives the iterator a list and records the Listed Directory.
    /// </summary>
    /// <param name="showedPair">Whether the load showed what it listed, so a pair it showed is checked against the
    /// list, which a re-sort may have changed after the load chose the pair.</param>
    internal void FollowListedOrder(bool showedPair = false)
    {
        if (ImageIterator is not { Files.Count: > 0 } iterator)
        {
            return;
        }

        if (iterator.Ordering.Order != CurrentOrdering().Order || (showedPair && PairSettings().ShowsPair))
        {
            _ = ResortAsync(checksPair: showedPair);
        }
    }

    /// <summary>
    /// Whether a pair is shown (the window's Side-by-Side or this tab's comparing) and whether the window loops, as the
    /// window the tab is in publishes them, or the record's settings for a tab without a window.
    /// </summary>
    public (bool ShowsPair, bool Looping) PairSettings() => _currentWindowSettings.CurrentValue is { } window
        ? (IsShowingPair.CurrentValue, window.IsLooping.CurrentValue)
        : (WindowSettings.IsSideBySideWithoutWindow(Settings) || IsComparing.Value,
            WindowSettings.IsLoopingWithoutWindow(Settings));

    /// <summary>
    /// Follows the settings the tab reads, once: the looping of the window the tab is in derives Next and Prev again,
    /// showing a pair (the window's Side-by-Side or this tab's comparing) derives them and has the shown file iterated
    /// to again, and including subdirectories lists the folder again. The File Order is not followed here: the tab's
    /// Listed Directory has it, and <see cref="DirectoryOrders"/> sorts the tab's files again when it changes.
    /// The window's settings switch to the next window's when the tab moves there, which catches up in the same way.
    /// </summary>
    private void FollowSettings()
    {
        // A tab closed while its first load ran has disposed what it would subscribe to
        if (_settingsSubscription is not null || IsDisposed || Services?.GlobalSettings is not { } settings)
        {
            return;
        }

        // The relist reads the stored value, so the write-throughs are notified before the tab
        settings.InitializeActionSettings();
        _settingsSubscription = Disposable.Combine(
            // The first value is not skipped, as a tab whose window had closed hears only its next window's; deriving
            // again from an unchanged looping changes nothing
            FollowWindowSetting(static w => w.IsLooping, WindowSettings.IsLoopingWithoutWindow(Settings))
                .DistinctUntilChanged()
                .Subscribe(_ => DeriveCanNavigate()),
            // The pair is derived from the files, so the shown file is iterated to again, as a pair or alone
            IsShowingPair.Skip(1).SubscribeAwait(async (_, _) =>
            {
                DeriveCanNavigate();
                if (ImageIterator is { Files.Count: > 0 } iterator)
                {
                    await iterator.ReloadAsync(clearCache: false).ConfigureAwait(false);
                }
            }),
            settings.IsIncludingSubdirectories.Skip(1).SubscribeAwait(async (_, x) =>
            {
                if (ImageIterator is { Files.Count: > 0 } && FileInfo.CurrentValue is { } file)
                {
                    await Task.Run(() => Load.LoadAsync(file.FullName, new LoadOptions(Relist: true)).AsTask(), x).ConfigureAwait(false);
                }
            }));
    }

    /// <summary>
    /// Whether Next and Prev run, derived from the tab's own state (ADR-0017): no iterator or a crop means neither; a
    /// list of one and nothing extracting means neither; else forwards needs an image ahead (a pair when side-by-side
    /// or comparison is on), or looping, or an extraction still listing pages; backwards needs an image behind, or
    /// looping.
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
                var (showsPair, looping) = PairSettings();
                forwards = SideBySidePair.CanNavigateForwards(index, count, extracting, showsPair, looping);
                backwards = looping || index > 0;
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
    /// Makes any load still running give way to what the tab shows next without one, such as a pasted image: it is
    /// superseded and cancelled, so the file it decodes does not replace that image when it ends.
    /// </summary>
    public void SupersedeLoads()
    {
        BeginLoad();
        ResetNavigationCts();
    }

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

    #region Busy

    private int _busyClaims;
    private readonly BindableReactiveProperty<bool> _isBusy = new();
    private readonly Lock _busyGate = new();

    /// <summary>
    /// Whether an operation on this tab, such as a crop save, an optimize or a rename, holds a busy claim.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<bool> IsBusy => _isBusy;

    /// <summary>
    /// Claims the tab Busy until the returned claim is disposed. Claims count, so the tab stays Busy until every
    /// claim ends; disposing a claim twice releases it once.
    /// </summary>
    public IDisposable ClaimBusy()
    {
        Interlocked.Increment(ref _busyClaims);
        PublishBusy();
        return new BusyClaim(this);
    }

    // Claims start and end on any thread. Publishing under one lock orders the writes, so the last thread to publish
    // has already changed the count and publishes the state as it is now; a publish after the tab is disposed does
    // nothing, since Dispose disposes the property under the same lock
    private void PublishBusy()
    {
        lock (_busyGate)
        {
            if (Volatile.Read(ref _isDisposed))
            {
                return;
            }

            _isBusy.Value = Volatile.Read(ref _busyClaims) > 0;
        }
    }

    private sealed class BusyClaim(TabViewModel tab) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
            {
                return;
            }

            Interlocked.Decrement(ref tab._busyClaims);
            tab.PublishBusy();
        }
    }

    #endregion

    #region Titles

    /// <summary>
    /// Puts the tab in the Loading Title Phase, until the next image shows or <see cref="EndLoading(int)"/>.
    /// </summary>
    /// <returns>The ticket that <see cref="EndLoading(int)"/> takes.</returns>
    public int SetLoading()
    {
        var ticket = Titles.EnterLoading();
        DeriveViewKind();
        return ticket;
    }

    /// <summary>Puts the tab in the Downloading Title Phase with <paramref name="text"/>, such as a progress report.</summary>
    /// <returns>The ticket that <see cref="EndLoading(int)"/> takes.</returns>
    public int SetDownloading(string text)
    {
        var ticket = Titles.Enter(TitlePhase.Downloading, text);
        DeriveViewKind();
        return ticket;
    }

    /// <summary>
    /// Puts the tab in the Error Title Phase with <paramref name="text"/>, until the next load or shown image. An Error
    /// has no end call.
    /// </summary>
    public void SetError(string text)
    {
        Titles.Enter(TitlePhase.Error, text);
        DeriveViewKind();
    }

    /// <summary>
    /// Ends Loading or Downloading when the load that entered it with <paramref name="ticket"/> gave up without showing
    /// anything, unless a newer phase was entered since. An Error stays.
    /// </summary>
    public void EndLoading(int ticket)
    {
        Titles.EndLoading(ticket);
        DeriveViewKind();
    }

    /// <summary>
    /// Shows <paramref name="image"/> in place of the shown model's image, as a Stand-in: a thumbnail while the next
    /// image loads, or an Effects Render. What the tab shows is unchanged: neither model changes, nothing is recorded
    /// and <see cref="Shown"/> does not fire. A Stand-in ends the animation playbacks.
    /// </summary>
    public void ShowStandIn(object? image)
    {
        // The image first: a control that loses its player draws its image again, and the old one may be disposed
        Image.Value = image;
        Animation.End();
    }

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

    /// <summary>
    /// Gives the tab a new iterator and gallery coordinator from its services, with no files. A test seam: no
    /// production code calls it, since the tab's <see cref="Load"/> makes the iterator on its first load.
    /// </summary>
    /// <exception cref="InvalidOperationException">The tab has no services.</exception>
    /// <param name="alsoFilesChanged">Hears every change to the file list after the gallery does, for a test that
    /// records them.</param>
    internal void Initialize(Action<FileListChange>? alsoFilesChanged = null)
    {
        if (Services is null)
        {
            throw new InvalidOperationException("The tab has no services");
        }
        GalleryCoordinator?.Dispose();
        var coordinator = new GalleryCoordinator(this, Services.Thumbnails);
        GalleryCoordinator = coordinator;
        ImageIterator = new ImageIterator(Services.Cache, Services.Thumbnails, this,
            alsoFilesChanged is null
                ? coordinator.FilesChanged
                : change =>
                {
                    coordinator.FilesChanged(change);
                    alsoFilesChanged(change);
                });
        FollowSettings();
        DeriveCanNavigate();
    }

    /// <summary>
    /// Makes the tab's iterator and gallery coordinator from its services when it has none, and has it browse
    /// <paramref name="files"/> at the file it shows, which it then watches the folder of.
    /// </summary>
    /// <exception cref="InvalidOperationException">The tab has no services.</exception>
    internal void InitializeImageIterator(IReadOnlyList<FileInfo> files)
    {
        if (Services is null)
        {
            throw new InvalidOperationException("The tab has no services");
        }
        var cache = Services.Cache;
        GalleryCoordinator ??= new GalleryCoordinator(this, Services.Thumbnails);
        ImageIterator ??= new ImageIterator(cache, Services.Thumbnails, this,
            GalleryCoordinator.FilesChanged);
        FollowSettings();
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
            HoldShown();
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
    /// its files, the image it shows and its gallery, and shows the start-up menu. Its View History is kept, so Back
    /// shows the image it showed again.
    /// </summary>
    internal ValueTask BecomeFreshAsync()
    {
        _fileWatcherService?.Unwatch(this);
        ResetNavigationCts();
        Thumbnails?.Release(Id);
        DisposeImageIterator();
        Display(new ImageModel());
        // The View History is kept, so Back shows what the tab showed again
        ViewHistory.Leave();
        _hasLostItsFiles = true;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Shows the tab's file again after PicView rewrote it, such as a save over it: decoded anew, with its thumbnail
    /// forgotten. When <paramref name="turnApplied"/> the file holds the tab's turn and flip, so it shows upright, since
    /// keeping the turn would show it turned twice and the next save would turn the file twice. The turn is undone as the
    /// rewritten file shows, so it never shows turned twice even for a moment.
    /// </summary>
    public async ValueTask ShowRewrittenAsync(bool turnApplied)
    {
        if (FileInfo.CurrentValue is not { } file)
        {
            return;
        }

        var path = file.FullName;
        ForgetRewritten(path);
        if (ImageIterator is not { } iterator)
        {
            return;
        }

        if (turnApplied)
        {
            Volatile.Write(ref _uprightOnShownPath, path);
        }

        try
        {
            await iterator.ReloadAsync(clearCache: false).ConfigureAwait(false);
        }
        finally
        {
            // A reload that showed nothing must not undo the turn of a later showing of the file
            Interlocked.CompareExchange(ref _uprightOnShownPath, null, path);
        }
    }

    /// <summary>
    /// Forgets the decoded image and the thumbnail of a file PicView rewrote, without showing it again, as the pixels it
    /// wrote are what the tab shows already.
    /// </summary>
    public void ForgetRewritten(string path)
    {
        // The image first, as the file watcher does, so a thumbnail made meanwhile is not made from the old image
        Services?.Cache.Invalidate(path);
        Thumbnails?.Forget(path);
    }

    /// <summary>
    /// Shows a file PicView wrote in place of the one the tab showed, such as a save under another name or format. When
    /// <paramref name="turnApplied"/> it holds the tab's turn and flip, so it shows upright.
    /// </summary>
    public async ValueTask ShowReplacementAsync(string newPath, bool turnApplied)
    {
        if (turnApplied)
        {
            Volatile.Write(ref _uprightOnShownPath, newPath);
        }

        try
        {
            await Load.LoadAsync(newPath).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.CompareExchange(ref _uprightOnShownPath, null, newPath);
        }
    }

    // The rewritten file whose next showing undoes the turn, set by ShowRewrittenAsync and ShowReplacementAsync
    private string? _uprightOnShownPath;

    /// <summary>
    /// Closes the image the tab shows and shows the start-up menu, as the Close image action: the tab becomes fresh, as
    /// when it lost its files, and leaves the archives it opened. Does nothing unless the tab shows the image viewer or
    /// its comparison, so a crop and the start-up menu stay as they are. Comparing stays on, so the next image the tab
    /// shows compares.
    /// </summary>
    public async ValueTask CloseImageAsync()
    {
        if (_viewKind.Value is not (TabViewKind.ImageViewer or TabViewKind.Comparison))
        {
            return;
        }

        await BecomeFreshAsync().ConfigureAwait(false);
        Archives.Cleanup();
    }

    /// <summary>
    /// Closes this tab in the Main Window that owns it now, which also disposes it.
    /// </summary>
    public void CloseTab()
    {
        IsClosing = true; // Signal it to be removed from the UI
        ParentWindowContext.WindowTabs.CloseTab(this);
    }

    /// <summary>
    /// Closes every other tab of the Main Window that owns this tab.
    /// </summary>
    public void CloseOtherTabs() => ParentWindowContext.WindowTabs.CloseOtherTabs(this);

    /// <summary>
    /// Closes the tabs to the left of this tab in the Main Window that owns it.
    /// </summary>
    public void CloseTabsToTheLeft() => ParentWindowContext.WindowTabs.CloseTabsToTheLeft(this);

    /// <summary>
    /// Closes the tabs to the right of this tab in the Main Window that owns it.
    /// </summary>
    public void CloseTabsToTheRight() => ParentWindowContext.WindowTabs.CloseTabsToTheRight(this);

    public CancellationTokenSource GetTabCancellation()
    {
        if (IsDisposed)
        {
            // A closed tab's source is cancelled and disposed; a late caller gets a cancelled one, never a live one
            var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            return cancelled;
        }

        if (!NavigationCts.IsCancellationRequested)
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
        // No files to browse, so their folder is not followed either: a deleted folder must not reset a Single Image
        _fileWatcherService?.Unwatch(this);
        ImageIterator?.Dispose();
        ImageIterator = null;
        // No files, so no directory is listed, and its File Order goes with its last tab
        ListDirectory(null);
        FileInfo.Value = null;
        SecondaryFileInfo.Value = null;
        SecondaryModel = null;
        HoldShown();
        Animation.End();
        // No files, so the position goes too: a new iterator must not light the buttons from the old list's place
        SetPosition(-1, 0);
    }
    
    public void Dispose()
    {
        // First, so a load still running stops at its next check instead of showing into a disposed tab
        Volatile.Write(ref _isDisposed, true);
        _fileWatcherService?.Unwatch(this);
        Thumbnails?.Release(Id);

        // Disposed by now, so this lets go of what the tab showed and holds nothing
        HoldShown();
        ImageIterator?.Dispose();
        Services?.Orders.Leave(this, _listedDirectory.CurrentValue);
        _archiveStateSubscription?.Dispose();
        _settingsSubscription?.Dispose();
        Archives.Cleanup();
        GalleryCoordinator?.Dispose();

        // Cancel first: a URL download or a preload still holding the token would otherwise run on after the close
        NavigationCts.Cancel();
        NavigationCts.Dispose();
        Orientation.Dispose();
        Zoom.Dispose();
        Animation.Dispose();
        Hookups?.Dispose();
        Views?.Dispose();
        _viewsSubscription?.Dispose();
        _currentView.Dispose();
        _currentWindowSettings.Dispose();
        _titles?.Dispose();
        lock (_comparisonGate)
        {
            _comparison?.Dispose();
            _comparison = null;
        }
        _isShowingPair.Dispose();
        _comparing.Dispose();
        _viewKind.Dispose();
        _shown.Dispose();
        lock (_busyGate)
        {
            _isBusy.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }

}
