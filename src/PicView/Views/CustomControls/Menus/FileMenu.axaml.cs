using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PicView.CustomControls.Menus
{
    public partial class FileMenu : UserControl
    {
        public FileMenu()
        {
            InitializeComponent();
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
