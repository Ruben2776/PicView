using System.ComponentModel;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.Titles;

/// <summary>
/// A state a tab is told it is in, which stands in for its derived Titles until it ends (ADR-0015). Loading and
/// Downloading end at the next shown image or when the load gives up; Error ends at the next load or shown image.
/// Cropping is not stored: the tab's <see cref="TabViewModel.Crop"/> is read when the Titles are built.
/// </summary>
public enum TitlePhase
{
    None,
    Loading,
    Downloading,
    Error
}

/// <summary>
/// The Titles a tab derives from what it shows: the titlebar text, its tooltip, the window title and the tab's name.
/// Every input only marks them stale and raises one <see cref="PropertyChanged"/>; the four strings are built once
/// when the next reader asks and kept until the next mark, so a tab nobody binds builds nothing, and a shown image
/// that resets the zoom builds once, since the tab resets the Zoom Factor before it marks (ADR-0016). The tab is the
/// only writer: it marks from its own methods and through three subscriptions on its zoom level and its file list
/// position (ADR-0015).
/// </summary>
public sealed class TabTitles : INotifyPropertyChanged, IDisposable
{
    // One event for all four properties, so a mark allocates nothing. An empty name means "every property" to bindings
    private static readonly PropertyChangedEventArgs AllProperties = new(string.Empty);

    private readonly TabViewModel _tab;
    private readonly Lock _lock = new();
    private readonly IDisposable _subscriptions;

    private bool _stale = true;
    private TitlePhase _phase;
    private string? _phaseText;
    private int _ticket;
    private string _title = string.Empty;
    private string _titleTooltip = string.Empty;
    private string _windowTitle = string.Empty;
    private string _tabTitle = string.Empty;
    private int _buildCount;

    internal TabTitles(TabViewModel tab)
    {
        _tab = tab;
        _subscriptions = Disposable.Combine(
            tab.Zoom.Level.Subscribe(_ => Mark()),
            tab.NavigationIndex.Subscribe(_ => Mark()),
            tab.MaxIndex.Subscribe(_ => Mark()));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The titlebar text.</summary>
    public string Title
    {
        get
        {
            BuildIfStale();
            return _title;
        }
    }

    /// <summary>The titlebar's tooltip: the full path of the shown file.</summary>
    public string TitleTooltip
    {
        get
        {
            BuildIfStale();
            return _titleTooltip;
        }
    }

    /// <summary>The window's title, with the app name.</summary>
    public string WindowTitle
    {
        get
        {
            BuildIfStale();
            return _windowTitle;
        }
    }

    /// <summary>The tab's name in the tab strip.</summary>
    public string TabTitle
    {
        get
        {
            BuildIfStale();
            return _tabTitle;
        }
    }

    /// <summary>The Title Phase the tab is in.</summary>
    public TitlePhase Phase => _phase;

    /// <summary>Whether the tab is cropping, which replaces the titlebar text and its tooltip.</summary>
    public bool IsCropping => _tab.Crop is not null;

    /// <summary>How many times the four strings were built. For tests.</summary>
    internal int BuildCount => _buildCount;

    /// <summary>
    /// Builds the Titles again on the next read. The two title settings, the full path in the titlebar and side by
    /// side, have no change notification, so their toggles call this; nothing else needs to.
    /// </summary>
    public void Refresh() => Mark();

    internal void Mark()
    {
        Volatile.Write(ref _stale, true);
        PropertyChanged?.Invoke(this, AllProperties);
    }

    /// <summary>Enters Loading and returns its ticket, which <see cref="EndLoading(int)"/> takes.</summary>
    internal int EnterLoading() => Enter(TitlePhase.Loading);

    /// <summary>
    /// Enters <paramref name="phase"/> and returns its ticket, a new one each call. Entering replaces whatever phase the
    /// tab was in, so the ticket of the phase it replaced no longer ends anything.
    /// </summary>
    internal int Enter(TitlePhase phase, string? text = null)
    {
        int ticket;
        lock (_lock)
        {
            _phase = phase;
            _phaseText = text;
            ticket = ++_ticket;
        }
        Mark();
        return ticket;
    }

    /// <summary>A shown image ends every phase; a shown file that changed on disk only marks.</summary>
    internal void OnShown(ShownChange change)
    {
        if (change is ShownChange.Image)
        {
            lock (_lock)
            {
                _phase = TitlePhase.None;
                _phaseText = null;
            }
        }
        Mark();
    }

    /// <summary>
    /// The load that said Loading or Downloading with <paramref name="ticket"/> gave up without showing anything. It ends
    /// the phase only while the ticket is the latest one handed out, so a load that gives up late leaves a newer one's
    /// phase in place. An Error stays.
    /// </summary>
    internal void EndLoading(int ticket)
    {
        lock (_lock)
        {
            if (ticket == _ticket)
            {
                EndLoadingLocked();
            }
        }
        Mark();
    }

    // Callers hold _lock
    private void EndLoadingLocked()
    {
        if (_phase is TitlePhase.Loading or TitlePhase.Downloading)
        {
            _phase = TitlePhase.None;
            _phaseText = null;
        }
    }

    private void BuildIfStale()
    {
        if (!Volatile.Read(ref _stale))
        {
            return;
        }

        lock (_lock)
        {
            if (!_stale)
            {
                return;
            }

            // Cleared before building, so a mark that lands while the inputs are read makes the next read build again
            _stale = false;
            Build();
            _buildCount++;
        }
    }

    // Callers hold _lock
    private void Build()
    {
        var translation = TranslationManager.Translation;
        switch (_phase)
        {
            case TitlePhase.Loading:
                SetAll(translation?.Loading ?? string.Empty);
                return;
            case TitlePhase.Downloading:
            case TitlePhase.Error:
                SetAll(_phaseText ?? string.Empty);
                return;
        }

        // Read once, as a load on another thread can show other models meanwhile
        var model = _tab.Model;
        var secondary = _tab.SecondaryModel;
        var iterator = _tab.ImageIterator;
        var singleImageType = _tab.SingleImageType;
        var zoom = _tab.Zoom.Level.CurrentValue;
        var width = model.PixelWidth;
        var height = model.PixelHeight;

        if (singleImageType is not SingleImageType.None)
        {
            // A Single Image is titled by the name it was shown with (ADR-0014); a download by its URL
            var name = singleImageType is SingleImageType.Url
                ? _tab.SourceURL ?? string.Empty
                : _tab.SingleImageTitle
                  ?? (singleImageType is SingleImageType.Clipboard ? translation?.ClipboardImage : model.FileInfo?.FullName)
                  ?? string.Empty;
            var single = ImageTitleFormatter.GenerateTitleForSingleImage(width, height, name, zoom);
            _title = single.BaseTitle;
            _windowTitle = single.TitleWithAppName;
            _titleTooltip = single.FilePathTitle;
            _tabTitle = name;
        }
        else if (model.FileInfo is not { } file)
        {
            SetAll(translation?.NoImage ?? string.Empty);
        }
        else if (iterator is null || iterator.CurrentIndex is -1)
        {
            // A file whose navigation has not listed it yet, as at startup (ADR-0005): the indexed title follows
            SetAll(translation?.Loading ?? string.Empty);
        }
        else
        {
            var count = iterator.Files.Count;
            var first = new ImageTitleInfo(file, width, height, iterator.CurrentIndex, count);
            var sideBySide = Settings.ImageScaling.ShowImageSideBySide && secondary?.FileInfo is not null;
            WindowTitles titles;
            if (model.TiffNavigation is { } tiff)
            {
                titles = ImageTitleFormatter.GenerateTiffTitleStrings(first, zoom, tiff.CurrentPage, tiff.PageCount);
            }
            else if (sideBySide)
            {
                var second = new ImageTitleInfo(secondary!.FileInfo!, secondary.PixelWidth, secondary.PixelHeight,
                    iterator.SecondaryCurrentIndex, count);
                titles = ImageTitleFormatter.GenerateTitleForSideBySide(first, second, zoom, iterator.Files);
            }
            else
            {
                titles = ImageTitleFormatter.GenerateTitleStrings(first, zoom);
            }

            _title = Settings.UIProperties.ShowFullPathInTitleBar ? titles.FilePathTitle : titles.BaseTitle;
            _windowTitle = titles.TitleWithAppName;
            _titleTooltip = titles.FilePathTitle;
            _tabTitle = sideBySide
                ? StringExtensions.CombineWithSeparator(file.Name, secondary!.FileInfo!.Name)
                : file.Name;
        }

        // Cropping replaces only the titlebar text and its tooltip; the window and the tab keep the file's name
        if (_tab.Crop is null)
        {
            return;
        }

        var cropMessage = translation?.CropMessage ?? string.Empty;
        _title = cropMessage;
        _titleTooltip = cropMessage;
    }

    private void SetAll(string text)
    {
        _title = text;
        _titleTooltip = text;
        _tabTitle = text;
        _windowTitle = StringExtensions.CombineWithAppName(text);
    }

    public void Dispose() => _subscriptions.Dispose();
}
