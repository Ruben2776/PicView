using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PicView.CustomControls.Menus
{
    public partial class SettingsMenu : UserControl
    {
        public SettingsMenu()
        {
            InitializeComponent();
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
