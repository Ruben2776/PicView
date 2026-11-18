using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// The gallery settings every tab shares publish their changes (ADR-0020): each is a bindable over its stored value,
/// and its write-through is the one writer of that stored value, followed by one coalesced save. The constructor
/// subscribes only the derived flags; the write-throughs and the commands start in <see cref="Start"/>, so a change
/// before it writes and saves nothing (ADR-0034).
/// </summary>
public class GallerySharedSettingsViewModel
{
    private readonly Lock _startLock = new();
    private bool _isStarted;
    private readonly CoalescedSave _coalescedSave;

    /// <param name="save">Saves the settings; null means <see cref="SettingsManager.SaveSettingsAsync"/>.</param>
    public GallerySharedSettingsViewModel(Func<ValueTask<bool>>? save = null)
    {
        _coalescedSave = new CoalescedSave(save ?? SaveSettingsAsync, nameof(GallerySharedSettingsViewModel));

        ExpandedGalleryDisplayMode =
            new BindableReactiveProperty<GalleryDisplayMode>(Settings.Gallery.ExpandedGalleryDisplayMode);
        DockedGalleryStretchMode = new BindableReactiveProperty<GalleryStretchMode>(Settings.Gallery.DockedGalleryStretchMode);
        ExpandedGalleryStretchMode =
            new BindableReactiveProperty<GalleryStretchMode>(Settings.Gallery.ExpandedGalleryStretchMode);
        DockedGalleryItemSize = new BindableReactiveProperty<double>(Settings.Gallery.DockedGalleryItemSize);
        ExpandedGalleryItemSize = new BindableReactiveProperty<double>(Settings.Gallery.ExpandedGalleryItemSize);
        GalleryItemSpacing = new BindableReactiveProperty<double>(Settings.Gallery.ItemSpacing);
        GalleryLineSpacing = new BindableReactiveProperty<double>(Settings.Gallery.LineSpacing);
        ShowMotionPhotoBadges = new BindableReactiveProperty<bool>(Settings.Gallery.ShowMotionPhotoBadges);

        IsExpandedDisplayModeColumns = ExpandedGalleryDisplayMode
            .Select(static mode => mode is GalleryDisplayMode.Columns)
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

    /// <summary>The save in flight, completed when idle. For tests.</summary>
    internal Task Saving => _coalescedSave.Saving;

    /// <summary>
    /// The Expanded Gallery's display mode, app-wide. Only the two display mode commands write it, and it writes
    /// through to the settings record and saves (ADR-0020).
    /// </summary>
    public BindableReactiveProperty<GalleryDisplayMode> ExpandedGalleryDisplayMode { get; }

    /// <summary>Sets the display mode; an unknown value sets Columns.</summary>
    public ReactiveCommand<GalleryDisplayMode> SetExpandedDisplayModeCommand { get; } = new();

    /// <summary>Goes from Columns to the last used tile mode, and from a tile mode back to Columns.</summary>
    public ReactiveCommand<Unit> ToggleExpandedDisplayModeCommand { get; } = new();

    public IReadOnlyBindableReactiveProperty<bool> IsExpandedDisplayModeColumns { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsExpandedDisplayModeMasonry { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsExpandedDisplayModeRows { get; }

    /// <summary>Whether the display mode shows tiles, which is Masonry and Rows.</summary>
    public IReadOnlyBindableReactiveProperty<bool> IsExpandedDisplayModeTiles { get; }

    /// <summary>Sets the Docked Gallery's Thumbnail Stretch; an unknown value is ignored.</summary>
    public ReactiveCommand<GalleryStretchMode> SetDockedStretchModeCommand { get; } = new();

    /// <summary>Sets the Expanded Gallery's Thumbnail Stretch; an unknown value is ignored.</summary>
    public ReactiveCommand<GalleryStretchMode> SetExpandedStretchModeCommand { get; } = new();

    public IReadOnlyBindableReactiveProperty<bool> IsDockedStretchUniform { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsDockedStretchUniformToFill { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsDockedStretchSquare { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsDockedStretchSquareFill { get; }

    public IReadOnlyBindableReactiveProperty<bool> IsExpandedStretchUniform { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsExpandedStretchUniformToFill { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsExpandedStretchSquare { get; }
    public IReadOnlyBindableReactiveProperty<bool> IsExpandedStretchSquareFill { get; }

    public BindableReactiveProperty<bool> ShowMotionPhotoBadges { get; }

    public BindableReactiveProperty<double> DockedGalleryItemSize { get; }

    public BindableReactiveProperty<double> DockedGalleryMaxItemSize { get; } =
        new(GalleryDefaults.MaxDockedGalleryItemHeight);

    public BindableReactiveProperty<double> DockedGalleryMinItemSize { get; } =
        new(GalleryDefaults.MinDockedGalleryItemHeight);

    public BindableReactiveProperty<double> ExpandedGalleryItemSize { get; }

    public BindableReactiveProperty<double> ExpandedGalleryMaxItemSize { get; } =
        new(GalleryDefaults.MaxExpandedGalleryItemHeight);

    public BindableReactiveProperty<double> ExpandedGalleryMinItemSize { get; } =
        new(GalleryDefaults.MinExpandedGalleryItemHeight);

    public BindableReactiveProperty<double> GalleryItemSpacing { get; }
    public BindableReactiveProperty<double> GalleryLineSpacing { get; }

    public BindableReactiveProperty<GalleryStretchMode> DockedGalleryStretchMode { get; }

    public BindableReactiveProperty<GalleryStretchMode> ExpandedGalleryStretchMode { get; }

    /// <summary>
    /// Starts the write-throughs of the gallery settings and their commands. Idempotent and safe from any thread; a
    /// concurrent caller returns only after the subscriptions exist.
    /// </summary>
    public void Start()
    {
        lock (_startLock)
        {
            if (_isStarted)
            {
                return;
            }

            // The write-throughs subscribe first, so a later subscriber that reads the record sees the new value (ADR-0020)
            ExpandedGalleryDisplayMode.Skip(1).Subscribe(mode =>
            {
                Settings.Gallery.ExpandedGalleryDisplayMode = mode;
                if (GalleryDisplayModes.ShowsTiles(mode))
                {
                    Settings.Gallery.RowsWasLastUsedDisplayMode = mode is GalleryDisplayMode.Rows;
                }

                _coalescedSave.Soon();
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(ExpandedGalleryDisplayMode)));

            DockedGalleryStretchMode.Skip(1).Subscribe(mode =>
            {
                Settings.Gallery.DockedGalleryStretchMode = mode;
                _coalescedSave.Soon();
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(DockedGalleryStretchMode)));

            ExpandedGalleryStretchMode.Skip(1).Subscribe(mode =>
            {
                Settings.Gallery.ExpandedGalleryStretchMode = mode;
                _coalescedSave.Soon();
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(ExpandedGalleryStretchMode)));

            DockedGalleryItemSize.Skip(1).Subscribe(size =>
            {
                Settings.Gallery.DockedGalleryItemSize = size;
                _coalescedSave.Soon();
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(DockedGalleryItemSize)));

            ExpandedGalleryItemSize.Skip(1).Subscribe(size =>
            {
                Settings.Gallery.ExpandedGalleryItemSize = size;
                _coalescedSave.Soon();
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(ExpandedGalleryItemSize)));

            GalleryItemSpacing.Skip(1).Subscribe(spacing =>
            {
                // A spacing below the minimum is not accepted: the slider is put back on the accepted one
                var accepted = GalleryDefaults.AcceptedItemSpacing(spacing);
                if (Math.Abs(accepted - spacing) > 0.001)
                {
                    GalleryItemSpacing.Value = accepted;
                }

                if (Math.Abs(Settings.Gallery.ItemSpacing - accepted) > 0.001)
                {
                    Settings.Gallery.ItemSpacing = accepted;
                    _coalescedSave.Soon();
                }
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(GalleryItemSpacing)));

            GalleryLineSpacing.Skip(1).Subscribe(spacing =>
            {
                // A spacing below the minimum is not accepted: the slider is put back on the accepted one
                var accepted = GalleryDefaults.AcceptedLineSpacing(spacing);
                if (Math.Abs(accepted - spacing) > 0.001)
                {
                    GalleryLineSpacing.Value = accepted;
                }

                if (Math.Abs(Settings.Gallery.LineSpacing - accepted) > 0.001)
                {
                    Settings.Gallery.LineSpacing = accepted;
                    _coalescedSave.Soon();
                }
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(GalleryLineSpacing)));

            ShowMotionPhotoBadges.Skip(1).Subscribe(show =>
            {
                if (Settings.Gallery.ShowMotionPhotoBadges != show)
                {
                    Settings.Gallery.ShowMotionPhotoBadges = show;
                    _coalescedSave.Soon();
                }
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(ShowMotionPhotoBadges)));

            SetExpandedDisplayModeCommand.Subscribe(mode =>
            {
                ExpandedGalleryDisplayMode.Value = GalleryDisplayModes.Validate(mode);
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(SetExpandedDisplayModeCommand)));

            ToggleExpandedDisplayModeCommand.Subscribe(_ =>
            {
                ExpandedGalleryDisplayMode.Value = GalleryDisplayModes.Toggle(ExpandedGalleryDisplayMode.Value,
                    Settings.Gallery.RowsWasLastUsedDisplayMode);
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(ToggleExpandedDisplayModeCommand)));

            SetDockedStretchModeCommand.Subscribe(mode =>
            {
                if (Enum.IsDefined(mode))
                {
                    DockedGalleryStretchMode.Value = mode;
                }
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(SetDockedStretchModeCommand)));

            SetExpandedStretchModeCommand.Subscribe(mode =>
            {
                if (Enum.IsDefined(mode))
                {
                    ExpandedGalleryStretchMode.Value = mode;
                }
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(SetExpandedStretchModeCommand)));

            _isStarted = true;
        }
    }
}
