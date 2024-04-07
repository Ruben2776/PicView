using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using PicView.Avalonia.Navigation;
using PicView.Avalonia.ViewModels;
using PicView.Core.Config;

namespace PicView.Avalonia.Views;

public partial class GalleryView : UserControl
{
    public GalleryView()
    {
        InitializeComponent();
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (!NavigationHelper.CanNavigate(vm))
        {
            return;
        }

        if (sender is not Border item)
        {
            return;
        }

        if (item.Parent is not ListBoxItem listBoxItem)
        {
            return;
        }

        if (listBoxItem.Parent is not ListBox parent)
        {
            return;
        }

        //var index = parent.ItemContainerGenerator.IndexFromContainer(item);
        //var itemsControl = new ItemsControl();
        //var index2 = itemsControl.IndexFromContainer(listBoxItem);
        //var index3 = itemsControl.IndexFromContainer(item);
        var index4 = parent.ItemContainerGenerator.IndexFromContainer(listBoxItem);
        //var index5 = parent.ItemContainerGenerator.IndexFromContainer(item);
        //var index6 = itemsControl.IndexFromContainer(parent);
        //var index7 = itemsControl.IndexFromContainer(listBoxItem);
        //var index8 = itemsControl.IndexFromContainer(item);

        vm.ToggleGalleryCommand.Execute(null);
        vm.LoadPicAtIndex(index4).ConfigureAwait(false);
    }

    private void GalleryListBox_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var scrollViewer = GalleryListBox.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer is null)
        {
            return;
        }

        if (e.Delta.Y > 0)
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                scrollViewer.LineLeft();
                scrollViewer.LineLeft();
            }
            else
            {
                scrollViewer.LineRight();
                scrollViewer.LineRight();
            }
        }
        else
        {
            if (SettingsHelper.Settings.Zoom.HorizontalReverseScroll)
            {
                scrollViewer.LineRight();
                scrollViewer.LineRight();
            }
            else
            {
                scrollViewer.LineLeft();
                scrollViewer.LineLeft();
            }
        }
    }
}