using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace PicView.Views.CustomTitleBars
{
    public partial class WinTitleBar : UserControl
    {
        public WinTitleBar()
        {
            InitializeComponent();

            PointerPressed += MoveWindow;
        }

        private void MoveWindow(object? sender, PointerPressedEventArgs e)
        {
            if (VisualRoot is null) { return; }
            
            var hostWindow = (Window)VisualRoot;
            hostWindow?.BeginMoveDrag(e);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
