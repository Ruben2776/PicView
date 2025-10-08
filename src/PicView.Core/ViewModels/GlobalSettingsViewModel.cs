using R3;

namespace PicView.Core.ViewModels;

public class GlobalSettingsViewModel
{
    public BindableReactiveProperty<bool> IsTopMost { get; } = new(Settings.WindowProperties.TopMost);

    public BindableReactiveProperty<bool> IsIncludingSubdirectories { get; } =
        new(Settings.Sorting.IncludeSubDirectories);

    public BindableReactiveProperty<bool> IsScrollingEnabled { get; } = new();

    public BindableReactiveProperty<bool> IsStretched { get; } = new(Settings.ImageScaling.StretchImage);

    public BindableReactiveProperty<bool> IsLooping { get; } = new(Settings.UIProperties.Looping);

    public BindableReactiveProperty<bool> IsAutoFit { get; } = new(Settings.WindowProperties.AutoFit);

    public BindableReactiveProperty<bool> IsFileHistoryEnabled { get; } = new(Settings.Navigation.IsFileHistoryEnabled);

    public BindableReactiveProperty<bool> IsShowingTaskbarProgress { get; } = new(Settings.UIProperties.IsTaskbarProgressEnabled);
    
    public BindableReactiveProperty<bool> ShowSetAsWallpaper { get; } = new(Settings.UIProperties.ShowSetAsWallpaper);
}