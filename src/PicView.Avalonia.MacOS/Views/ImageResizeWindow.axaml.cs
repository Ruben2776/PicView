using Avalonia.Controls;
using Avalonia.Input;
using PicView.Core.Localization;

namespace PicView.Avalonia.MacOS.Views;

public partial class ImageResizeWindow : Window
{
    public ImageResizeWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            MinWidth = MaxWidth = Width;
            Height = 500;
            Title = TranslationHelper.Translation.ResizeImage + " - PicView";
        };
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
    }
}