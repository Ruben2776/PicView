using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Views.UC;

public partial class GalleryItemSizeSlider : UserControl
{
    public GalleryItemSizeSlider()
    {
        InitializeComponent();
    }
    
    public void SetMaxAndMin()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        if (GalleryFunctions.IsFullGalleryOpen)
        {
            CustomSlider.Maximum = vm.MaxFullGalleryItemHeight;
            CustomSlider.MinWidth = vm.MinFullGalleryItemHeight;
        }
        else
        {
            CustomSlider.Maximum = vm.MaxBottomGalleryItemHeight;
            CustomSlider.Minimum = vm.MinBottomGalleryItemHeight;
        }
    }

    private void RangeBase_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm ||
            Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        if (GalleryFunctions.IsBottomGalleryOpen && !GalleryFunctions.IsFullGalleryOpen)
        {
            // Change the sizes of the bottom gallery items
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (vm.GetBottomGalleryItemHeight == e.NewValue)
            {
                return;
            }
            SettingsHelper.Settings.Gallery.BottomGalleryItemSize = e.NewValue;
            vm.GetGalleryItemHeight = e.NewValue;
            var mainView = desktop.MainWindow.GetControl<MainView>("MainView");
            var gallery = mainView.GalleryView;
            gallery.Height = vm.GalleryHeight;
            WindowHelper.SetSize(vm);
        }
        else
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (vm.GetFullGalleryItemHeight == e.NewValue)
            {
                return;
            }
            vm.GetFullGalleryItemHeight = e.NewValue;
            if (GalleryFunctions.IsFullGalleryOpen)
            {
                WindowHelper.SetSize(vm);
                vm.GetGalleryItemHeight = e.NewValue;
            }
            SettingsHelper.Settings.Gallery.ExpandedGalleryItemSize = e.NewValue;
        }
       
        _ = SettingsHelper.SaveSettingsAsync();
    }
}
