using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Views;

public partial class AppearanceView : UserControl
{
    public AppearanceView()
    {
        InitializeComponent();
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                TaskBarToggleButton.IsVisible = false;
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TaskBarToggleButton.IsVisible = false;
                });
            }
        }
        Loaded += AppearanceView_Loaded;
    }

    private void AppearanceView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        GalleryStretchMode.DetermineStretchMode(vm);
        
        if (vm.IsUniformFullChecked)
        {
            FullGalleryComboBox.SelectedIndex = 0;
        }
        else if (vm.IsUniformToFillFullChecked)
        {
            FullGalleryComboBox.SelectedIndex = 1;
        }
        else if (vm.IsFillFullChecked)
        {
            FullGalleryComboBox.SelectedIndex = 2;
        }
        else if (vm.IsNoneFullChecked)
        {
            FullGalleryComboBox.SelectedIndex = 3;
        }
        else if (vm.IsSquareFullChecked)
        {
            FullGalleryComboBox.SelectedIndex = 4;
        }
        else if (vm.IsFillSquareFullChecked)
        {
            FullGalleryComboBox.SelectedIndex = 5;
        }
        
        if (vm.IsUniformBottomChecked)
        {
            BottomGalleryComboBox.SelectedIndex = 0;
        }
        else if (vm.IsUniformToFillBottomChecked)
        {
            BottomGalleryComboBox.SelectedIndex = 1;
        }
        else if (vm.IsFillBottomChecked)
        {
            BottomGalleryComboBox.SelectedIndex = 2;
        }
        else if (vm.IsNoneBottomChecked)
        {
            BottomGalleryComboBox.SelectedIndex = 3;
        }
        else if (vm.IsSquareBottomChecked)
        {
            BottomGalleryComboBox.SelectedIndex = 4;
        }
        else if (vm.IsFillSquareBottomChecked)
        {
            BottomGalleryComboBox.SelectedIndex = 5;
        }
        
        FullGalleryComboBox.SelectionChanged += async (_, _) => await FullGalleryComboBox_SelectionChanged();
        BottomGalleryComboBox.SelectionChanged += async (_, _) => await BottomGalleryComboBox_SelectionChanged();
    }
    
    private async Task FullGalleryComboBox_SelectionChanged()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        
        if (FullGalleryUniformItem.IsSelected)
        {
            await GalleryStretchMode.ChangeFullGalleryItemStretch(vm, Stretch.Uniform);
        }
        else if (FullGalleryUniformToFillItem.IsSelected)
        {
            await GalleryStretchMode.ChangeFullGalleryItemStretch(vm, Stretch.UniformToFill);
        }
        else if (FullGalleryFillItem.IsSelected)
        {
            await GalleryStretchMode.ChangeFullGalleryItemStretch(vm, Stretch.Fill);
        }
        else if (FullGalleryNoneItem.IsSelected)
        {
            await GalleryStretchMode.ChangeFullGalleryItemStretch(vm, Stretch.None);
        }
        else if (FullGallerySquareItem.IsSelected)
        {
            await GalleryStretchMode.ChangeFullGalleryStretchSquare(vm);
        }
        else if (FullGalleryFillSquareItem.IsSelected)
        {
            await GalleryStretchMode.ChangeFullGalleryStretchSquareFill(vm);
        }
    }

    private async Task BottomGalleryComboBox_SelectionChanged()
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        
        if (BottomGalleryUniformItem.IsSelected)
        {
            await GalleryStretchMode.ChangeBottomGalleryItemStretch(vm, Stretch.Uniform);
        }
        else if (BottomGalleryUniformToFillItem.IsSelected)
        {
            await GalleryStretchMode.ChangeBottomGalleryItemStretch(vm, Stretch.UniformToFill);
        }
        else if (BottomGalleryFillItem.IsSelected)
        {
            await GalleryStretchMode.ChangeBottomGalleryItemStretch(vm, Stretch.Fill);
        }
        else if (BottomGalleryNoneItem.IsSelected)
        {
            await GalleryStretchMode.ChangeBottomGalleryItemStretch(vm, Stretch.None);
        }
        else if (BottomGallerySquareItem.IsSelected)
        {
            await GalleryStretchMode.ChangeBottomGalleryStretchSquare(vm);
        }
        else if (BottomGalleryFillSquareItem.IsSelected)
        {
            await GalleryStretchMode.ChangeBottomGalleryStretchSquareFill(vm);
        }
    }
}