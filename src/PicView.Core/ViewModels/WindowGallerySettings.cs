using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using R3;
using GalleryRecord = PicView.Core.Config.Gallery;

namespace PicView.Core.ViewModels;

/// <summary>
/// One Main Window's gallery look, dock edge and start-docked choice: Window Settings, with the commands that change
/// them. Seeded from the settings record when the window opens and written back only through <see cref="StoreTo"/>;
/// a change writes nothing and saves nothing. The commands start in <see cref="Start"/>, so one executed before it does
/// nothing (ADR-0005, ADR-0034).
/// </summary>
public sealed class WindowGallerySettings : IDisposable
{
    private readonly Lock _startLock = new();
    private bool _isStarted;

    // Read on any thread; written by the display-mode change only
    private volatile bool _rowsWasLastUsedDisplayMode;

    // Read by anything that checks before subscribing
    private volatile bool _isDisposed;

    private WindowGallerySettings(GalleryRecord values)
    {
        DockPosition = new BindableReactiveProperty<GalleryDockPosition>(values.DockPosition);
        StartsDocked = new BindableReactiveProperty<bool>(values.IsGalleryDocked);
        ExpandedGalleryDisplayMode =
            new BindableReactiveProperty<GalleryDisplayMode>(values.ExpandedGalleryDisplayMode);
        DockedGalleryStretchMode = new BindableReactiveProperty<GalleryStretchMode>(values.DockedGalleryStretchMode);
        ExpandedGalleryStretchMode = new BindableReactiveProperty<GalleryStretchMode>(values.ExpandedGalleryStretchMode);
        DockedGalleryItemSize = new BindableReactiveProperty<double>(AcceptedDockedSize(values.DockedGalleryItemSize));
        ExpandedGalleryItemSize =
            new BindableReactiveProperty<double>(AcceptedExpandedSize(values.ExpandedGalleryItemSize));
        _rowsWasLastUsedDisplayMode = values.RowsWasLastUsedDisplayMode;

        IsExpandedDisplayModeGrid = ExpandedGalleryDisplayMode
            .Select(static mode => mode is GalleryDisplayMode.Grid)
            .ToReadOnlyBindableReactiveProperty();
        IsExpandedDisplayModeMasonry = ExpandedGalleryDisplayMode
            .Select(static mode => mode is GalleryDisplayMode.Masonry)
            .ToReadOnlyBindableReactiveProperty();
        IsExpandedDisplayModeRows = ExpandedGalleryDisplayMode
            .Select(static mode => mode is GalleryDisplayMode.Rows)
            .ToReadOnlyBindableReactiveProperty();
        IsExpandedDisplayModeTiles = ExpandedGalleryDisplayMode
            .Select(GalleryDisplayModes.ShowsTiles)
            .ToReadOnlyBindableReactiveProperty();

        IsDockedStretchUniform = DockedGalleryStretchMode
            .Select(static mode => mode is GalleryStretchMode.Uniform)
            .ToReadOnlyBindableReactiveProperty();
        IsDockedStretchUniformToFill = DockedGalleryStretchMode
            .Select(static mode => mode is GalleryStretchMode.UniformToFill)
            .ToReadOnlyBindableReactiveProperty();
        IsDockedStretchSquare = DockedGalleryStretchMode
            .Select(static mode => mode is GalleryStretchMode.Square)
            .ToReadOnlyBindableReactiveProperty();
        IsDockedStretchSquareFill = DockedGalleryStretchMode
            .Select(static mode => mode is GalleryStretchMode.FillSquare)
            .ToReadOnlyBindableReactiveProperty();

        IsExpandedStretchUniform = ExpandedGalleryStretchMode
            .Select(static mode => mode is GalleryStretchMode.Uniform)
            .ToReadOnlyBindableReactiveProperty();
        IsExpandedStretchUniformToFill = ExpandedGalleryStretchMode
            .Select(static mode => mode is GalleryStretchMode.UniformToFill)
            .ToReadOnlyBindableReactiveProperty();
        IsExpandedStretchSquare = ExpandedGalleryStretchMode
            .Select(static mode => mode is GalleryStretchMode.Square)
            .ToReadOnlyBindableReactiveProperty();
        IsExpandedStretchSquareFill = ExpandedGalleryStretchMode
            .Select(static mode => mode is GalleryStretchMode.FillSquare)
            .ToReadOnlyBindableReactiveProperty();
    }

    /// <summary>
    /// The edge every Docked Gallery of this window sits at; set by a tab's Gallery Mode Switcher when the user docks
    /// at an edge.
    /// </summary>
    public BindableReactiveProperty<GalleryDockPosition> DockPosition { get; }

    /// <summary>
    /// Whether a new tab of this window starts with its gallery docked: the last choice made by hand here, set by a
    /// tab's Gallery Mode Switcher. Hiding the gallery for crop, a Single Image or the file limit does not change it.
    /// </summary>
    public BindableReactiveProperty<bool> StartsDocked { get; }

    /// <summary>The Expanded Gallery's display mode; changed by the two display mode commands.</summary>
    public BindableReactiveProperty<GalleryDisplayMode> ExpandedGalleryDisplayMode { get; }

    /// <summary>
    /// Whether Rows (true) or Masonry (false) was the last used of the two tile modes. Changed only by a display-mode
    /// change, after <see cref="Start"/>.
    /// </summary>
    public bool RowsWasLastUsedDisplayMode => _rowsWasLastUsedDisplayMode;

    public BindableReactiveProperty<GalleryStretchMode> DockedGalleryStretchMode { get; }

    public BindableReactiveProperty<GalleryStretchMode> ExpandedGalleryStretchMode { get; }

    /// <summary>The Docked Gallery's item size, held to the docked limits once started.</summary>
    public BindableReactiveProperty<double> DockedGalleryItemSize { get; }

    /// <summary>The Expanded Gallery's item size, held to the expanded limits once started.</summary>
    public BindableReactiveProperty<double> ExpandedGalleryItemSize { get; }

    /// <summary>Sets the display mode; an unknown value sets Grid.</summary>
    public ReactiveCommand<GalleryDisplayMode> SetExpandedDisplayModeCommand { get; } = new();

    /// <summary>Goes from Grid to the last used tile mode, and from a tile mode back to Grid.</summary>
    public ReactiveCommand<Unit> ToggleExpandedDisplayModeCommand { get; } = new();

    /// <summary>Sets the Docked Gallery's Thumbnail Stretch; an unknown value is ignored.</summary>
    public ReactiveCommand<GalleryStretchMode> SetDockedStretchModeCommand { get; } = new();

    /// <summary>Sets the Expanded Gallery's Thumbnail Stretch; an unknown value is ignored.</summary>
    public ReactiveCommand<GalleryStretchMode> SetExpandedStretchModeCommand { get; } = new();

    public IReadOnlyBindableReactiveProperty<bool> IsExpandedDisplayModeGrid { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsExpandedDisplayModeMasonry { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsExpandedDisplayModeRows { get; }

    /// <summary>Whether the display mode shows tiles, which is Masonry and Rows.</summary>
    public IReadOnlyBindableReactiveProperty<bool> IsExpandedDisplayModeTiles { get; }

    public IReadOnlyBindableReactiveProperty<bool> IsDockedStretchUniform { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsDockedStretchUniformToFill { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsDockedStretchSquare { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsDockedStretchSquareFill { get; }

    public IReadOnlyBindableReactiveProperty<bool> IsExpandedStretchUniform { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsExpandedStretchUniformToFill { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsExpandedStretchSquare { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsExpandedStretchSquareFill { get; }

    /// <summary>Whether the window closed and disposed these settings.</summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>The values of the record, or the defaults without one.</summary>
    public static WindowGallerySettings FromRecord(GalleryRecord? record) => new(record ?? new GalleryRecord());

    /// <summary>
    /// The look a gallery with no window shows (tests): the record's, dock edge included, as a window seeded from it
    /// would have it, or the defaults without one.
    /// </summary>
    public static GalleryLook LookWithoutWindow(GalleryRecord? record)
    {
        var values = record ?? new GalleryRecord();
        return new GalleryLook(AcceptedDockedSize(values.DockedGalleryItemSize),
            AcceptedExpandedSize(values.ExpandedGalleryItemSize), values.DockedGalleryStretchMode,
            values.ExpandedGalleryStretchMode, values.ExpandedGalleryDisplayMode, values.DockPosition);
    }

    /// <summary>
    /// Whether a tab with no window (tests) starts with its gallery docked: the record's choice, as a window seeded
    /// from it would have it, or the default without one.
    /// </summary>
    public static bool StartsDockedWithoutWindow(GalleryRecord? record) => (record ?? new GalleryRecord()).IsGalleryDocked;

    /// <summary>
    /// This window's gallery look, dock edge included, published at once on subscribe and again on each change. The
    /// sizes are the accepted ones: a size outside its limits is set back from inside its own change, so a subscriber
    /// after <see cref="Start"/> hears the refused size last.
    /// </summary>
    public Observable<GalleryLook> Look =>
        Observable.CombineLatest(
            DockedGalleryItemSize.Select(AcceptedDockedSize),
            ExpandedGalleryItemSize.Select(AcceptedExpandedSize),
            DockedGalleryStretchMode,
            ExpandedGalleryStretchMode,
            ExpandedGalleryDisplayMode,
            DockPosition,
            static (dockedSize, expandedSize, dockedStretch, expandedStretch, displayMode, dock) =>
                new GalleryLook(dockedSize, expandedSize, dockedStretch, expandedStretch, displayMode, dock));

    /// <summary>The settings of a window opened from this one's window: the same values, started by its own window.</summary>
    public WindowGallerySettings Copy()
    {
        var values = new GalleryRecord();
        StoreTo(values);
        return new WindowGallerySettings(values);
    }

    /// <summary>Writes the eight values; the group's other settings are app-wide and stay as they are.</summary>
    public void StoreTo(GalleryRecord record)
    {
        record.DockPosition = DockPosition.Value;
        record.IsGalleryDocked = StartsDocked.Value;
        record.ExpandedGalleryDisplayMode = ExpandedGalleryDisplayMode.Value;
        record.RowsWasLastUsedDisplayMode = _rowsWasLastUsedDisplayMode;
        record.DockedGalleryStretchMode = DockedGalleryStretchMode.Value;
        record.ExpandedGalleryStretchMode = ExpandedGalleryStretchMode.Value;
        record.DockedGalleryItemSize = DockedGalleryItemSize.Value;
        record.ExpandedGalleryItemSize = ExpandedGalleryItemSize.Value;
    }

    /// <summary>
    /// Starts the commands' subscriptions. Idempotent and safe from any thread; a concurrent caller returns only after
    /// the subscriptions exist.
    /// </summary>
    public void Start()
    {
        lock (_startLock)
        {
            if (_isStarted || _isDisposed)
            {
                return;
            }

            // The follower of the mode subscribes first, so a later subscriber sees RowsWasLastUsedDisplayMode up to date
            ExpandedGalleryDisplayMode.Skip(1).Subscribe(mode =>
            {
                if (GalleryDisplayModes.ShowsTiles(mode))
                {
                    _rowsWasLastUsedDisplayMode = mode is GalleryDisplayMode.Rows;
                }
            }, DebugHelper.LogError(nameof(WindowGallerySettings), nameof(ExpandedGalleryDisplayMode)));

            // A size outside its limits is not accepted: the slider is put back on the accepted one
            DockedGalleryItemSize.Skip(1).Subscribe(size =>
            {
                var accepted = AcceptedDockedSize(size);
                if (Math.Abs(accepted - size) > 0.001)
                {
                    DockedGalleryItemSize.Value = accepted;
                }
            }, DebugHelper.LogError(nameof(WindowGallerySettings), nameof(DockedGalleryItemSize)));

            ExpandedGalleryItemSize.Skip(1).Subscribe(size =>
            {
                var accepted = AcceptedExpandedSize(size);
                if (Math.Abs(accepted - size) > 0.001)
                {
                    ExpandedGalleryItemSize.Value = accepted;
                }
            }, DebugHelper.LogError(nameof(WindowGallerySettings), nameof(ExpandedGalleryItemSize)));

            SetExpandedDisplayModeCommand.Subscribe(mode =>
            {
                ExpandedGalleryDisplayMode.Value = GalleryDisplayModes.Validate(mode);
            }, DebugHelper.LogError(nameof(WindowGallerySettings), nameof(SetExpandedDisplayModeCommand)));

            ToggleExpandedDisplayModeCommand.Subscribe(_ =>
            {
                ExpandedGalleryDisplayMode.Value = GalleryDisplayModes.Toggle(ExpandedGalleryDisplayMode.Value,
                    _rowsWasLastUsedDisplayMode);
            }, DebugHelper.LogError(nameof(WindowGallerySettings), nameof(ToggleExpandedDisplayModeCommand)));

            SetDockedStretchModeCommand.Subscribe(mode =>
            {
                if (Enum.IsDefined(mode))
                {
                    DockedGalleryStretchMode.Value = mode;
                }
            }, DebugHelper.LogError(nameof(WindowGallerySettings), nameof(SetDockedStretchModeCommand)));

            SetExpandedStretchModeCommand.Subscribe(mode =>
            {
                if (Enum.IsDefined(mode))
                {
                    ExpandedGalleryStretchMode.Value = mode;
                }
            }, DebugHelper.LogError(nameof(WindowGallerySettings), nameof(SetExpandedStretchModeCommand)));

            _isStarted = true;
        }
    }

    private static double AcceptedDockedSize(double size) =>
        double.IsNaN(size)
            ? GalleryDefaults.MinDockedGalleryItemHeight
            : Math.Clamp(size, GalleryDefaults.MinDockedGalleryItemHeight, GalleryDefaults.MaxDockedGalleryItemHeight);

    private static double AcceptedExpandedSize(double size) =>
        double.IsNaN(size)
            ? GalleryDefaults.MinExpandedGalleryItemHeight
            : Math.Clamp(size, GalleryDefaults.MinExpandedGalleryItemHeight, GalleryDefaults.MaxExpandedGalleryItemHeight);

    public void Dispose()
    {
        lock (_startLock)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
        }

        SetExpandedDisplayModeCommand.Dispose();
        ToggleExpandedDisplayModeCommand.Dispose();
        SetDockedStretchModeCommand.Dispose();
        SetExpandedStretchModeCommand.Dispose();
        IsExpandedDisplayModeGrid.Dispose();
        IsExpandedDisplayModeMasonry.Dispose();
        IsExpandedDisplayModeRows.Dispose();
        IsExpandedDisplayModeTiles.Dispose();
        IsDockedStretchUniform.Dispose();
        IsDockedStretchUniformToFill.Dispose();
        IsDockedStretchSquare.Dispose();
        IsDockedStretchSquareFill.Dispose();
        IsExpandedStretchUniform.Dispose();
        IsExpandedStretchUniformToFill.Dispose();
        IsExpandedStretchSquare.Dispose();
        IsExpandedStretchSquareFill.Dispose();
        DockPosition.Dispose();
        StartsDocked.Dispose();
        ExpandedGalleryDisplayMode.Dispose();
        DockedGalleryStretchMode.Dispose();
        ExpandedGalleryStretchMode.Dispose();
        DockedGalleryItemSize.Dispose();
        ExpandedGalleryItemSize.Dispose();
    }
}
