using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PicView.UserControls
{
    public partial class TitleBox : UserControl
    {
        public TitleBox()
        {
            InitializeComponent();
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
