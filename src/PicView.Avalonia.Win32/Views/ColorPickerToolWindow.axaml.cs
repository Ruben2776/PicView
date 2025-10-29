using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using PicView.Avalonia.FileSystem; // for ColorUtils

namespace PicView.Avalonia.Win32.Views;

public partial class ColorPickerToolWindow : Window
{
    private Color _current;

    public ColorPickerToolWindow(Color color)
    {
        InitializeComponent(); // now works, generated from .axaml
        _current = color;
        UpdateUI(color);
    }

    private void UpdateUI(Color color)
    {
        ColorPreview.Background = new SolidColorBrush(color);
        HexBox.Text = $"{color.R:X2}{color.G:X2}{color.B:X2}";

        var hsb = ColorPickerToolManager.ToHsb(color);
        HueSlider.Value = hsb.H;
        SatSlider.Value = hsb.S * 100;
        BriSlider.Value = hsb.B * 100;
    }

    private async void CopyHex(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
            await clipboard.SetTextAsync(HexBox.Text);
    }

    private void SliderChanged(object? sender, RoutedEventArgs e)
    {
        var newColor = ColorPickerToolManager.FromHsb(
            HueSlider.Value,
            SatSlider.Value / 100,
            BriSlider.Value / 100);

        _current = newColor;
        UpdateUI(newColor);
    }
}
