using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PicView.CustomControls.Menus
{
    public partial class ToolsMenu : UserControl
    {
        public ToolsMenu()
        {
            InitializeComponent();
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
