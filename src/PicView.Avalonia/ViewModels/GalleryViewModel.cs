using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using PicView.Avalonia.Functions;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Gallery;
using R3;

namespace PicView.Avalonia.ViewModels;

public class GalleryViewModel : IDisposable
{
    public GalleryItemViewModel GalleryItem { get; } = new();

    public BindableReactiveProperty<Thickness> GalleryMargin { get; } = new();

    public BindableReactiveProperty<GalleryMode> GalleryMode { get; } = new(Core.Gallery.GalleryMode.Closed);

    public BindableReactiveProperty<Stretch> GalleryStretch { get; } = new();
    public BindableReactiveProperty<VerticalAlignment> GalleryVerticalAlignment { get; } = new();
    public BindableReactiveProperty<Orientation> GalleryOrientation { get; } = new();

    public BindableReactiveProperty<bool> IsGalleryExpanded { get; } = new();
    
    public BindableReactiveProperty<bool> IsBottomGalleryShown { get; } = new(Settings.Gallery.IsBottomGalleryShown);
    public BindableReactiveProperty<bool> IsBottomGalleryShownInHiddenUI { get; } =
        new(Settings.Gallery.ShowBottomGalleryInHiddenUI);
    
    #region Gallery Stretch IsChecked

    public BindableReactiveProperty<bool> IsUniformBottomChecked { get; } = new();

    public BindableReactiveProperty<bool> IsUniformFullChecked { get; } = new();

    public BindableReactiveProperty<bool> IsUniformMenuChecked { get; } = new();

    public BindableReactiveProperty<bool> IsUniformToFillBottomChecked { get; } = new();

    public BindableReactiveProperty<bool> IsUniformToFillFullChecked { get; } = new();

    public BindableReactiveProperty<bool> IsUniformToFillMenuChecked { get; } = new();

    public BindableReactiveProperty<bool> IsFillBottomChecked { get; } = new();

    public BindableReactiveProperty<bool> IsFillFullChecked { get; } = new();

    public BindableReactiveProperty<bool> IsFillMenuChecked { get; } = new();

    public BindableReactiveProperty<bool> IsNoneBottomChecked { get; } = new();

    public BindableReactiveProperty<bool> IsNoneFullChecked { get; } = new();

    public BindableReactiveProperty<bool> IsNoneMenuChecked { get; } = new();

    public BindableReactiveProperty<bool> IsSquareBottomChecked { get; } = new();

    public BindableReactiveProperty<bool> IsSquareFullChecked { get; } = new();

    public BindableReactiveProperty<bool> IsSquareMenuChecked { get; } = new();

    public BindableReactiveProperty<bool> IsFillSquareBottomChecked { get; } = new();

    public BindableReactiveProperty<bool> IsFillSquareFullChecked { get; } = new();

    public BindableReactiveProperty<bool> IsFillSquareMenuChecked { get; } = new();

    #endregion

    #region Commands
    public ReactiveCommand ToggleGalleryCommand { get; } = new(ToggleGallery);
    public ReactiveCommand ToggleBottomGalleryCommand { get; } = new(ToggleBottomGallery);
    public ReactiveCommand CloseGalleryCommand { get; } = new(CloseGallery);
    public ReactiveCommand<string> GalleryItemStretchCommand { get; } = new(GalleryItemStretch);

    private static void ToggleGallery(Unit unit) => FunctionsMapper.ToggleGallery();
    private static void ToggleBottomGallery(Unit unit) => FunctionsMapper.OpenCloseBottomGallery();
    private static void CloseGallery(Unit unit) => FunctionsMapper.CloseGallery();
    private static void GalleryItemStretch(string value) => GalleryHelper.SetGalleryItemStretch(value);
    
    #endregion

    public void GalleryItemSizeUpdateSubscription(MainViewModel vm)
    {
        Observable.EveryValueChanged(GalleryItem.ItemHeight, x => x.Value)
            .Skip(1)
            .SubscribeAwait(async (x,_) =>
            {
                await GalleryItemSizeChangedAsync(vm, x);
            });
    }
    
    private static async Task GalleryItemSizeChangedAsync(MainViewModel vm, double newValue)
    {
        if (GalleryFunctions.IsFullGalleryOpen)
        {
            await ExpandedGalleryItemSizeChangedAsync(vm,  newValue);
        }
        else
        {
            await BottomGalleryItemSizeChangedAsync(vm, newValue);
        }
    }

    private static async Task ExpandedGalleryItemSizeChangedAsync(MainViewModel vm, double newValue)
    {
        if (vm.Gallery.GalleryItem.ExpandedGalleryItemHeight.CurrentValue.Equals(newValue))
        {
            return;
        }
        vm.Gallery.GalleryItem.ExpandedGalleryItemHeight.Value = newValue;
        Settings.Gallery.ExpandedGalleryItemSize = newValue;
        if (GalleryFunctions.IsFullGalleryOpen)
        {
            vm.Gallery.GalleryItem.ItemHeight.Value = Settings.Gallery.ExpandedGalleryItemSize;
        }
        await WindowResizing.SetSizeAsync(vm);
        await SaveSettingsAsync();
    }
    
    private static async Task BottomGalleryItemSizeChangedAsync(MainViewModel vm, double newValue)
    {
        if (vm.Gallery.GalleryItem.BottomGalleryItemHeight.CurrentValue.Equals(newValue))
        {
            return;
        }
        vm.Gallery.GalleryItem.BottomGalleryItemHeight.Value = newValue;
        Settings.Gallery.BottomGalleryItemSize = newValue;
        if (Settings.Gallery.IsBottomGalleryShown)
        {
            vm.Gallery.GalleryItem.ItemHeight.Value = Settings.Gallery.BottomGalleryItemSize;
        }
        await WindowResizing.SetSizeAsync(vm);
        await SaveSettingsAsync();
    }

    public void Dispose()
    {
        Disposable.Dispose(GalleryItem,
            GalleryMargin,
            IsBottomGalleryShown,
            IsBottomGalleryShownInHiddenUI,
            GalleryMode,
            GalleryStretch,
            GalleryVerticalAlignment,
            GalleryOrientation,
            IsGalleryExpanded,
            ToggleGalleryCommand,
            ToggleBottomGalleryCommand,
            CloseGalleryCommand,
            GalleryItemStretchCommand,
            IsUniformBottomChecked,
            IsUniformFullChecked,
            IsUniformMenuChecked,
            IsUniformToFillBottomChecked,
            IsUniformToFillFullChecked,
            IsUniformToFillMenuChecked,
            IsFillBottomChecked,
            IsFillFullChecked,
            IsFillMenuChecked,
            IsNoneBottomChecked,
            IsNoneFullChecked,
            IsNoneMenuChecked,
            IsSquareBottomChecked,
            IsSquareFullChecked,
            IsSquareMenuChecked,
            IsFillSquareBottomChecked,
            IsFillSquareFullChecked,
            IsFillSquareMenuChecked
            );
    }
}