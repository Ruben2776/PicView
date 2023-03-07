using System.Windows.Controls;
using System.Windows.Media;

namespace PicView.Views.UserControls.Buttons
{
    /// <summary>
    /// Interaction logic for StarButtonOutline.xaml
    /// </summary>
    public partial class StarOutlineButton : UserControl
    {
        public StarOutlineButton()
        {
            InitializeComponent();
        }

        public void FillStar()
        {
            var filled = (DrawingImage)TryFindResource("Star_FilledDrawingImage");
            Star.Source = filled;
        }

        public void OutlineStar()
        {
            var outline = (DrawingImage)TryFindResource("Star_OutlineDrawingImage");
            Star.Source = outline;
        }
    }
}