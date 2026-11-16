using System.Diagnostics;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using R3;

namespace PicView.Core.ViewModels;

public class GallerySharedSettingsViewModel
{
    private readonly Func<ValueTask<bool>> _save;
    private Task _saving = Task.CompletedTask;
    private bool _isInitialized;

    /// <param name="save">Saves the settings; null means <see cref="SettingsManager.SaveSettingsAsync"/>.</param>
    public GallerySharedSettingsViewModel(Func<ValueTask<bool>>? save = null)
    {
        _save = save ?? SaveSettingsAsync;
        ExpandedGalleryDisplayMode =
            new BindableReactiveProperty<GalleryDisplayMode>(Settings.Gallery.ExpandedGalleryDisplayMode);

        // The write-through subscribes first, so a later subscriber that reads the record sees the new value (ADR-0020)
        ExpandedGalleryDisplayMode.Skip(1).Subscribe(mode =>
        {
            Settings.Gallery.ExpandedGalleryDisplayMode = mode;
            if (GalleryDisplayModes.ShowsTiles(mode))
            {
                Settings.Gallery.RowsWasLastUsedDisplayMode = mode is GalleryDisplayMode.Rows;
            }

            _saving = SaveAsync();
        }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(ExpandedGalleryDisplayMode)));

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

        SetExpandedDisplayModeCommand.Subscribe(mode =>
        {
            ExpandedGalleryDisplayMode.Value = GalleryDisplayModes.Validate(mode);
        }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(SetExpandedDisplayModeCommand)));

        ToggleExpandedDisplayModeCommand.Subscribe(_ =>
        {
            ExpandedGalleryDisplayMode.Value = GalleryDisplayModes.Toggle(ExpandedGalleryDisplayMode.Value,
                Settings.Gallery.RowsWasLastUsedDisplayMode);
        }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(ToggleExpandedDisplayModeCommand)));
    }

    /// <summary>The save in flight, completed when idle. For tests.</summary>
    internal Task Saving => _saving;

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

    public ReactiveCommand<GalleryStretchMode> SetDockedStretchModeCommand { get; } = new();
    public ReactiveCommand<GalleryStretchMode> SetExpandedStretchModeCommand { get; } = new();
    
    public BindableReactiveProperty<bool> IsDockedStretchUniform { get; } =
        new(Settings.Gallery.DockedGalleryStretchMode == GalleryStretchMode.Uniform);
    public BindableReactiveProperty<bool> IsDockedStretchUniformToFill { get; } =
        new(Settings.Gallery.DockedGalleryStretchMode == GalleryStretchMode.UniformToFill);
    public BindableReactiveProperty<bool> IsDockedStretchSquare { get; } =
        new(Settings.Gallery.DockedGalleryStretchMode == GalleryStretchMode.Square);
    public BindableReactiveProperty<bool> IsDockedStretchSquareFill { get; } =
        new(Settings.Gallery.DockedGalleryStretchMode == GalleryStretchMode.FillSquare);
    
    public BindableReactiveProperty<bool> IsExpandedStretchUniform { get; } =
        new(Settings.Gallery.ExpandedGalleryStretchMode == GalleryStretchMode.Uniform);
    public BindableReactiveProperty<bool> IsExpandedStretchUniformToFill { get; } =
        new(Settings.Gallery.ExpandedGalleryStretchMode == GalleryStretchMode.UniformToFill);
    public BindableReactiveProperty<bool> IsExpandedStretchSquare { get; } =
        new(Settings.Gallery.ExpandedGalleryStretchMode == GalleryStretchMode.Square);
    public BindableReactiveProperty<bool> IsExpandedStretchSquareFill { get; } =
        new(Settings.Gallery.ExpandedGalleryStretchMode == GalleryStretchMode.FillSquare);

    public BindableReactiveProperty<bool> ShowMotionPhotoBadges { get; } =
        new(Settings.Gallery.ShowMotionPhotoBadges);

    public BindableReactiveProperty<double> DockedGalleryItemSize { get; } =
        new(Settings.Gallery.DockedGalleryItemSize);

    public BindableReactiveProperty<double> DockedGalleryMaxItemSize { get; } =
        new(GalleryDefaults.MaxDockedGalleryItemHeight);

    public BindableReactiveProperty<double> DockedGalleryMinItemSize { get; } =
        new(GalleryDefaults.MinDockedGalleryItemHeight);

    public BindableReactiveProperty<double> ExpandedGalleryItemSize { get; } =
        new(Settings.Gallery.ExpandedGalleryItemSize);

    public BindableReactiveProperty<double> ExpandedGalleryMaxItemSize { get; } =
        new(GalleryDefaults.MaxExpandedGalleryItemHeight);

    public BindableReactiveProperty<double> ExpandedGalleryMinItemSize { get; } =
        new(GalleryDefaults.MinExpandedGalleryItemHeight);

    public BindableReactiveProperty<double> GalleryItemSpacing { get; } = new(Settings.Gallery.ItemSpacing);
    public BindableReactiveProperty<double> GalleryLineSpacing { get; } = new(Settings.Gallery.LineSpacing);

    public BindableReactiveProperty<GalleryStretchMode> DockedGalleryStretchMode { get; } =
        new(Settings.Gallery.DockedGalleryStretchMode);

    public BindableReactiveProperty<GalleryStretchMode> ExpandedGalleryStretchMode { get; } =
        new(Settings.Gallery.ExpandedGalleryStretchMode);

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
#if DEBUG
        Debug.Assert(Settings?.Gallery is not null);
#endif
        SetDockedStretchModeCommand.Subscribe(mode =>
        {
            GallerySettingsConverter.UpdateDockedGalleryStretchMode(this, mode);
        }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(Initialize)));
        
        SetExpandedStretchModeCommand.Subscribe(mode =>
        {
            GallerySettingsConverter.UpdateExpandedGalleryStretchMode(this, mode);
        }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(Initialize)));
        
        ToggleGalleryVisibilitySubscription();

        Observable.EveryValueChanged(Settings.Gallery, x => x.DockedGalleryItemSize)
            .Subscribe(x =>
            {
                DockedGalleryItemSize.Value = x;
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(Settings.Gallery.DockedGalleryItemSize)));

        Observable.EveryValueChanged(Settings.Gallery, x => x.ExpandedGalleryItemSize)
            .Subscribe(x =>
            {
                ExpandedGalleryItemSize.Value = x;
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(Settings.Gallery.ExpandedGalleryItemSize)));

        Observable.EveryValueChanged(Settings.Gallery, x => x.ItemSpacing)
            .Subscribe(x =>
            {
                GalleryItemSpacing.Value = x;
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(Settings.Gallery.ItemSpacing)));

        Observable.EveryValueChanged(Settings.Gallery, x => x.LineSpacing)
            .Subscribe(x =>
            {
                GalleryLineSpacing.Value = x;
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(Settings.Gallery.LineSpacing)));

        Observable.EveryValueChanged(Settings.Gallery, x => x.DockedGalleryStretchMode)
            .Subscribe(x =>
            {
                GallerySettingsConverter.UpdateDockedGalleryStretchMode(this, x);
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(DockedGalleryStretchMode)));

        Observable.EveryValueChanged(Settings.Gallery, x => x.ExpandedGalleryStretchMode)
            .Subscribe(x =>
            {
                GallerySettingsConverter.UpdateExpandedGalleryStretchMode(this, x);
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(ExpandedGalleryStretchMode)));


        DockedGalleryItemSize
            .Skip(1)
            .SubscribeAwait(async (x, _) =>
            {
                Settings.Gallery.DockedGalleryItemSize = x;
                await SaveSettingsAsync().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(GalleryItemSpacing)));

        ExpandedGalleryItemSize
            .Skip(1)
            .SubscribeAwait(async (x, _) =>
            {
                Settings.Gallery.ExpandedGalleryItemSize = x;
                await SaveSettingsAsync().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(GalleryItemSpacing)));

        GalleryItemSpacing
            .Skip(1)
            .SubscribeAwait(async (x, _) =>
            {
                // A spacing below the minimum is not accepted: the slider is put back on the accepted one
                var accepted = GalleryDefaults.AcceptedItemSpacing(x);
                if (Math.Abs(accepted - x) > 0.001)
                {
                    GalleryItemSpacing.Value = accepted;
                }

                if (Math.Abs(Settings.Gallery.ItemSpacing - accepted) > 0.001)
                {
                    Settings.Gallery.ItemSpacing = accepted;
                    await SaveSettingsAsync().ConfigureAwait(false);
                }
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(GalleryItemSpacing)));

        GalleryLineSpacing
            .Skip(1)
            .SubscribeAwait(async (x, _) =>
            {
                // A spacing below the minimum is not accepted: the slider is put back on the accepted one
                var accepted = GalleryDefaults.AcceptedLineSpacing(x);
                if (Math.Abs(accepted - x) > 0.001)
                {
                    GalleryLineSpacing.Value = accepted;
                }

                if (Math.Abs(Settings.Gallery.LineSpacing - accepted) > 0.001)
                {
                    Settings.Gallery.LineSpacing = accepted;
                    await SaveSettingsAsync().ConfigureAwait(false);
                }
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(GalleryLineSpacing)));
    }

    private async Task SaveAsync()
    {
        try
        {
            await _save().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            DebugHelper.LogDebug(nameof(GallerySharedSettingsViewModel), nameof(SaveAsync), e);
        }
    }

    private void ToggleGalleryVisibilitySubscription()
    {
#if DEBUG
        Debug.Assert(Settings?.Gallery is not null);
#endif

        ShowMotionPhotoBadges
            .Skip(1)
            .SubscribeAwait(async (x, _) =>
            {
                if (Settings.Gallery.ShowMotionPhotoBadges != x)
                {
                    Settings.Gallery.ShowMotionPhotoBadges = x;
                    await SaveSettingsAsync().ConfigureAwait(false);
                }
            }, result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(GallerySharedSettingsViewModel), nameof(Initialize),
                        result.Exception);
                }
#endif
            });

        Observable.EveryValueChanged(Settings.Gallery, x => x.ShowMotionPhotoBadges)
            .Subscribe(x => { ShowMotionPhotoBadges.Value = x; }, result =>
            {
#if DEBUG
                if (result is { IsFailure: true, Exception: not null })
                {
                    DebugHelper.LogDebug(nameof(GallerySharedSettingsViewModel), nameof(Initialize),
                        result.Exception);
                }
#endif
            });
    }
}