using System.Diagnostics;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using R3;

namespace PicView.Core.ViewModels;

public class GallerySharedSettingsViewModel
{
    private bool _isInitialized;

    public BindableReactiveProperty<double> ItemHeight { get; } = new(0);
    public BindableReactiveProperty<double> ItemWidth { get; } = new(0);

    public BindableReactiveProperty<object> GalleryStretch { get; } = new();
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

    public BindableReactiveProperty<bool> IsDockedGalleryShownInHiddenUI { get; } =
        new(Settings.Gallery.ShowDockedGalleryInHiddenUI);

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
                DockedGalleryStretchMode.Value = x;
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(DockedGalleryStretchMode)));

        Observable.EveryValueChanged(Settings.Gallery, x => x.ExpandedGalleryStretchMode)
            .Subscribe(x =>
            {
                ExpandedGalleryStretchMode.Value = x;
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
                if (Math.Abs(Settings.Gallery.ItemSpacing - x) > 0.001)
                {
                    Settings.Gallery.ItemSpacing = x;
                    await SaveSettingsAsync().ConfigureAwait(false);
                }
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(GalleryItemSpacing)));

        GalleryLineSpacing
            .Skip(1)
            .SubscribeAwait(async (x, _) =>
            {
                if (Math.Abs(Settings.Gallery.LineSpacing - x) > 0.001)
                {
                    Settings.Gallery.LineSpacing = x;
                    await SaveSettingsAsync().ConfigureAwait(false);
                }
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(GalleryLineSpacing)));

        DockedGalleryStretchMode
            .Skip(1)
            .SubscribeAwait(async (newMode, _) =>
            {
                if (Settings.Gallery.DockedGalleryStretchMode == newMode)
                {
                    return;
                }

                GallerySettingsConverter.UpdateDockedGalleryStretchMode(this, newMode);
                await SaveSettingsAsync().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(DockedGalleryStretchMode)));

        ExpandedGalleryStretchMode
            .Skip(1)
            .SubscribeAwait(async (newMode, _) =>
            {
                if (Settings.Gallery.ExpandedGalleryStretchMode == newMode)
                {
                    return;
                }
                
                GallerySettingsConverter.UpdateExpandedGalleryStretchMode(this, newMode);
                await SaveSettingsAsync().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(GallerySharedSettingsViewModel), nameof(ExpandedGalleryStretchMode)));

    }

    private void ToggleGalleryVisibilitySubscription()
    {
#if DEBUG
        Debug.Assert(Settings?.Gallery is not null);
#endif

        IsDockedGalleryShownInHiddenUI
            .Skip(1)
            .SubscribeAwait(async (x, _) =>
            {
                if (Settings.Gallery.ShowDockedGalleryInHiddenUI != x)
                {
                    Settings.Gallery.ShowDockedGalleryInHiddenUI = x;
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

        Observable.EveryValueChanged(Settings.Gallery, x => x.ShowDockedGalleryInHiddenUI)
            .Subscribe(x => { IsDockedGalleryShownInHiddenUI.Value = x; }, result =>
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