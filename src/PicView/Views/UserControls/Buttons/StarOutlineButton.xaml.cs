using System.Windows.Media;

namespace PicView.Views.UserControls.Buttons
{
    public partial class StarOutlineButton
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