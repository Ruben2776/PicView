using R3;

namespace PicView.Core.ViewModels;

public class DropDownMenuViewModel : IDisposable
{
    public DropDownMenuViewModel()
    {
        CloseMenuCommand = new ReactiveCommand(CloseMenus);
    }

    public BindableReactiveProperty<bool> IsFileMenuVisible { get; } = new(false);
    public BindableReactiveProperty<bool> IsSettingsMenuVisible { get; } = new(false);
    public ReactiveCommand CloseMenuCommand { get; }

    public void ToggleFileMenu()
    {
        IsFileMenuVisible.Value = !IsFileMenuVisible.Value;
        IsSettingsMenuVisible.Value = false;
        IsDropDownMenuVisible.Value = false;
    }

    public void ToggleSettingsMenu()
    {
        IsSettingsMenuVisible.Value = !IsSettingsMenuVisible.Value;
        IsFileMenuVisible.Value = false;
        IsDropDownMenuVisible.Value = false;
    }
    
    public void CloseMenus(Unit unit)
    {
        IsFileMenuVisible.Value = false;
        IsSettingsMenuVisible.Value = false;
    }
    
    public BindableReactiveProperty<bool> IsDropDownMenuVisible { get; } = new(false);
    public BindableReactiveProperty<bool> IsExpandedOptionsOpened { get; } = new(false);
    
    public BindableReactiveProperty<int> GalleryCarouselIndex { get; } = new(0);
    public BindableReactiveProperty<bool> IsGalleryCarouselVisible { get; } = new(true);

    public BindableReactiveProperty<int> SlideshowCarouselIndex { get; } = new(0);
    public BindableReactiveProperty<bool> IsSlideshowCarouselVisible { get; } = new(true);
    
    public BindableReactiveProperty<int> ToolWindowsCarouselIndex { get; } = new(0);
    public BindableReactiveProperty<bool> IsToolWindowsCarouselVisible { get; } = new(true);
    
    public BindableReactiveProperty<int> SettingsCarouselIndex { get; } = new(0);
    public BindableReactiveProperty<bool> IsSettingsCarouselVisible { get; } = new(true);
    
    public BindableReactiveProperty<bool> IsFileHistoryVisible { get; } = new(false);

    private const int DefaultDelay = 95;
    public void OpenGalleryOptions()
    {
        GalleryCarouselIndex.Value = 1;
        SlideshowCarouselIndex.Value = 0;
        ToolWindowsCarouselIndex.Value = 0;
        SettingsCarouselIndex.Value = 0;
        IsGalleryCarouselVisible.Value = true;
        IsExpandedOptionsOpened.Value = true;
        IsSlideshowCarouselVisible.Value = false;
        IsToolWindowsCarouselVisible.Value = false;
        IsSettingsCarouselVisible.Value = false;
        IsFileHistoryVisible.Value = false;
    }

    public async ValueTask CloseGalleryOptions()
    {
        GalleryCarouselIndex.Value = 0;
        await CloseCarousel();
        IsFileHistoryVisible.Value = Settings.Navigation.IsFileHistoryEnabled;
    }

    public void OpenSlideshowOptions()
    {
        SlideshowCarouselIndex.Value = 1;
        GalleryCarouselIndex.Value = 0;
        ToolWindowsCarouselIndex.Value = 0;
        SettingsCarouselIndex.Value = 0;
        IsExpandedOptionsOpened.Value = true;
        IsSlideshowCarouselVisible.Value = true;
        IsGalleryCarouselVisible.Value = false;
        IsToolWindowsCarouselVisible.Value = false;
        IsSettingsCarouselVisible.Value = false;
        IsFileHistoryVisible.Value = false;
    }

    public async ValueTask CloseSlideshowOptions()
    {
        SlideshowCarouselIndex.Value = 0;
        await CloseCarousel();
        IsFileHistoryVisible.Value = Settings.Navigation.IsFileHistoryEnabled;
    }

    public async ValueTask CloseCarousel()
    {
        await Task.Delay(DefaultDelay);
        IsExpandedOptionsOpened.Value = false;
        IsGalleryCarouselVisible.Value = true;
        IsSlideshowCarouselVisible.Value = true;
        IsToolWindowsCarouselVisible.Value = true;
        IsSettingsCarouselVisible.Value = true;
        IsFileHistoryVisible.Value = Settings.Navigation.IsFileHistoryEnabled;
    }

    public void CloseToDefault()
    {
        SlideshowCarouselIndex.Value = 0;
        GalleryCarouselIndex.Value = 0;
        ToolWindowsCarouselIndex.Value = 0;
        SettingsCarouselIndex.Value = 0;
        IsExpandedOptionsOpened.Value = false;
        IsGalleryCarouselVisible.Value = true;
        IsSlideshowCarouselVisible.Value = true;
        IsToolWindowsCarouselVisible.Value = true;
        IsSettingsCarouselVisible.Value = true;
        IsFileHistoryVisible.Value = Settings.Navigation.IsFileHistoryEnabled;
    }
    
    public void OpenToolWindowsOptions()
    {
        ToolWindowsCarouselIndex.Value = 1;
        GalleryCarouselIndex.Value = 0;
        SlideshowCarouselIndex.Value = 0;
        SettingsCarouselIndex.Value = 0;
        IsExpandedOptionsOpened.Value = true;
        IsToolWindowsCarouselVisible.Value = true;
        IsGalleryCarouselVisible.Value = false;
        IsSlideshowCarouselVisible.Value = false;
        IsSettingsCarouselVisible.Value = false;
        IsFileHistoryVisible.Value = false;
    }
    
    public async ValueTask CloseToolWindowsOptions()
    {
        ToolWindowsCarouselIndex.Value = 0;
        await CloseCarousel();
    }
    
    public void OpenSettingsOptions()
    {
        SettingsCarouselIndex.Value = 1;
        GalleryCarouselIndex.Value = 0;
        SlideshowCarouselIndex.Value = 0;
        IsExpandedOptionsOpened.Value = true;
        IsToolWindowsCarouselVisible.Value = false;
        IsGalleryCarouselVisible.Value = false;
        IsSlideshowCarouselVisible.Value = false;
        IsSettingsCarouselVisible.Value = true;
        IsFileHistoryVisible.Value = false;
    }
    
    public async ValueTask CloseSettingsOptions()
    {
        SettingsCarouselIndex.Value = 0;
        await CloseCarousel();
    }
    
    public void Dispose()
    {
        IsFileMenuVisible.Dispose();
        IsSettingsMenuVisible.Dispose();
        CloseMenuCommand.Dispose();
        GC.SuppressFinalize(this);
    }

}