using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.Converters;
using PicView.Avalonia.UI;
using PicView.Avalonia.ViewModels;

namespace PicView.Avalonia.Views;

public partial class ExifView : UserControl
{
    public ExifView()
    {
        InitializeComponent();
        PixelHeightTextBox.KeyDown += async (s, e) => await PixelHeightTextBox_OnKeyDown(s, e);
        PixelWidthTextBox.KeyDown += async (s, e) => await PixelWidthTextBox_OnKeyDown(s, e);
    }

    private async Task PixelHeightTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is null)
        {
            return;
        }
        var vm = (MainViewModel)DataContext;
        var textBox = (TextBox)sender!;
        if (textBox is null)
        {
            return;
        }
        var text = ((TextBox)sender!).Text;
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (!double.TryParse(text, out var height))
        {
            return;
        }

        if (height > 0)
        {
            var success = await ConversionHelper.ResizeByHeight(vm.FileInfo, height).ConfigureAwait(false);
            if (success)
            {
                vm.ImageIterator?.PreLoader?.Remove(vm.ImageIterator.Index, vm.ImageIterator.Pics);
                await vm.ImageIterator?.IterateToIndex(vm.ImageIterator.Index);
            }
        }
    }

    private async Task PixelWidthTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is null)
        {
            return;
        }
        var vm = (MainViewModel)DataContext;
        var textBox = (TextBox)sender!;
        if (textBox is null)
        {
            return;
        }
        var text = ((TextBox)sender!).Text;
        if (e.Key != Key.Enter)
        {
            return;
        }

        if (!double.TryParse(text, out var width))
        {
            return;
        }

        if (width > 0)
        {
            var success = await ConversionHelper.ResizeByWidth(vm.FileInfo, width).ConfigureAwait(false);
            if (success)
            {
                vm.ImageIterator?.PreLoader?.Remove(vm.ImageIterator.Index, vm.ImageIterator.Pics);
                await vm.ImageIterator?.IterateToIndex(vm.ImageIterator.Index);
            }
        }
    }
}