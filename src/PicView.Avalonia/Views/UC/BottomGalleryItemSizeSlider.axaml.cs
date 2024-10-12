using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
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
        if (DataContext is not MainViewModel vm )
        {
            return;
        }
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (vm.GetBottomGalleryItemHeight == e.NewValue)
        {
            return;
        }
        vm.GetBottomGalleryItemHeight = e.NewValue;
        
        if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown && !GalleryFunctions.IsFullGalleryOpen)
        {
            vm.GetGalleryItemHeight = e.NewValue;
            UIHelper.GetGalleryView.Height = vm.GalleryHeight;
            WindowHelper.SetSize(vm);
        }
        
        // Binding to height depends on timing of the update. Maybe find a cleaner mvvm solution one day
        // Maybe save this on close or some other way
        SettingsHelper.Settings.Gallery.BottomGalleryItemSize = e.NewValue;
    }
}