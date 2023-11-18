using System.Windows;

namespace PicView.Views.UserControls.Misc
{
    public partial class DragDropOverlay
    {
        public DragDropOverlay()
        {
            InitializeComponent();
        }

        public void UpdateContent(UIElement element)
        {
            ContentHolder.Content = element;
        }
    }
}