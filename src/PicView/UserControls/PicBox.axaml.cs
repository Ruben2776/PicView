using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PicView.UserControls
{
    public partial class PicBox : UserControl
    {
        public PicBox()
        {
            InitializeComponent();
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
