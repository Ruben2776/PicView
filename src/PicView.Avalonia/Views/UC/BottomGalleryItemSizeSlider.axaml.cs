using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Views.UC;

public partial class BottomGalleryItemSizeSlider : UserControl
{
    public BottomGalleryItemSizeSlider()
    {
        InitializeComponent();
    }
    private void BottomGallery_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm ||
            Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (vm.GetBottomGalleryItemHeight == e.NewValue)
        {
            return;
        }
        SettingsHelper.Settings.Gallery.BottomGalleryItemSize = e.NewValue;
        if (GalleryFunctions.IsBottomGalleryOpen && !GalleryFunctions.IsFullGalleryOpen)
        {
            vm.GetGalleryItemHeight = e.NewValue;
            var mainView = desktop.MainWindow.GetControl<MainView>("MainView");
            var gallery = mainView.GalleryView;
            gallery.Height = vm.GalleryHeight;
            WindowHelper.SetSize(vm);
        }
        
        // Binding to height depends on timing of the update. Maybe find a cleaner mvvm solution one day
        // Maybe save this on close or some other way
        _ = SettingsHelper.SaveSettingsAsync();
    }
}