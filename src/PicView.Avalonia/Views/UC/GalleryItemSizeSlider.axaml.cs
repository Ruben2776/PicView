using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Views.UC;

public partial class GalleryItemSizeSlider : UserControl
{
    public GalleryItemSizeSlider()
    {
        InitializeComponent();
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
            WindowHelper.SetSize(vm);
            var mainView = desktop.MainWindow.GetControl<MainView>("MainView");
            var gallery = mainView.GalleryView;
            gallery.Height = vm.GalleryHeight;
        }
        else
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (vm.GetExpandedGalleryItemHeight == e.NewValue)
            {
                return;
            }
            vm.GetExpandedGalleryItemHeight = e.NewValue;
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
