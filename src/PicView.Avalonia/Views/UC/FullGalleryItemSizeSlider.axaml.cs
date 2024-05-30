using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.Helpers;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Views.UC;

public partial class FullGalleryItemSizeSlider : UserControl
{
    public FullGalleryItemSizeSlider()
    {
        InitializeComponent();
    }
    private void FullGallery_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (vm.GetExpandedGalleryItemHeight == e.NewValue)
        {
            return;
        }
        WindowHelper.SetSize(vm);
        if (GalleryFunctions.IsFullGalleryOpen)
        {
            vm.GetGalleryItemHeight = vm.GetExpandedGalleryItemHeight;
        }
        // Binding to height depends on timing of the update. Maybe find a cleaner mvvm solution one day

        // Maybe save this on close or some other way
        SettingsHelper.Settings.Gallery.ExpandedGalleryItemSize = e.NewValue;
        _ = SettingsHelper.SaveSettingsAsync();
    }
}