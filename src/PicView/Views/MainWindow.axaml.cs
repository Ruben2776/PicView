using Avalonia;
using Avalonia.Animation.Animators;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using PicView.ViewModels;

namespace PicView.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void TopLevel_OnOpened(object? sender, EventArgs e)
        {
            (DataContext as MainWindowViewModel)?.LoadCommand.Execute(null);
        }
    }
}