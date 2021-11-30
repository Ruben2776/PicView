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

            PointerPressed += (_, e) => MoveWindow(e);
        }

        private void MoveWindow(PointerPressedEventArgs e)
        {
            if (VisualRoot is null) { return; }
            
            Window hostWindow = (Window)VisualRoot;
            hostWindow?.BeginMoveDrag(e);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
