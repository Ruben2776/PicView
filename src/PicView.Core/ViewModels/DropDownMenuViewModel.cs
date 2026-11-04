using R3;

namespace PicView.Core.ViewModels;

public class DropDownMenuViewModel : IDisposable
{
    public BindableReactiveProperty<bool> IsFileMenuVisible { get; } = new(false);
    public BindableReactiveProperty<bool> IsSettingsMenuVisible { get; } = new(false);
    public BindableReactiveProperty<bool> IsDropDownMenuVisible { get; } = new(false);
    public BindableReactiveProperty<bool> IsExpandedOptionsOpened { get; } = new(false);
    
    public BindableReactiveProperty<int> GalleryCarouselIndex { get; } = new(0);
    public BindableReactiveProperty<bool> IsGalleryCarouselVisible { get; } = new(true);

    public BindableReactiveProperty<int> SlideshowCarouselIndex { get; } = new(0);
    public BindableReactiveProperty<bool> IsSlideshowCarouselVisible { get; } = new(true);
    
    public BindableReactiveProperty<int> SettingsCarouselIndex { get; } = new(0);
    public BindableReactiveProperty<bool> IsSettingsCarouselVisible { get; } = new(true);
    
    public IReadOnlyBindableReactiveProperty<bool> IsFileHistoryVisible { get; }
    
    public BindableReactiveProperty<bool> IsExtraButtonsVisible { get; } = new(true);
    
    public ReactiveCommand CloseMenuCommand { get; }
    
    public DropDownMenuViewModel(GlobalSettingsViewModel? settings = null)
    {
        IsFileHistoryVisible = (settings?.IsFileHistoryEnabled.AsObservable() ?? Observable.Return(false))
            .CombineLatest(IsExpandedOptionsOpened, static (on, open) => on && !open)
            .ToReadOnlyBindableReactiveProperty();
        CloseMenuCommand = new ReactiveCommand(CloseMenus);
    }

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
    
    public void CloseMenus() => CloseMenus(Unit.Default);

    public void CloseMenus(Unit unit)
    {
        IsFileMenuVisible.Value = false;
        IsSettingsMenuVisible.Value = false;
    }

    private const int DefaultDelay = 95;
    public void OpenGalleryOptions()
    {
        GalleryCarouselIndex.Value = 1;
        SlideshowCarouselIndex.Value = 0;
        SettingsCarouselIndex.Value = 0;
        IsGalleryCarouselVisible.Value = true;
        IsExpandedOptionsOpened.Value = true;
        IsSlideshowCarouselVisible.Value = false;
        IsSettingsCarouselVisible.Value = false;
        IsExtraButtonsVisible.Value = false;
    }

    public async ValueTask CloseGalleryOptions()
    {
        GalleryCarouselIndex.Value = 0;
        await CloseCarousel().ConfigureAwait(false);
        IsExtraButtonsVisible.Value = true;
    }

    public void OpenSlideshowOptions()
    {
        SlideshowCarouselIndex.Value = 1;
        GalleryCarouselIndex.Value = 0;
        SettingsCarouselIndex.Value = 0;
        IsExpandedOptionsOpened.Value = true;
        IsSlideshowCarouselVisible.Value = true;
        IsGalleryCarouselVisible.Value = false;
        IsSettingsCarouselVisible.Value = false;
        IsExtraButtonsVisible.Value = false;
    }

    public async ValueTask CloseSlideshowOptions()
    {
        SlideshowCarouselIndex.Value = 0;
        await CloseCarousel().ConfigureAwait(false);
        IsExtraButtonsVisible.Value = true;
    }

    public async ValueTask CloseCarousel()
    {
        await Task.Delay(DefaultDelay).ConfigureAwait(false);
        IsExpandedOptionsOpened.Value = false;
        IsGalleryCarouselVisible.Value = true;
        IsSlideshowCarouselVisible.Value = true;
        IsSettingsCarouselVisible.Value = true;
        IsExtraButtonsVisible.Value = true;
    }

    public void CloseToDefault()
    {
        SlideshowCarouselIndex.Value = 0;
        GalleryCarouselIndex.Value = 0;
        SettingsCarouselIndex.Value = 0;
        IsExpandedOptionsOpened.Value = false;
        IsGalleryCarouselVisible.Value = true;
        IsSlideshowCarouselVisible.Value = true;
        IsSettingsCarouselVisible.Value = true;
        IsExtraButtonsVisible.Value = true;
    }
    
    public void OpenSettingsOptions()
    {
        SettingsCarouselIndex.Value = 1;
        GalleryCarouselIndex.Value = 0;
        SlideshowCarouselIndex.Value = 0;
        IsExpandedOptionsOpened.Value = true;
        IsGalleryCarouselVisible.Value = false;
        IsSlideshowCarouselVisible.Value = false;
        IsSettingsCarouselVisible.Value = true;
        IsExtraButtonsVisible.Value = false;
    }
    
    public async ValueTask CloseSettingsOptions()
    {
        SettingsCarouselIndex.Value = 0;
        await CloseCarousel().ConfigureAwait(false);
    }
    
    public void Dispose()
    {
        IsFileMenuVisible.Dispose();
        IsSettingsMenuVisible.Dispose();
        CloseMenuCommand.Dispose();
        IsFileHistoryVisible.Dispose();
        GC.SuppressFinalize(this);
    }

}