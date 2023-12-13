using System.Windows;

namespace PicView.WPF.Views.UserControls.Misc
{
    public partial class DragDropOverlay
    {
        public DragDropOverlay()
        {
            InitializeComponent();
        }

        public void UpdateContent(UIElement element)
        {
            if (element is null)
            {
                return;
            }
            try
            {
                ContentHolder.Content = element;
            }
            catch (Exception)
            {
                return;
            }
        }
    }
}