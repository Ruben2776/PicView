using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Gallery;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.Views.UC.Menus;
using PicView.Core.Config;

namespace PicView.Avalonia.ImageTransformations;
public static class Rotation
{
    public enum RotationButton
    {
        WindowBorderButton,
        RotateRightButton,
        RotateLeftButton
    }

    public static async Task RotateRight(MainViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => { vm.ImageViewer.Rotate(false); });
    }
    
    public static async Task RotateRight(MainViewModel? vm, RotationButton rotationButton)
    {
        await RotateRight(vm);
        
        // Check if it should move the cursor
        if (!SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            return;
        }

        await MoveCursorAfterRotation(vm, rotationButton);
    }

    private static async Task MoveCursorAfterRotation(MainViewModel? vm, RotationButton rotationButton)
    {
        // Move cursor when button is clicked
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                Button? button;
                ImageMenu? menu;
                switch (rotationButton)
                {
                    case RotationButton.WindowBorderButton:
                        button = UIHelper.GetTitlebar.GetControl<Button>("RotateRightButton");
                        break;
                    case RotationButton.RotateRightButton:
                        menu = UIHelper.GetMainView.MainGrid.Children.OfType<ImageMenu>().FirstOrDefault();
                        button = menu?.GetControl<Button>("RotateRightButton");
                        break;
                    case RotationButton.RotateLeftButton:
                        menu = UIHelper.GetMainView.MainGrid.Children.OfType<ImageMenu>().FirstOrDefault();
                        button = menu?.GetControl<Button>("RotateLeftButton");
                        break;
                    default:
                        return;
                }

                if (button is null || !button.IsPointerOver)
                {
                    return;
                }

                var p = button.PointToScreen(new Point(10, 15));
                vm.PlatformService?.SetCursorPos(p.X, p.Y);
            }
#if DEBUG
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            #else
            catch (Exception) { }
#endif
        });
    }
            
    public static async Task RotateLeft(MainViewModel? vm)
    {
        if (vm is null)
        {
            return;
        }

        if (GalleryFunctions.IsFullGalleryOpen)
        {
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() => { vm.ImageViewer.Rotate(true); });
    }

    public static async Task RotateLeft(MainViewModel vm, RotationButton rotationButton)
    {
        await RotateLeft(vm);
        
        // Check if it should move the cursor
        if (!SettingsHelper.Settings.WindowProperties.AutoFit)
        {
            return;
        }
        await MoveCursorAfterRotation(vm, rotationButton);
    }
}
