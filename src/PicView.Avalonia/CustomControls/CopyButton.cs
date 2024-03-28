using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace PicView.Avalonia.CustomControls;

public class CopyButton : Button
{
    public static readonly AvaloniaProperty<string> CopyTextProperty =
        AvaloniaProperty.Register<CopyButton, string>(nameof(CopyText));
    protected override Type StyleKeyOverride => typeof(Button);
    
    public string CopyText
    {
        get => (string)GetValue(CopyTextProperty);
        set => SetValue(CopyTextProperty, value);
    }

    public CopyButton()
    {
        Click += CopyButton_OnClick;
    }

    private async void CopyButton_OnClick(object? sender, RoutedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(CopyText))
        {
            return;
        }
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        var dataObject = new DataObject();
        dataObject.Set(DataFormats.Text, CopyText);
        await clipboard.SetDataObjectAsync(dataObject);
    }
}