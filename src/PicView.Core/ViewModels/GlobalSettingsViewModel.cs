using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using R3;

namespace PicView.Core.ViewModels;

public class GlobalSettingsViewModel
{
    private bool _isExtraSettingsInitialized;
    public BindableReactiveProperty<bool> IsIncludingSubdirectories { get; } =
        new(Settings.Sorting.IncludeSubDirectories);

    public BindableReactiveProperty<bool> IsLooping { get; } = new(Settings.UIProperties.Looping);

    public BindableReactiveProperty<bool> IsFileHistoryEnabled { get; } = new(Settings.Navigation.IsFileHistoryEnabled);
    
    public BindableReactiveProperty<bool> ShowSetAsWallpaper { get; } = new(Settings.UIProperties.ShowSetAsWallpaper);
    
    public BindableReactiveProperty<object?> ImageBackground { get; } = new();
    
    public BindableReactiveProperty<object?> ConstrainedImageBackground { get; } = new();
    
    public BindableReactiveProperty<int> BackgroundChoice { get; } = new();
    
    public BindableReactiveProperty<int> GalleryMouseWheelBehavior { get; } = new((int)Settings.Gallery.GalleryMouseWheelBehavior);
    
    public void InitializeExtraSettings()
    {
        if (_isExtraSettingsInitialized)
        {
            return;
        }
        _isExtraSettingsInitialized = true;
        
        GalleryMouseWheelBehavior
            .Subscribe(x => {
                Settings.Gallery.GalleryMouseWheelBehavior = (GalleryMouseWheel)x;
            }, DebugHelper.LogError(nameof(GlobalSettingsViewModel), nameof(GalleryMouseWheelBehavior)));
    }
}