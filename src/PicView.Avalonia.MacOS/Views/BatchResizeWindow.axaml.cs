using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Localization;

namespace PicView.Avalonia.MacOS.Views;

public partial class BatchResizeWindow : Window
{
    public BatchResizeWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            MinWidth = MaxWidth = Width;
            Height = 500;
            Title = TranslationHelper.Translation.BatchResize + " - PicView";
            
            // Keep window position when resizing
            ClientSizeProperty.Changed.Subscribe(size =>
            {
                WindowResizing.HandleWindowResize(this, size);
            });
        };
        KeyDown += (_, e) =>
        {
            if (e.Key is Key.Escape)
            {
                Close();
            }
        };
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        if (VisualRoot is null) { return; }

        var hostWindow = (Window)VisualRoot;
        hostWindow?.BeginMoveDrag(e);
    }
}