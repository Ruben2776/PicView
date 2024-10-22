using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.Converters;
using PicView.Avalonia.Resizing;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Views;

public partial class ExifView : UserControl
{
    public ExifView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            PixelWidthTextBox.KeyDown += async (s, e) => await OnKeyDownVerifyInput(s,e);
            PixelHeightTextBox.KeyDown += async (s, e) => await OnKeyDownVerifyInput(s,e);

            PixelWidthTextBox.KeyUp += delegate { AdjustAspectRatio(PixelWidthTextBox); };
            PixelHeightTextBox.KeyUp += delegate { AdjustAspectRatio(PixelHeightTextBox); };
            
        };
    }
    
    private void AdjustAspectRatio(TextBox sender)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }
        var aspectRatio = (double)vm.PixelWidth / vm.PixelHeight;
        AspectRatioHelper.SetAspectRatioForTextBox(PixelWidthTextBox, PixelHeightTextBox, sender == PixelWidthTextBox,
            aspectRatio, DataContext as MainViewModel);
    }

    private static async Task DoResize(MainViewModel vm, bool isWidth, object width, object height)
    {
        if (isWidth)
        {
            if (!double.TryParse((string?)width, out var widthValue))
            {
                return;
            }
            if (widthValue > 0)
            {
                var success = await ConversionHelper.ResizeByWidth(vm.FileInfo, widthValue).ConfigureAwait(false);
                if (success)
                {
                    if (vm.ImageIterator is not null)
                    {
                        await vm.ImageIterator.QuickReload().ConfigureAwait(false);
                    }
                }
            }
        }
        else
        {
            if (!double.TryParse((string?)height, out var heightValue))
            {
                return;
            }
            if (heightValue > 0)
            {
                var success = await ConversionHelper.ResizeByHeight(vm.FileInfo, heightValue).ConfigureAwait(false);
                if (success)
                {
                    vm.ImageIterator?.RemoveCurrentItemFromPreLoader();
                    await vm.ImageIterator?.IterateToIndex(vm.ImageIterator.CurrentIndex);
                }
            }
        }
    }
    
    private async Task OnKeyDownVerifyInput(object? sender, KeyEventArgs? e)
    {
        switch (e.Key)
        {
            case Key.D0:
            case Key.D1:
            case Key.D2:
            case Key.D3:
            case Key.D4:
            case Key.D5:
            case Key.D6:
            case Key.D7:
            case Key.D8:
            case Key.D9:
            case Key.NumPad0:
            case Key.NumPad1:
            case Key.NumPad2:
            case Key.NumPad3:
            case Key.NumPad4:
            case Key.NumPad5:
            case Key.NumPad6:
            case Key.NumPad7:
            case Key.NumPad8:
            case Key.NumPad9:
            case Key.Back:
            case Key.Delete:
                break; // Allow numbers and basic operations
    
            case Key.Left:
            case Key.Right:
            case Key.Tab:
            case Key.OemBackTab:
                break; // Allow navigation keys
    
            case Key.A:
            case Key.C:
            case Key.X:
            case Key.V:
                if (e.KeyModifiers == KeyModifiers.Control)
                {
                    // Allow Ctrl + A, Ctrl + C, Ctrl + X, and Ctrl + V (paste)
                    break;
                }
    
                e.Handled = true; // Only allow with Ctrl
                return;
    
            case Key.Oem5: // Key for `%` symbol (may vary based on layout)
                break; // Allow the percentage symbol (%)
    
            case Key.Escape: // Handle Escape key
                Focus();
                e.Handled = true;
                return;
    
            case Key.Enter: // Handle Enter key for saving
                if (DataContext is not MainViewModel vm)
                {
                    return;
                }

                await DoResize(vm, Equals(sender, PixelWidthTextBox), PixelWidthTextBox.Text, PixelHeightTextBox.Text).ConfigureAwait(false);
                return;
    
            default:
                e.Handled = true; // Block all other inputs
                return;
        }
    }
}