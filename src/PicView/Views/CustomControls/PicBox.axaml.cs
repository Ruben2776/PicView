using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PicView.CustomControls
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
