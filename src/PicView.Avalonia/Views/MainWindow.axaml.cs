using Avalonia.Controls;
using PicView.Core.Config;

namespace PicView.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Models.LoadSettings.StartLoading();
        }
    }
}