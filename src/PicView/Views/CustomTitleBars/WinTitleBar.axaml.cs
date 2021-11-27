using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;

namespace PicView.UserControls
{
    public partial class WinTitleBar : UserControl
    {
        private Button closeButton;

        public WinTitleBar()
        {
            InitializeComponent();
            closeButton = this.FindControl<Button>("CloseButton");

            closeButton.Click += (_, _) => CloseWindow();

            PointerPressed += (_, e) => MoveWindow(e);
        }

        private void CloseWindow()
        {
            Window hostWindow = (Window)VisualRoot;
            hostWindow.Close();
        }

        private void MoveWindow(PointerPressedEventArgs e)
        {
            Window hostWindow = (Window)VisualRoot;
            hostWindow.BeginMoveDrag(e);
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
