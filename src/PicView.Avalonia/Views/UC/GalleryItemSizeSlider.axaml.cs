using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;
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
            CustomSlider.Minimum = vm.MinFullGalleryItemHeight;
        }
        else
        {
            CustomSlider.Maximum = vm.MaxBottomGalleryItemHeight;
            CustomSlider.Minimum = vm.MinBottomGalleryItemHeight;
        }
    }

    private void RangeBase_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (vm.GetFullGalleryItemHeight == e.NewValue)
            {
                return;
            }
            vm.GetFullGalleryItemHeight = e.NewValue;
            vm.GetGalleryItemHeight = vm.GetFullGalleryItemHeight;
            WindowResizing.SetSize(vm);
            // Binding to height depends on timing of the update. Maybe find a cleaner mvvm solution one day
        
            // Maybe save this on close or some other way
            SettingsHelper.Settings.Gallery.ExpandedGalleryItemSize = e.NewValue;
            
        }
        else if (SettingsHelper.Settings.Gallery.IsBottomGalleryShown)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (vm.GetBottomGalleryItemHeight == e.NewValue)
            {
                return;
            }
            vm.GetBottomGalleryItemHeight = e.NewValue;
        
            vm.GetGalleryItemHeight = e.NewValue;
            UIHelper.GetGalleryView.Height = vm.GalleryHeight;
            WindowResizing.SetSize(vm);
        
            // Binding to height depends on timing of the update. Maybe find a cleaner mvvm solution one day
            // Maybe save this on close or some other way
            SettingsHelper.Settings.Gallery.BottomGalleryItemSize = e.NewValue;
        }
       
        _ = SettingsHelper.SaveSettingsAsync();
    }
}
