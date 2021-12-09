using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PicView.CustomControls.Menus
{
    public partial class ImageMenu : UserControl
    {
        public ImageMenu()
        {
            InitializeComponent();
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
