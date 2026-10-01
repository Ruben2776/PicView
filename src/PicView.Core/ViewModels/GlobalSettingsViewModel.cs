using PicView.Core.DebugTools;
using R3;

namespace PicView.Core.ViewModels;

public class GlobalSettingsViewModel
{
    private bool _isInitialized;
    public BindableReactiveProperty<bool> IsIncludingSubdirectories { get; } =
        new(Settings.Sorting.IncludeSubDirectories);

    public BindableReactiveProperty<bool> IsLooping { get; } = new(Settings.UIProperties.Looping);

    public BindableReactiveProperty<bool> IsFileHistoryEnabled { get; } = new(Settings.Navigation.IsFileHistoryEnabled);

    public BindableReactiveProperty<bool> IsShowingTaskbarProgress { get; } = new(Settings.UIProperties.IsTaskbarProgressEnabled);
    
    public BindableReactiveProperty<bool> ShowSetAsWallpaper { get; } = new(Settings.UIProperties.ShowSetAsWallpaper);
    
    public BindableReactiveProperty<object?> ImageBackground { get; } = new();
    
    public BindableReactiveProperty<object?> ConstrainedImageBackground { get; } = new();
    
    public BindableReactiveProperty<int> BackgroundChoice { get; } = new();
    
    public BindableReactiveProperty<int> MouseWheelBehavior { get; } = new(Settings.Zoom.CtrlZoom ? 0 : 1);
    public BindableReactiveProperty<bool> CtrlZoom { get; } = new(Settings.Zoom.CtrlZoom);
    
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;
        
        Observable.EveryValueChanged(this, x => x.MouseWheelBehavior.CurrentValue)
            .SubscribeAwait(async (x, _) =>
            {
                var ctrlZoom = x == 0;
                Settings.Zoom.CtrlZoom = ctrlZoom;
                if (CtrlZoom.Value != ctrlZoom) CtrlZoom.Value = ctrlZoom;
                await SaveSettingsAsync().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(GlobalSettingsViewModel), nameof(MouseWheelBehavior)));

        Observable.EveryValueChanged(this, x => x.MouseWheelBehavior.CurrentValue)
            .SubscribeAwait(async (x, _) =>
            {
                var ctrlZoom = x == 0;
                Settings.Zoom.CtrlZoom = ctrlZoom;
                if (CtrlZoom.Value != ctrlZoom) CtrlZoom.Value = ctrlZoom;
                await SaveSettingsAsync().ConfigureAwait(false);
            }, DebugHelper.LogError(nameof(SettingsViewModel), nameof(MouseWheelBehavior)));
    }
}